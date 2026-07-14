import optuna
import torch
import numpy as np
import os
from torch.distributions import Categorical
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.base_env import ActionTuple

# 모델 임포트
from model import ArenaPPOModel

# 하이퍼파라미터
EPISODES_PER_TRIAL = 30
TIMEOUT_STEP_LIMIT = 3000
MODEL_PATH = "ArenaPPO_Ep1400.pt"

# 연산 장치 설정
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

env_channel = EnvironmentParametersChannel()
engine_channel = EngineConfigurationChannel()
engine_channel.set_configuration_parameters(time_scale=10.0)
global_env = None

# 평가용 PyTorch 모델 로드
def load_model():
    model = ArenaPPOModel(input_dim=30, hidden_dim=256, move_actions=9, skill_actions=5).to(device)
    if not os.path.exists(MODEL_PATH):
        raise FileNotFoundError(f"[오류] {MODEL_PATH} 파일을 찾을 수 없습니다.")
    
    model.load_state_dict(torch.load(MODEL_PATH, map_location=device))
    model.eval() # 추론 모드 전환 (그래디언트 연산 비활성화)
    return model

def objective(trial, model, env):
    # Optuna 밸런스 탐색 공간 설정 (5단위, 0.05단위 제약)
    warrior_hp = trial.suggest_int("warrior_hp", 150, 300, step=5)
    mage_hp = trial.suggest_int("mage_hp", 100, 250, step=5)
    warrior_dmg = trial.suggest_float("warrior_dmg_mult", 0.5, 1.5, step=0.05)
    mage_dmg = trial.suggest_float("mage_dmg_mult", 0.5, 1.5, step=0.05)

    # 유니티로 파라미터 전송
    env_channel.set_float_parameter("warrior_max_hp", float(warrior_hp))
    env_channel.set_float_parameter("mage_max_hp", float(mage_hp))
    env_channel.set_float_parameter("warrior_dmg_mult", float(warrior_dmg))
    env_channel.set_float_parameter("mage_dmg_mult", float(mage_dmg))

    env.reset()
    
    behavior_names = list(env.behavior_specs.keys())
    if not behavior_names:
        return 999.0 # 에러 시 높은 Loss 반환

    behavior_name = behavior_names[0]

    warrior_wins = 0
    mage_wins = 0
    timeouts = 0
    
    # 시뮬레이션 루프
    for _ in range(EPISODES_PER_TRIAL):
        env.reset()
        is_done = False
        step_count = 0

        while not is_done:
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            
            # --- 행동(Action) 추론 ---
            if len(decision_steps) > 0:
                obs = torch.tensor(decision_steps.obs[1], dtype=torch.float32, device=device)
                
                with torch.no_grad():
                    move_logits, skill_logits, _ = model.forward(obs)
                    move_dist = Categorical(logits=move_logits)
                    skill_dist = Categorical(logits=skill_logits)
                    
                    move_action = move_dist.sample().cpu().numpy()
                    skill_action = skill_dist.sample().cpu().numpy()
                    
                actions_np = np.column_stack((move_action, skill_action))
                action_tuple = ActionTuple(discrete=actions_np)
                env.set_actions(behavior_name, action_tuple)
            
            env.step()
            step_count += 1
            
            # --- 결과 판정 (사망 또는 타임아웃) ---
            next_decision_steps, next_terminal_steps = env.get_steps(behavior_name)
            
            # 1) 사망으로 인한 에피소드 종료
            if len(next_terminal_steps.agent_id) > 0:
                is_done = True
                for agent_id in next_terminal_steps.agent_id:
                    reward = next_terminal_steps[agent_id].reward
                    class_id = int(next_terminal_steps[agent_id].obs[1][0])
                    
                    if reward > 0:
                        if class_id == 0:
                            warrior_wins += 1
                        elif class_id == 1:
                            mage_wins += 1
                break

            # 2) 타임아웃 종료
            if step_count >= TIMEOUT_STEP_LIMIT and not is_done:
                timeouts += 1
                is_done = True
                break

    # [수정됨] env.close() 삭제 완료

    # 목적 함수 계산 (Loss)
    warrior_win_rate = warrior_wins / EPISODES_PER_TRIAL
    mage_win_rate = mage_wins / EPISODES_PER_TRIAL
    timeout_rate = timeouts / EPISODES_PER_TRIAL

    # 승률 격차 최소화 + 무승부 억제(패널티 가중치 0.5)
    loss = abs(warrior_win_rate - mage_win_rate) + (0.5 * timeout_rate)

    print(f"Trial 결과 [HP: 전사{warrior_hp}/법사{mage_hp}, Dmg: 전사{warrior_dmg:.2f}/법사{mage_dmg:.2f}] "
          f"-> 전사 승: {warrior_wins}, 마법사 승: {mage_wins}, 무승부: {timeouts} | Loss: {loss:.4f}")
    
    return loss

if __name__ == "__main__":
    print("밸런싱 최적화 시뮬레이션을 준비합니다...")
    eval_model = load_model()
    
    # [수정됨] 전역 환경(Global Environment) 단 한 번 초기화
    print("유니티 에디터에서 Play 버튼을 대기합니다.")
    global_env = UnityEnvironment(file_name=None, side_channels=[env_channel, engine_channel])

    study = optuna.create_study(
        study_name="arena_balance_study",
        storage="sqlite:///arena_balance.db",
        load_if_exists=True,
        direction="minimize"
    )
    
    # 람다 함수에서 global_env 매개변수를 정상적으로 넘겨줌
    study.optimize(lambda trial: objective(trial, eval_model, global_env), n_trials=50)

    # 모든 트라이얼이 종료된 후 마지막에 한 번만 닫기
    if global_env is not None:
        global_env.close()
        
    print("\n" + "="*50)
    print("최적의 황금 밸런스 파라미터 도출 완료:")
    print(f"최적의 밸런스 값: {study.best_params}")
    print("="*50)