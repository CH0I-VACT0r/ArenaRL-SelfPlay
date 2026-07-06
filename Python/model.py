import torch
import torch.nn as nn
from torch.distributions import Categorical

class ArenaPPOModel(nn.Module):
    def __init__(self, input_dim=30, hidden_dim=256, move_actions=9, skill_actions=5):
        super(ArenaPPOModel, self).__init__()
        
        # 공통 특징 추출기 (Feature Extractor)
        # 30차원의 관측 데이터를 받아 256차원으로 분석
        self.feature_layer = nn.Sequential(
            nn.Linear(input_dim, hidden_dim),
            nn.ReLU(),
            nn.Linear(hidden_dim, hidden_dim),
            nn.ReLU()
        )
        
        # Actor (행동 결정기)
        # 이동 행동 (0~8)의 확률(Logits)을 출력
        self.actor_move = nn.Linear(hidden_dim, move_actions)
        # 스킬 행동 (0~4)의 확률(Logits)을 출력
        self.actor_skill = nn.Linear(hidden_dim, skill_actions)
        
        # Critic (가치 평가)
        # 현재 상태가 얼마나 유리한지 점수(Value)로 출력합니다. (스칼라 값)
        self.critic = nn.Linear(hidden_dim, 1)

    def forward(self, x):
        # 신경망을 통과시켜 확률 분포의 근간이 되는 Logits와 Value 추출
        features = self.feature_layer(x)
        
        move_logits = self.actor_move(features)
        skill_logits = self.actor_skill(features)
        value = self.critic(features)
        
        return move_logits, skill_logits, value

    def get_action(self, obs):
        # 관측값(obs)을 받아 실제 유니티로 보낼 행동과 학습에 필요한 확률값을 반환
        move_logits, skill_logits, _ = self.forward(obs)
        
        # 각 행동에 대한 확률 분포 생성 (Categorical)
        move_dist = Categorical(logits=move_logits)
        skill_dist = Categorical(logits=skill_logits)
        
        # 확률 분포에 따라 실제 행동 샘플링 (예: 90% 확률이면 90% 확률로 해당 행동 선택)
        move_action = move_dist.sample()
        skill_action = skill_dist.sample()
        
        # 학습(Backprop)을 위해 선택된 행동의 로그 확률(Log Prob) 계산
        log_prob_move = move_dist.log_prob(move_action)
        log_prob_skill = skill_dist.log_prob(skill_action)
        
        # 행동 2개의 로그 확률을 더해서 하나의 스칼라 값으로 만듦
        total_log_prob = log_prob_move + log_prob_skill
        
        return move_action, skill_action, total_log_prob