using UnityEngine;

// 1. 기본 투사체 공격
public class Skill_MageBasic : SkillBase
{
    public Skill_MageBasic() { SkillId = 7; MaxCooldown = 1.0f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId);
        Vector2 targetDir = caster.GetFacingDirection(); // 기본값: 바라보는 방향

        if (caster.enemyTransform != null)
        {
            targetDir = (caster.enemyTransform.position - caster.transform.position).normalized;
        }

        string key = "BasicProjectile";
        GameObject projGo = SimplePoolManager.Spawn(key);
        projGo.transform.position = caster.transform.position;

        ProjectileHelper proj = projGo.GetComponent<ProjectileHelper>();
        if (proj == null) proj = projGo.AddComponent<ProjectileHelper>();

        // 대상 유무와 상관없이 해당 방향으로 발사
        proj.Initialize(key, caster, targetDir, 4.5f, 5f, false, SkillId);
        caster.Visualizer.DrawCircle(caster.transform.position, 0.3f, 0.1f, Color.cyan);

        CurrentCooldown = MaxCooldown;
    }
}

// 2. 텔레포트
public class Skill_Teleport : SkillBase
{
    public Skill_Teleport() { SkillId = 8; MaxCooldown = 5.0f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId);
        Vector2 dir = caster.GetFacingDirection();
        float dist = 2.0f;

        RaycastHit2D hit = Physics2D.Raycast(caster.transform.position, dir, dist, LayerMask.GetMask("Wall"));
        if (hit.collider != null) dist = hit.distance - 0.5f;

        Vector2 teleportPos = (Vector2)caster.transform.position + dir * dist;
        caster.rb.MovePosition(teleportPos);
        caster.Visualizer.DrawLine(caster.transform.position, teleportPos, 0.2f, Color.white);

        CurrentCooldown = MaxCooldown;
    }
}

// 3. AoE 기절
public class Skill_NovaStun : SkillBase
{
    public Skill_NovaStun() { SkillId = 9; MaxCooldown = 5.0f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId, true, true);
        caster.Visualizer.DrawCircle(caster.transform.position, 1.5f, 0.25f, new Color(1f, 0f, 0f, 0.2f));
        caster.StartCasting(0.25f, this);
        CurrentCooldown = MaxCooldown;
    }

    public override void OnCastComplete(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordChargeSuccess(caster.classId, SkillId);
        caster.Visualizer.DrawCircle(caster.transform.position, 1.5f, 0.2f, new Color(1f, 0f, 0f, 0.8f));
        Collider2D[] hits = Physics2D.OverlapCircleAll(caster.transform.position, 1.25f, LayerMask.GetMask("Agent"));

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

// 4. 장판 투사체
public class Skill_PoisonField : SkillBase
{
    public Skill_PoisonField() { SkillId = 10; MaxCooldown = 8.0f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId);
        Vector2 targetDir = caster.GetFacingDirection();
        float dist = 4.0f; // 기본 최대 사거리

        if (caster.enemyTransform != null)
        {
            targetDir = (caster.enemyTransform.position - caster.transform.position).normalized;
            dist = Vector2.Distance(caster.enemyTransform.position, caster.transform.position);
            dist = Mathf.Min(dist, 4.0f);
        }

        string key = "FieldProjectile";
        GameObject projGo = SimplePoolManager.Spawn(key);
        projGo.transform.position = caster.transform.position;

        ProjectileHelper proj = projGo.GetComponent<ProjectileHelper>();
        if (proj == null) proj = projGo.AddComponent<ProjectileHelper>();

        proj.Initialize(key, caster, targetDir, dist, 10f, true, SkillId);
        CurrentCooldown = MaxCooldown;
    }
}

// 5. 궤도 회전체
public class Skill_OrbitingSpheres : SkillBase
{
    public Skill_OrbitingSpheres() { SkillId = 11; MaxCooldown = 10.0f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId);
        string key = "OrbitSphere";
        for (int i = 0; i < 3; i++)
        {
            GameObject orbGo = SimplePoolManager.Spawn(key);
            orbGo.transform.position = caster.transform.position;

            OrbitingHelper orb = orbGo.GetComponent<OrbitingHelper>();
            if (orb == null) orb = orbGo.AddComponent<OrbitingHelper>();

            orb.Initialize(key, caster, i * 120f, SkillId);
        }
        CurrentCooldown = MaxCooldown;
    }
}

// 6. 메테오
public class Skill_Meteor : SkillBase
{
    private Vector2 targetPosAtCast;

    public Skill_Meteor() { SkillId = 12; MaxCooldown = 15.0f; }

    public override void Execute(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillCast(caster.classId, SkillId, true, true);
        if (caster.enemyTransform != null)
        {
            targetPosAtCast = caster.enemyTransform.position;
        }
        else
        {
            targetPosAtCast = (Vector2)caster.transform.position + caster.GetFacingDirection() * 2.5f;
        }

        caster.isDangerActive = true;
        caster.activeDangerCenter = targetPosAtCast;

        caster.Visualizer.DrawCircle(targetPosAtCast, 3.0f, 0.5f, new Color(1f, 0.5f, 0f, 0.3f));
        caster.StartCasting(1.5f, this);
        CurrentCooldown = MaxCooldown;
    }

    public override void OnCastComplete(ArenaAgent caster)
    {
        if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordChargeSuccess(caster.classId, SkillId);
        caster.Visualizer.DrawCircle(targetPosAtCast, 3.0f, 0.3f, new Color(1f, 0f, 0f, 0.9f));
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosAtCast, 3.0f, LayerMask.GetMask("Agent"));

        foreach (var hit in hits)
        {
            if (hit.gameObject != caster.gameObject)
            {
                ArenaAgent target = hit.GetComponent<ArenaAgent>();
                if (target != null)
                {
                    target.TakeDamage(60f, caster);
                    target.ApplyStun(1.5f);
                    if (TelemetryManager.Instance != null) TelemetryManager.Instance.RecordSkillHit(caster.classId, SkillId, 60f, true);
                }
            }
        }
    }
}