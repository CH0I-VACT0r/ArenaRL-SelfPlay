using UnityEngine;

public abstract class SkillBase
{
    public int SkillId { get; protected set; }
    public float MaxCooldown { get; protected set; }
    public float CurrentCooldown { get; set; }
    public Sprite SkillIcon { get; protected set; }
    public abstract void Execute(ArenaAgent caster);
    public virtual void OnCastComplete(ArenaAgent caster) { }

    public virtual void UpdateCooldown(float dt)
    {
        if (CurrentCooldown > 0) CurrentCooldown -= dt;
    }
}

public class Skill_BasicAttack : SkillBase
{
    private float attackRadius = 2.0f;
    private float attackAngle = 120f;
    private float damage = 20f;

    public Skill_BasicAttack() { 
        SkillId = 1; 
        MaxCooldown = 0.5f;
        SkillIcon = Resources.Load<Sprite>("SkillIcons/WarriorBasicAttack");
    }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId);
        Vector2 dir = caster.GetFacingDirection();
        Vector2 castPos = caster.transform.position;

        // 시각적 피드백
        caster.Visualizer.DrawCone(castPos, dir, attackRadius, attackAngle, 0.2f, new Color(1f, 0f, 0f, 0.4f));

        // 타격 판정 로직
        Collider2D[] hits = Physics2D.OverlapCircleAll(castPos, attackRadius, LayerMask.GetMask("Agent"));

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject != caster.gameObject)
            {
                Vector2 closestPoint = hit.ClosestPoint(castPos);
                Vector2 dirToClosest = closestPoint - castPos;

                if (dirToClosest.sqrMagnitude <= 0.001f)
                {
                    ApplyDamage(hit, caster);
                    continue;
                }

                dirToClosest.Normalize();
                float angleToClosest = Vector2.Angle(dir, dirToClosest);

                if (angleToClosest <= attackAngle / 2f)
                {
                    ApplyDamage(hit, caster);
                }
            }
        }
        CurrentCooldown = MaxCooldown;
    }

    private void ApplyDamage(Collider2D hit, ArenaAgent caster)
    {
        ArenaAgent target = hit.GetComponent<ArenaAgent>();
        if (target != null)
        {
            target.TakeDamage(damage, caster);
            if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillHit(caster.classId, SkillId, damage);
        }
    }
}

public class Skill_Dash : SkillBase
{
    private float dashDistance = 3.0f; // 돌진 거리
    private float dashDuration = 0.25f; // 돌진에 소요되는 시간
    private float damage = 10f;        // 돌진 경로 타격 데미지

    public Skill_Dash() { SkillId = 2; MaxCooldown = 2.5f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId);
        Vector2 dir = caster.GetFacingDirection();
        Vector2 startPos = caster.transform.position;

        // 물리적 돌진 상태 진입
        float speed = dashDistance / dashDuration;
        caster.StartDash(dashDuration, speed, dir);

        // 시각적 피드백: 돌진 궤적 표시
        caster.Visualizer.DrawLine(startPos, startPos + dir * dashDistance, dashDuration, Color.cyan);

        // 타격 판정
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            startPos,
            new Vector2(1.5f, 1.5f),
            angle,
            dir,
            dashDistance,
            LayerMask.GetMask("Agent")
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != caster.gameObject)
            {
                ArenaAgent target = hit.collider.GetComponent<ArenaAgent>();
                if (target != null)
                {
                    target.TakeDamage(damage, caster);
                    if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillHit(caster.classId, SkillId, damage);
                }
            }
        }

        CurrentCooldown = MaxCooldown;
    }
}

public class Skill_ChargeCC : SkillBase
{
    public Skill_ChargeCC() { SkillId = 3; MaxCooldown = 5.0f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId, true, true);
        caster.isDangerActive = true;
        caster.activeDangerCenter = caster.transform.position;

        caster.StartCasting(0.25f, this);
        CurrentCooldown = MaxCooldown;
    }

    public override void OnCastComplete(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordChargeSuccess(caster.classId, SkillId);
        Vector2 dir = caster.GetFacingDirection();
        Vector2 attackPos = (Vector2)caster.transform.position + dir * 1.5f;
        float radius = 1.5f;

        // 시각화: 원형 피드백
        caster.Visualizer.DrawCircle(attackPos, radius, 0.2f, new Color(0f, 0f, 1f, 0.4f));

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, radius, LayerMask.GetMask("Agent"));
        foreach (var hit in hits)
        {
            if (hit.gameObject != caster.gameObject)
            {
                ArenaAgent target = hit.GetComponent<ArenaAgent>();
                if (target != null)
                {
                    target.TakeDamage(30f, caster);
                    target.ApplyStun(1.0f);
                    if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillHit(caster.classId, SkillId, 30f, true);
                }
            }
        }
    }
}

public class Skill_Parry : SkillBase
{
    public Skill_Parry() { SkillId = 4; MaxCooldown = 4.0f; }
    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId);
        Debug.Log($"{caster.gameObject.name}: 4번 패링 준비 (0.3초 무적 대기)");
        caster.ActivateParry(0.3f);
        CurrentCooldown = MaxCooldown;
    }
}

public class Skill_CcImmuneBuff : SkillBase
{
    public Skill_CcImmuneBuff() { SkillId = 5; MaxCooldown = 8.0f; }
    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId);
        Debug.Log($"{caster.gameObject.name}: 5번 군중제어 면역 버프 (3초 지속)");
        caster.ActivateCcImmune(3.0f);
        CurrentCooldown = MaxCooldown;
    }
}

public class Skill_ChainPull : SkillBase
{
    public Skill_ChainPull() { SkillId = 6; MaxCooldown = 7.5f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId, true, true);

        caster.isDangerActive = true;
        caster.activeDangerCenter = caster.transform.position;

        caster.StartCasting(0.5f, this);
        CurrentCooldown = MaxCooldown;
    }

    public override void OnCastComplete(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordChargeSuccess(caster.classId, SkillId);
        Vector2 targetDir = (caster.enemyTransform.position - caster.transform.position).normalized;
        Vector2 castPos = caster.transform.position;
        float range = 4.0f;

        // 시각화: 사슬이 뻗어나가는 라인 피드백
        caster.Visualizer.DrawLine(castPos, castPos + targetDir * range, 0.3f, Color.magenta);

        // RaycastAll을 사용하여 경로상의 모든 대상을 찾음
        RaycastHit2D[] hits = Physics2D.RaycastAll(castPos, targetDir, range, LayerMask.GetMask("Agent"));

        foreach (RaycastHit2D hit in hits)
        {
            // 자신은 무시
            if (hit.collider != null && hit.collider.gameObject != caster.gameObject)
            {
                ArenaAgent target = hit.collider.GetComponent<ArenaAgent>();
                if (target != null)
                {
                    // 데미지 및 기절 적용
                    target.TakeDamage(50f, caster);
                    target.ApplyStun(1.0f);
                    if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillHit(caster.classId, SkillId, 50f, true);
                    // 강제 견인
                    Vector2 pullPos = castPos + targetDir * 1.0f;
                    target.rb.MovePosition(pullPos);
                    target.rb.velocity = Vector2.zero;

                    break;
                }
            }
        }
    }
}