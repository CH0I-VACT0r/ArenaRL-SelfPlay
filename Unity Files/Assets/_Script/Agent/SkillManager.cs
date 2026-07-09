using UnityEngine;

public class SkillManager : MonoBehaviour
{
    // 장착된 3개의 스킬 객체를 담는 배열
    public SkillBase[] equippedSkills = new SkillBase[4];
    private ArenaAgent agent;

    public void Initialize(ArenaAgent _agent, int[] skillIds)
    {
        agent = _agent;

        // Python에서 전달된 3개의 ID를 바탕으로 객체 생성 매핑
        for (int i = 0; i < 4; i++)
        {
            equippedSkills[i] = CreateSkillById(skillIds[i]);
        }
    }

    private SkillBase CreateSkillById(int id)
    {
        switch (id)
        {
            // 전사 스킬 (1 ~ 6)
            case 1: return new Skill_BasicAttack();
            case 2: return new Skill_Dash();
            case 3: return new Skill_ChargeCC();
            case 4: return new Skill_Parry();
            case 5: return new Skill_CcImmuneBuff();
            case 6: return new Skill_ChainPull();

            // 마법사 스킬 (7 ~ 12)
            case 7: return new Skill_MageBasic();
            case 8: return new Skill_Teleport();
            case 9: return new Skill_NovaStun();
            case 10: return new Skill_PoisonField();
            case 11: return new Skill_OrbitingSpheres();
            case 12: return new Skill_Meteor();
            default: return new Skill_BasicAttack(); // Fallback
        }
    }

    public void UpdateCooldowns(float dt)
    {
        foreach (var skill in equippedSkills)
        {
            if (skill != null) skill.UpdateCooldown(dt);
        }
    }

    public void TryExecuteSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;

        SkillBase skill = equippedSkills[slotIndex];
        if (skill == null) return;

        if (skill.CurrentCooldown <= 0f)
        {
            // 행동 비용 페널티
            agent.AddReward(-0.005f);

            // 조준 정렬 보상 계산
            if (agent.enemyTransform != null)
            {
                // 적을 향하는 정규화된 방향 벡터
                Vector2 dirToEnemy = (agent.enemyTransform.position - agent.transform.position).normalized;
                // 에이전트가 현재 바라보고 있는 방향 벡터
                Vector2 agentForward = agent.GetFacingDirection();

                // 두 벡터의 내적 (1.0 = 완벽히 같은 방향, 0 = 수직, -1 = 반대 방향)
                float dotProduct = Vector2.Dot(agentForward, dirToEnemy);

                // 내적 0.9 이상으로 정밀하게 조준했을 때 가산점 부여
                if (dotProduct >= 0.9f)
                {
                    agent.AddReward(0.002f);
                }
            }
            skill.Execute(agent);
        }
        else
        {
            agent.AddReward(-0.005f);
        }
    }

    // 관측(Observation)을 위한 쿨타임 가용 상태 반환 (0.0 또는 1.0)
    public float GetSkillReadyStatus(int slotIndex)
    {
        if (equippedSkills[slotIndex] == null) return 0f;
        return equippedSkills[slotIndex].CurrentCooldown <= 0f ? 1f : 0f;
    }
}
