using UnityEngine;
using Unity.MLAgents;

public class ArenaEnvironment : MonoBehaviour
{
    public static ArenaEnvironment Instance { get; private set; }

    [Header("Agents")]
    public ArenaAgent warriorAgent;
    public ArenaAgent mageAgent;

    [Header("Sudden Death System")]
    private float gameTimer = 0f;
    private float dotDamageTimer = 0f;
    private const float SUDDEN_DEATH_START_TIME = 40f; // 40초 이후 활성화
    private const float DOT_DAMAGE_INTERVAL = 1.0f;    // 1초 주기
    private const float DAMAGE_PERCENT = 0.05f;       // 최대 체력의 5%
    private bool isEpisodeActive = false;

    [Header("Current Balance Stats (Read Only)")]
    public float warriorMaxHp;
    public float mageMaxHp;
    public float warriorDamageMultiplier;
    public float mageDamageMultiplier;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 파이썬(Optuna) 환경에서 env.reset()이 호출될 때마다 새로운 파라미터를 받아옴.
        Academy.Instance.OnEnvironmentReset += ResetEnvironment;

        // 에디터 실행 시 초기값 세팅을 위해 한 번 수동 호출
        ResetEnvironment();
    }

    private void ResetEnvironment()
    {
        UpdateBalanceParameters();
        gameTimer = 0f;
        dotDamageTimer = 0f;
        isEpisodeActive = true;
    }

    private void FixedUpdate()
    {
        if (!isEpisodeActive) return;

        gameTimer += Time.fixedDeltaTime;

        // 40초 경과 시점부터 도트 대미지
        if (gameTimer >= SUDDEN_DEATH_START_TIME)
        {
            dotDamageTimer += Time.fixedDeltaTime;
            if (dotDamageTimer >= DOT_DAMAGE_INTERVAL)
            {
                ApplySuddenDeathDamage();
                dotDamageTimer = 0f;
            }
        }
    }

    private float RoundToFive(float value)
    {
        return Mathf.Round(value / 5f) * 5f;
    }


    private void UpdateBalanceParameters()
    {
        // 파이썬에서 전달한 수치 읽기 (값이 없으면 뒤의 기본값 200, 1.0 적용)
        float rawWarriorHp = Academy.Instance.EnvironmentParameters.GetWithDefault("warrior_hp", 200f);
        float rawMageHp = Academy.Instance.EnvironmentParameters.GetWithDefault("mage_hp", 200f);
        float rawWarriorDmg = Academy.Instance.EnvironmentParameters.GetWithDefault("warrior_dmg_mult", 1.0f);
        float rawMageDmg = Academy.Instance.EnvironmentParameters.GetWithDefault("mage_dmg_mult", 1.0f);

        // 난수 및 소수점을 5의 배수 단위로 저장
        warriorMaxHp = RoundToFive(rawWarriorHp);
        mageMaxHp = RoundToFive(rawMageHp);

        // 데미지 배율은 소수점 둘째 자리까지만 유지 (예: 1.05, 1.10)
        warriorDamageMultiplier = Mathf.Round(rawWarriorDmg * 20f) / 20f;
        mageDamageMultiplier = Mathf.Round(rawMageDmg * 20f) / 20f;

        // 에이전트 체력 업데이트 (새로운 에피소드가 시작될 때 반영)
        if (warriorAgent != null) warriorAgent.maxHp = warriorMaxHp;
        if (mageAgent != null) mageAgent.maxHp = mageMaxHp;

        Debug.Log($"[ArenaEnvironment] Balance Updated - Warrior HP: {warriorMaxHp} (Dmg: {warriorDamageMultiplier}), Mage HP: {mageMaxHp} (Dmg: {mageDamageMultiplier})");
    }

   
    private void ApplySuddenDeathDamage()
    {
        if (warriorAgent == null || mageAgent == null) return;

        // 대미지 적용 전 직전 프레임의 정확한 체력 확보
        float warriorPreHp = warriorAgent.currentHp;
        float magePreHp = mageAgent.currentHp;

        float warriorDot = warriorAgent.maxHp * DAMAGE_PERCENT;
        float mageDot = mageAgent.maxHp * DAMAGE_PERCENT;

        // 환경 대미지이므로 attacker는 null로 전달
        warriorAgent.TakeDamage(warriorDot, null);
        mageAgent.TakeDamage(mageDot, null);

        // 동일 프레임 내 동시 사망 여부 정밀 검사
        if (warriorAgent.currentHp <= 0 && mageAgent.currentHp <= 0)
        {
            isEpisodeActive = false;
            ResolveSimultaneousDeath(warriorPreHp, magePreHp);
        }
    }

    private void ResolveSimultaneousDeath(float warriorHp, float mageHp)
    {
        // 직전 프레임 체력 비교를 통한 판정승 처리
        if (warriorHp > mageHp)
        {
            // 전사 판정승
            warriorAgent.AddReward(1.0f);
            mageAgent.AddReward(-1.0f);
        }
        else if (mageHp > warriorHp)
        {
            // 마법사 판정승
            mageAgent.AddReward(1.0f);
            warriorAgent.AddReward(-1.0f);
        }
        else
        {
            // 이론상 완벽히 일치할 경우 (진짜 무승부) 페널티 없이 종료
            warriorAgent.AddReward(0.0f);
            mageAgent.AddReward(0.0f);
        }

        // Telemetry 기록 처리
        if (TelemetryManager.Instance != null)
        {
            TelemetryManager.Instance.RecordResult(0, warriorHp > mageHp);
            TelemetryManager.Instance.RecordResult(1, mageHp > warriorHp);
        }

        warriorAgent.EndEpisode();
        mageAgent.EndEpisode();
    }
}