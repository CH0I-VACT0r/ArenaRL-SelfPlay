import numpy as np

class ReplayBuffer:
    def __init__(self, buffer_size=10240, batch_size=1024):
        self.buffer_size = buffer_size
        self.batch_size = batch_size
        
        # 각 데이터를 담을 배열 초기화
        self.obs = []
        self.move_actions = []
        self.skill_actions = []
        self.rewards = []
        self.log_probs = []
        self.values = []
        self.dones = []
        
        self.pointer = 0 # 버퍼 내 현재 위치

    def store(self, obs, move_action, skill_action, reward, log_prob, value, done):
        # 에이전트의 한 스텝 경험을 버퍼에 저장 (순환 큐 형태)
        if len(self.obs) < self.buffer_size:
            self.obs.append(obs)
            self.move_actions.append(move_action)
            self.skill_actions.append(skill_action)
            self.rewards.append(reward)
            self.log_probs.append(log_prob)
            self.values.append(value)
            self.dones.append(done)
        else:
            self.obs[self.pointer] = obs
            self.move_actions[self.pointer] = move_action
            self.skill_actions[self.pointer] = skill_action
            self.rewards[self.pointer] = reward
            self.log_probs[self.pointer] = log_prob
            self.values[self.pointer] = value
            self.dones[self.pointer] = done
            
        self.pointer = (self.pointer + 1) % self.buffer_size

    def sample_batch(self):
        # 학습에 사용할 무작위 미니배치 추출
        indices = np.random.choice(len(self.obs), self.batch_size, replace=False)
        
        batch_obs = np.array([self.obs[i] for i in indices])
        batch_move_actions = np.array([self.move_actions[i] for i in indices])
        batch_skill_actions = np.array([self.skill_actions[i] for i in indices])
        batch_rewards = np.array([self.rewards[i] for i in indices])
        batch_log_probs = np.array([self.log_probs[i] for i in indices])
        batch_values = np.array([self.values[i] for i in indices])
        batch_dones = np.array([self.dones[i] for i in indices])
        
        return (batch_obs, batch_move_actions, batch_skill_actions, batch_rewards, 
                batch_log_probs, batch_values, batch_dones)

    def clear(self):
        # PPO 업데이트 후, 버퍼 초기화 -> 새로운 경험 수집 준비
        self.obs = []
        self.move_actions = []
        self.skill_actions = []
        self.rewards = []
        self.log_probs = []
        self.values = []
        self.dones = []
        self.pointer = 0