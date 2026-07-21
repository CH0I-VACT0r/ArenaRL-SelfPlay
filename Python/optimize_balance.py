import optuna
import torch
import numpy as np
import os
import uuid
import json
from torch.distributions import Categorical
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from mlagents_envs.side_channel.side_channel import SideChannel, IncomingMessage
from mlagents_envs.base_env import ActionTuple

# 유니티 텔레메트리 수신용 사이드 채널
class PythonTelemetryChannel(SideChannel):
    def __init__(self) -> None:
        # C# 스크립트와 동일한 UUID
        super().__init__(uuid.UUID("11111111-2222-3333-4444-555555555555"))
        self.telemetry_history = []

    def on_message_received(self, msg: IncomingMessage) -> None:
        raw_string = msg.read_string()
        data_dict = json.loads(raw_string)
        self.telemetry_history.append(data_dict)
        
    def clear_history(self):
        self.telemetry_history.clear()

# 전역 채널 인스턴스 생성
telemetry_channel = PythonTelemetryChannel()

# 모델 임포트
from model import ArenaPPOModel

# 하이퍼파라미터
EPISODES_PER_TRIAL = 30
TIMEOUT_STEP_LIMIT = 2000
MODEL_PATH = "ArenaPPO_Ep7000.pt"

# 연산 장치 설정
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

env_channel = EnvironmentParametersChannel()
engine_channel = EngineConfigurationChannel()
engine_channel.set_configuration_parameters(time_scale=20.0)
global_env = None

# 평가용 PyTorch 모델 로드
def load_model():
    model = ArenaPPOModel(input_dim=31, hidden_dim=256, move_actions=9, skill_actions=5).to(device)
    if not os.path.exists(MODEL_PATH):
        raise FileNotFoundError(f"[오류] {MODEL_PATH} 파일을 찾을 수 없습니다.")
    
    model.load_state_dict(torch.load(MODEL_PATH, map_location=device))
    model.eval() # 추론 모드 전환 (그래디언트 연산 비활성화)
    return model

def objective(trial, model, env):
    # 기존 체력 및 대미지 탐색 공간
    warrior_hp = trial.suggest_int("warrior_hp", 200, 300, step=5)
    mage_hp = trial.suggest_int("mage_hp", 150, 250, step=5)
    warrior_dmg = trial.suggest_float("warrior_dmg_mult", 0.5, 1.5, step=0.05)
    mage_dmg = trial.suggest_float("mage_dmg_mult", 0.5, 1.5, step=0.05)

    # 이동 속도 및 쿨타임 탐색 공간 (-20% ~ +20% 범위, 0.05 단위)
    warrior_speed = trial.suggest_float("warrior_speed", 0.8, 1.2, step=0.05)
    mage_speed = trial.suggest_float("mage_speed", 0.8, 1.2, step=0.05)
    warrior_cd = trial.suggest_float("warrior_cd", 0.8, 1.2, step=0.05)
    mage_cd = trial.suggest_float("mage_cd", 0.8, 1.2, step=0.05)

    # 유니티로 파라미터 전송
    env_channel.set_float_parameter("warrior_max_hp", float(warrior_hp))
    env_channel.set_float_parameter("mage_max_hp", float(mage_hp))
    env_channel.set_float_parameter("warrior_dmg_mult", float(warrior_dmg))
    env_channel.set_float_parameter("mage_dmg_mult", float(mage_dmg))
    
    env_channel.set_float_parameter("warrior_speed", float(warrior_speed))
    env_channel.set_float_parameter("mage_speed", float(mage_speed))
    env_channel.set_float_parameter("warrior_cd", float(warrior_cd))
    env_channel.set_float_parameter("mage_cd", float(mage_cd))

    env.reset()
    
    behavior_names = list(env.behavior_specs.keys())
    if not behavior_names:
        return 999.0 # 에러 시 높은 Loss 반환

    behavior_name = behavior_names[0]

    warrior_wins = 0
    mage_wins = 0
    timeouts = 0
    episode_steps_list = []
    telemetry_channel.clear_history()

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
        episode_steps_list.append(step_count)

    # EP06: 다중 목적 함수

    # 승률 및 타임아웃 페널티
    warrior_win_rate = warrior_wins / EPISODES_PER_TRIAL
    mage_win_rate = mage_wins / EPISODES_PER_TRIAL
    win_gap = abs(warrior_win_rate - mage_win_rate)
    timeout_rate = timeouts / EPISODES_PER_TRIAL

    # 텔레메트리 데이터 평균 산출
    history = telemetry_channel.telemetry_history
    if len(history) == 0:
        return 999.0
    
    avg_survival = sum(d["survivalTime"] for d in history) / len(history)
    avg_distance = sum(d["averageDistance"] for d in history) / len(history)
    avg_w_hit = sum(d["warriorHitRate"] for d in history) / len(history)
    avg_m_hit = sum(d["mageHitRate"] for d in history) / len(history)
    avg_w_dps = sum(d["warriorDPS"] for d in history) / len(history)
    avg_m_dps = sum(d["mageDPS"] for d in history) / len(history)

    # 3. 세부 지표 페널티 계산 (목표값과의 오차율을 0~1 사이로 정규화)
    # 목표: 생존 20초, 거리 3.2, 명중률 30%(0.3), DPS 6.5
    survival_penalty = abs(20.0 - avg_survival) / 20.0
    distance_penalty = abs(3.2 - avg_distance) / 3.2
    
    hitrate_penalty = ((abs(0.3 - avg_w_hit) / 0.3) + (abs(0.3 - avg_m_hit) / 0.3)) / 2.0
    dps_penalty = ((abs(6.5 - avg_w_dps) / 6.5) + (abs(6.5 - avg_m_dps) / 6.5)) / 2.0

    # 4. 최종 통합 Loss 가중치 합산
    loss = (
        0.35 * win_gap
        + 0.20 * survival_penalty
        + 0.15 * timeout_rate
        + 0.10 * hitrate_penalty
        + 0.10 * distance_penalty
        + 0.10 * dps_penalty
    )

    print(f"Trial 결과 -> 승률격차: {win_gap:.2f}, 생존: {avg_survival:.1f}s, 거리: {avg_distance:.1f}, W_DPS: {avg_w_dps:.1f}")
    print(f"Final Integrated Loss: {loss:.4f}")
    
    return loss

if __name__ == "__main__":
    print("밸런싱 최적화 시뮬레이션을 준비합니다...")
    eval_model = load_model()
    
    # 전역 환경(Global Environment)
    # side_channels 리스트에 telemetry_channel 추가
    print("유니티 에디터에서 Play 버튼을 대기합니다.")
    global_env = UnityEnvironment(
        file_name=None, 
        side_channels=[env_channel, engine_channel, telemetry_channel]
    )

    study = optuna.create_study(
        study_name="Arena_Balance_EP07_V2",
        storage="sqlite:///arena_balance.db",
        load_if_exists=True,
        direction="minimize"
    )
    
    # 람다 함수에서 global_env 매개변수를 정상적으로 넘겨줌
    study.optimize(lambda trial: objective(trial, eval_model, global_env), n_trials=100)

    # 모든 트라이얼이 종료된 후 마지막에 한 번만 닫기
    if global_env is not None:
        global_env.close()
        
    print("\n" + "="*50)
    print("최적의 황금 밸런스 파라미터 도출 완료:")
    print(f"최적의 밸런스 값: {study.best_params}")
    print("="*50)