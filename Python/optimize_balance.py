import optuna
import torch
import numpy as np
import os
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel
from mlagents_envs.base_env import ActionTuple

# 모델 임포트
from model import ArenaPPOModel

# 하이퍼파라미터
EPISODES_PER_TRIAL = 30
TIMEOUT_STEP_LIMIT = 3000
MODEL_PATH = "ArenaPPO_Ep1000.pt"

# 연산 장치 설정
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# 평가용 PyTorch 모델 로드
def load_model():
    model = ArenaPPOModel(input_dim=30, hidden_dim=256, move_actions=9, skill_actions=5).to(device)
    if not os.path.exists(MODEL_PATH):
        raise FileNotFoundError(f"[오류] {MODEL_PATH} 파일을 찾을 수 없습니다.")
    
    model.load_state_dict(torch.load(MODEL_PATH, map_location=device))
    model.eval() # 추론 모드 전환 (그래디언트 연산 비활성화)
    return model

def objective(trial, model):
    # Optuna 밸런스 탐색 공간 설정 (5단위, 0.05단위 제약)
    warrior_hp = trial.suggest_int("warrior_hp", 150, 300, step=5)
    mage_hp = trial.suggest_int("mage_hp", 100, 250, step=5)
    warrior_dmg = trial.suggest_float("warrior_dmg_mult", 0.5, 1.5, step=0.05)
    mage_dmg = trial.suggest_float("mage_dmg_mult", 0.5, 1.5, step=0.05)

    # 유니티로 파라미터 전송
    env_channel = EnvironmentParametersChannel()
    env_channel.set_float_parameter("warrior_hp", float(warrior_hp))
    env_channel.set_float_parameter("mage_hp", float(mage_hp))
    env_channel.set_float_parameter("warrior_dmg_mult", float(warrior_dmg))
    env_channel.set_float_parameter("mage_dmg_mult", float(mage_dmg))

    env = UnityEnvironment(file_name=None, side_channels=[env_channel])
    env.reset()
    
    behavior_names = list(env.behavior_specs.keys())
    if not behavior_names:
        env.close()
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
                # train.py와 동일하게 obs[1]에서 30차원 데이터 추출
                obs = torch.tensor(decision_steps.obs[1], dtype=torch.float32, device=device)
                
                with torch.no_grad():
                    # forward를 호출하여 로짓(Logits) 획득
                    move_logits, skill_logits, _ = model.forward(obs)
                    
                    # 확률 분포 샘플링 대신 가장 높은 값(Argmax) 선택
                    move_action = torch.argmax(move_logits, dim=-1).cpu().numpy()
                    skill_action = torch.argmax(skill_logits, dim=-1).cpu().numpy()
                    
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
                    # 보상이 양수(승리)인 에이전트의 직업 판별 (0: 전사, 1: 마법사)
                    # 종료된 에이전트의 마지막 관측값(obs[1])의 첫 번째 인덱스(classId) 확인
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

    env.close()

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
    print("모델 로드 완료. 유니티 에디터에서 Play 버튼을 대기합니다.")

    # 람다(lambda)를 사용하여 model 인자를 objective 함수로 전달
    study = optuna.create_study(direction="minimize")
    study.optimize(lambda trial: objective(trial, eval_model), n_trials=50)

    print("\n" + "="*50)
    print("최적의 황금 밸런스 파라미터 도출 완료:")
    for key, value in study.best_params.items():
        if "dmg" in key:
            print(f" - {key}: {value:.2f}")
        else:
            print(f" - {key}: {value}")
    print("="*50)