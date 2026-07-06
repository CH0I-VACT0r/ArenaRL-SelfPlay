import torch
import torch.nn as nn
import torch.optim as optim
import torch.nn.functional as F
from torch.distributions import Categorical
import numpy as np
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.base_env import ActionTuple
from torch.utils.tensorboard import SummaryWriter
import time
import os

from model import ArenaPPOModel
from replay_buffer import ReplayBuffer

MAX_EPISODES = 50000
MAX_STEPS_PER_EPISODE = 3000
BUFFER_SIZE = 10240
BATCH_SIZE = 1024
LEARNING_RATE = 3e-4
GAMMA = 0.99
CLIP_EPSILON = 0.2
K_EPOCHS = 3

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# ELO 연산 함수 ---------------
def calculate_expected_score(rating_a, rating_b):
    return 1 / (1 + 10 ** ((rating_b - rating_a) / 400))

def update_elo(rating_a, rating_b, score_a, k=32):
    # score_a: 1 (A 승), 0 (A 패), 0.5 (무승부)
    expected_a = calculate_expected_score(rating_a, rating_b)
    expected_b = calculate_expected_score(rating_b, rating_a)

    new_rating_a = rating_a + k * (score_a - expected_a)
    new_rating_b = rating_b + k * ((1 - score_a) - expected_b)
    
    return new_rating_a, new_rating_b
# -----------------------------

def compute_advantages(rewards, values, dones, gamma):
    advantages = []
    advantage = 0
    for i in reversed(range(len(rewards))):
        if dones[i]:
            advantage = 0
        advantage = rewards[i] + gamma * advantage
        advantages.insert(0, advantage)
        
    advantages = torch.tensor(advantages, dtype=torch.float32, device=device)
    advantages = (advantages - advantages.mean()) / (advantages.std() + 1e-8)
    return advantages

def update_model(model, optimizer, replay_buffer, k_epochs, clip_epsilon):
    (obs, move_actions, skill_actions, rewards, log_probs, values, dones) = replay_buffer.sample_batch()
    
    obs = torch.tensor(obs, dtype=torch.float32, device=device)
    move_actions = torch.tensor(move_actions, dtype=torch.long, device=device)
    skill_actions = torch.tensor(skill_actions, dtype=torch.long, device=device)
    old_log_probs = torch.tensor(log_probs, dtype=torch.float32, device=device)
    old_values = torch.tensor(values, dtype=torch.float32, device=device)
    
    advantages = compute_advantages(rewards, old_values, dones, GAMMA)
    targets = advantages + old_values

    total_actor_loss = 0
    total_critic_loss = 0

    for _ in range(k_epochs):
        move_logits, skill_logits, current_values = model.forward(obs)
        
        move_dist = Categorical(logits=move_logits)
        skill_dist = Categorical(logits=skill_logits)
        
        new_log_prob_move = move_dist.log_prob(move_actions)
        new_log_prob_skill = skill_dist.log_prob(skill_actions)
        
        total_new_log_prob = new_log_prob_move + new_log_prob_skill
        ratio = torch.exp(total_new_log_prob - old_log_probs)
        
        surr1 = ratio * advantages
        surr2 = torch.clamp(ratio, 1 - clip_epsilon, 1 + clip_epsilon) * advantages
        
        actor_loss = -torch.min(surr1, surr2).mean()
        critic_loss = F.mse_loss(current_values.squeeze(), targets)

        # 엔트로피 보너스 계산 (행동의 무작위성 유지)
        entropy_move = move_dist.entropy()
        entropy_skill = skill_dist.entropy()
        entropy = (entropy_move + entropy_skill).mean()
        
        entropy_coef = 0.01

        loss = actor_loss + 0.5 * critic_loss - (entropy_coef * entropy)
        
        optimizer.zero_grad()
        loss.backward()
        optimizer.step()
        
        total_actor_loss += actor_loss.item()
        total_critic_loss += critic_loss.item()

    replay_buffer.clear()
    return total_actor_loss / k_epochs, total_critic_loss / k_epochs

def run_train():
    writer = SummaryWriter("runs/Arena_SelfPlay")
    print(f"\n[시스템 초기화] 현재 학습에 할당된 연산 장치: {device}")
    
    model = ArenaPPOModel(input_dim=30, hidden_dim=256, move_actions=9, skill_actions=5).to(device)
    optimizer = optim.Adam(model.parameters(), lr=LEARNING_RATE)
    replay_buffer = ReplayBuffer(buffer_size=BUFFER_SIZE, batch_size=BATCH_SIZE)

    # ==========================================
    # 체크포인트 이어서 학습하기
    # ==========================================
    CHECKPOINT_PATH = "./ArenaPPO_Ep000.pt" # 경로 추가

    if os.path.exists(CHECKPOINT_PATH):
        print(f"[체크포인트 로드] {CHECKPOINT_PATH} 파일에서 이전 학습 가중치를 성공적으로 불러옴.")
        model.load_state_dict(torch.load(CHECKPOINT_PATH, map_location=device))
    else:
        print("[신규 학습] 체크포인트 파일을 찾을 수 없어 처음부터 학습을 시작함.")
    # ==========================================

    print("\n" + "="*60)
    print("파이썬 학습 서버 OPEN")
    print("유니티 에디터로 이동하여 [Play(▶)] 버튼")
    print("="*60 + "\n")
    
    env = UnityEnvironment(file_name=None, seed=42, timeout_wait=120)
    env.reset()
    
    behavior_names = list(env.behavior_specs.keys())
    if not behavior_names:
        print("[오류] 연결된 Agent를 찾을 수 없습니다.")
        env.close()
        return
        
    behavior_name = behavior_names[0]
    print(f"[연결 성공] 유니티 환경과 통신이 시작되었습니다.")
    
    # ELO 초기화
    elo_warrior = 1200.0
    elo_mage = 1200.0

    total_steps = 0
    for episode in range(MAX_EPISODES):
        env.reset()
        decision_steps, terminal_steps = env.get_steps(behavior_name)
        
        num_agents = len(decision_steps.agent_id)
        if num_agents == 0:
            continue
            
        episode_reward = np.zeros(num_agents) 
        episode_step = 0
        
        # 에피소드 시작 시 에이전트 인덱스와 클래스(전사=0, 마법사=1) 매핑
        class_map = {}
        for i, agent_id in enumerate(decision_steps.agent_id):
            class_map[i] = int(decision_steps.obs[1][i][0])

        is_episode_done = False

        while len(decision_steps) > 0:
            episode_step += 1
            is_timeout = (episode_step >= MAX_STEPS_PER_EPISODE)
            
            # 인덱스 [1]의 30차원 데이터 추출
            obs = torch.tensor(decision_steps.obs[1], dtype=torch.float32, device=device)
            
            with torch.no_grad():
                move_action, skill_action, log_prob = model.get_action(obs)
                _, _, value = model.forward(obs)
                value = value.squeeze(-1) 
            
            actions_np = np.column_stack((move_action.cpu().numpy(), skill_action.cpu().numpy()))
            action_tuple = ActionTuple(discrete=actions_np)
            
            env.set_actions(behavior_name, action_tuple)
            env.step() 
            
            next_decision_steps, next_terminal_steps = env.get_steps(behavior_name)
            
            # --- 타임아웃 승패 판정 로직 ---
            custom_rewards = {}
            if is_timeout and len(next_decision_steps.agent_id) == 2:
                next_obs = next_decision_steps.obs[1]
                hp_0 = next_obs[0][5]
                hp_1 = next_obs[1][5]
                
                if hp_0 > hp_1:
                    custom_rewards = {next_decision_steps.agent_id[0]: 1.0, next_decision_steps.agent_id[1]: -1.0}
                elif hp_1 > hp_0:
                    custom_rewards = {next_decision_steps.agent_id[0]: -1.0, next_decision_steps.agent_id[1]: 1.0}
                else:
                    custom_rewards = {next_decision_steps.agent_id[0]: 0.0, next_decision_steps.agent_id[1]: 0.0}

            # 현재 판단을 내린 에이전트(t)를 기준으로 상태 업데이트
            for agent_index, agent_id in enumerate(decision_steps.agent_id):
                reward = 0.0
                done = False
                
                if agent_id in next_decision_steps:
                    reward = next_decision_steps[agent_id].reward
                    if is_timeout:
                        reward = custom_rewards.get(agent_id, reward)
                        done = True
                elif agent_id in next_terminal_steps:
                    reward = next_terminal_steps[agent_id].reward
                    done = True
                else:
                    continue 
                    
                episode_reward[agent_index] += reward
                
                replay_buffer.store(
                    obs[agent_index].cpu().numpy(),
                    move_action[agent_index].cpu().numpy(),
                    skill_action[agent_index].cpu().numpy(),
                    reward,
                    log_prob[agent_index].cpu().numpy(),
                    value[agent_index].cpu().numpy(),
                    done 
                )
            
            decision_steps = next_decision_steps
            total_steps += num_agents 

            if len(replay_buffer.obs) >= BUFFER_SIZE:
                avg_actor_loss, avg_critic_loss = update_model(model, optimizer, replay_buffer, K_EPOCHS, CLIP_EPSILON)
                writer.add_scalar("Loss/Actor", avg_actor_loss, total_steps)
                writer.add_scalar("Loss/Critic", avg_critic_loss, total_steps)
            
            if is_timeout or len(next_terminal_steps.agent_id) > 0:
                is_episode_done = True
                break
                
        # 에피소드 종료 시 ELO 점수 갱신 및 텐서보드 기록
        if is_episode_done and len(class_map) == 2:
            warrior_idx = [k for k, v in class_map.items() if v == 0][0]
            mage_idx = [k for k, v in class_map.items() if v == 1][0]
            
            # 최종 누적 보상 비교
            if episode_reward[warrior_idx] > episode_reward[mage_idx]:
                score_warrior = 1.0
            elif episode_reward[warrior_idx] < episode_reward[mage_idx]:
                score_warrior = 0.0
            else:
                score_warrior = 0.5 # 무승부
                
            elo_warrior, elo_mage = update_elo(elo_warrior, elo_mage, score_warrior)
            writer.add_scalar("Metric/ELO_Warrior", elo_warrior, episode)
            writer.add_scalar("Metric/ELO_Mage", elo_mage, episode)
            writer.add_scalar("Reward/Episode_Warrior", episode_reward[warrior_idx], episode)
            writer.add_scalar("Reward/Episode_Mage", episode_reward[mage_idx], episode)
        
        if episode % 10 == 0:
            print(f"Ep {episode} | 전사 ELO: {elo_warrior:.1f} | 마법사 ELO: {elo_mage:.1f} | 진행 스텝: {episode_step}")
        
        if episode % 100 == 0:
            torch.save(model.state_dict(), f"./ArenaPPO_Ep{episode}.pt")
            
    env.close()
    writer.close()

if __name__ == '__main__':
    run_train()