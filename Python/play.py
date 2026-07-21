import torch
import numpy as np
from torch.distributions import Categorical
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.base_env import ActionTuple
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
from model import ArenaPPOModel

# 모델 경로
MODEL_PATH = "ArenaPPO_Ep7000.pt" 
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

def play():
    print(f"[시스템 초기화] 플레이 테스트용 연산 장치: {device}")
    
    # 모델 로드 (관측 공간 31차원)
    model = ArenaPPOModel(input_dim=31, hidden_dim=256, move_actions=9, skill_actions=5).to(device)
    model.load_state_dict(torch.load(MODEL_PATH, map_location=device))
    model.eval() # 그래디언트 연산 비활성화 (순수 추론 모드)
    
    # 1배속 (현실 시간) 채널 설정
    engine_channel = EngineConfigurationChannel()
    engine_channel.set_configuration_parameters(time_scale=1.0)
    
    # 환경 연결
    print("유니티 에디터에서 Play 버튼을 대기합니다...")
    env = UnityEnvironment(file_name=None, side_channels=[engine_channel])
    env.reset()
    
    behavior_names = list(env.behavior_specs.keys())
    behavior_name = behavior_names[0]
    
    print("플레이 테스트 시뮬레이션 시작! (종료하려면 Ctrl+C)")
    
    try:
        while True:
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            
            # 살아있는 에이전트가 있을 경우 행동 추론
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
            
            # 유니티 엔진 1스텝 진행
            env.step()

    except KeyboardInterrupt:
        print("\n플레이 테스트를 종료합니다.")
    finally:
        env.close()

if __name__ == '__main__':
    play()