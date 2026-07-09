using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class TelemetryManager : MonoBehaviour
{
    public static TelemetryManager Instance { get; private set; }

    private string filePath;
    private StringBuilder csvBuilder = new StringBuilder();

    private int currentEpisode = 0;

    // 클래스 식별자(ClassId)를 키로 사용하는 에피소드 종합 데이터
    private Dictionary<int, AgentCombatStats> combatStats = new Dictionary<int, AgentCombatStats>();
    private Dictionary<int, Dictionary<int, SkillStats>> skillStats = new Dictionary<int, Dictionary<int, SkillStats>>();

    // [전투 통계 원시 데이터]
    private class AgentCombatStats
    {
        public float damageTaken = 0f;
        public float survivalTime = 0f;
        public float distanceAccumulator = 0f;
        public int stepCount = 0;
        public int isWin = 0; // 1 = Win (Kill), 0 = Loss (Death)
    }

    // [스킬 통계 원시 데이터]
    private class SkillStats
    {
        public int castCount = 0;
        public int hitCount = 0;
        public float totalDamage = 0f;
        public int chargeAttempt = 0;
        public int chargeSuccess = 0;
        public int ccAttempt = 0;
        public int ccSuccess = 0;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeCSV();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCSV()
    {
        filePath = Path.Combine(Application.dataPath, "Arena_Telemetry.csv");

        if (!File.Exists(filePath))
        {
            string header = "Episode,ClassId,SurvivalTime,DamageTaken,AvgDistance,IsWin," +
                            "SkillId,CastCount,HitCount,TotalDamage,ChargeAttempt,ChargeSuccess,CcAttempt,CcSuccess\n";
            File.WriteAllText(filePath, header);
        }
    }

    private void EnsureAgentExists(int classId)
    {
        if (!combatStats.ContainsKey(classId))
            combatStats[classId] = new AgentCombatStats();
        if (!skillStats.ContainsKey(classId))
            skillStats[classId] = new Dictionary<int, SkillStats>();
    }

    private void EnsureSkillExists(int classId, int skillId)
    {
        EnsureAgentExists(classId);
        if (!skillStats[classId].ContainsKey(skillId))
            skillStats[classId][skillId] = new SkillStats();
    }

    // 1. 매 스텝(FixedUpdate) 호출하여 거리 및 생존 시간 누적
    public void RecordStep(int classId, float distance, float deltaTime)
    {
        EnsureAgentExists(classId);
        combatStats[classId].distanceAccumulator += distance;
        combatStats[classId].stepCount++;
        combatStats[classId].survivalTime += deltaTime;
    }

    // 2. 피해 기록
    public void RecordDamageTaken(int classId, float damage)
    {
        EnsureAgentExists(classId);
        combatStats[classId].damageTaken += damage;
    }

    // 3. 승패 기록
    public void RecordResult(int classId, bool isWin)
    {
        EnsureAgentExists(classId);
        combatStats[classId].isWin = isWin ? 1 : 0;
    }

    // 4. 스킬 시전 및 전술 기록
    public void RecordSkillCast(int classId, int skillId, bool isChargeSkill = false, bool isCcSkill = false)
    {
        EnsureSkillExists(classId, skillId);
        skillStats[classId][skillId].castCount++;
        if (isChargeSkill) skillStats[classId][skillId].chargeAttempt++;
        if (isCcSkill) skillStats[classId][skillId].ccAttempt++;
    }

    // 5. 차지 성공 기록 (차지가 끊기지 않고 발동되었을 때 호출)
    public void RecordChargeSuccess(int classId, int skillId)
    {
        EnsureSkillExists(classId, skillId);
        skillStats[classId][skillId].chargeSuccess++;
    }

    // 6. 스킬 적중 및 데미지 기록
    public void RecordSkillHit(int classId, int skillId, float damage, bool didApplyCc = false)
    {
        EnsureSkillExists(classId, skillId);
        skillStats[classId][skillId].hitCount++;
        skillStats[classId][skillId].totalDamage += damage;
        if (didApplyCc) skillStats[classId][skillId].ccSuccess++;
    }

    // 7. 에피소드 종료 시 데이터 추출 및 초기화
    public void ExportEpisodeData()
    {
        if (combatStats.Count == 0) return;

        currentEpisode++;
        csvBuilder.Clear();

        foreach (var classData in combatStats)
        {
            int classId = classData.Key;
            AgentCombatStats combat = classData.Value;
            float avgDistance = combat.stepCount > 0 ? combat.distanceAccumulator / combat.stepCount : 0f;

            if (skillStats.ContainsKey(classId) && skillStats[classId].Count > 0)
            {
                foreach (var skillData in skillStats[classId])
                {
                    int skillId = skillData.Key;
                    SkillStats s = skillData.Value;

                    csvBuilder.AppendLine($"{currentEpisode},{classId},{combat.survivalTime:F2},{combat.damageTaken:F2},{avgDistance:F2},{combat.isWin}," +
                                          $"{skillId},{s.castCount},{s.hitCount},{s.totalDamage:F2},{s.chargeAttempt},{s.chargeSuccess},{s.ccAttempt},{s.ccSuccess}");
                }
            }
            else
            {
                // 스킬을 한 번도 쓰지 않은 경우 더미 데이터 기록
                csvBuilder.AppendLine($"{currentEpisode},{classId},{combat.survivalTime:F2},{combat.damageTaken:F2},{avgDistance:F2},{combat.isWin}," +
                                      $"-1,0,0,0.00,0,0,0,0");
            }
        }

        if (csvBuilder.Length > 0)
        {
            File.AppendAllText(filePath, csvBuilder.ToString());
        }

        combatStats.Clear();
        skillStats.Clear();
    }
}