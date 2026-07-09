using UnityEngine;

// 1. 투사체 스킬용 헬퍼
public class ProjectileHelper : MonoBehaviour
{
    private int skillId;
    public float speed = 10f;
    public float damage = 10f;
    public float maxDistance = 4.5f;
    public ArenaAgent caster;
    public bool leaveFieldOnHit = false;

    private Vector2 startPos;
    private Vector2 direction;
    private string poolKey;
    private float hitRadius = 0.4f;


    public void Initialize(string _poolKey, ArenaAgent _caster, Vector2 _dir, float _dist, float _dmg, bool _leaveField, int _skillId)
    {
        poolKey = _poolKey;
        caster = _caster;
        direction = _dir.normalized; // 방향 저장
        maxDistance = _dist;
        damage = _dmg;
        leaveFieldOnHit = _leaveField;
        startPos = transform.position;

        if (speed <= 0f) speed = 10f;

        skillId = _skillId;
    }

    private void FixedUpdate()
    {
        // 좌표 이동
        Vector2 nextPos = (Vector2)transform.position + (direction * speed * Time.fixedDeltaTime);
        transform.position = nextPos;

        // 충돌 판정
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius, LayerMask.GetMask("Agent", "Wall"));
        foreach (Collider2D hit in hits)
        {
            ArenaAgent target = hit.GetComponent<ArenaAgent>();

            // 1. 시전자 본인이면 무시 (root 대신 정확한 객체 비교)
            if (target != null && target == caster) continue;

            // 2. 벽 충돌
            if (hit.CompareTag("Wall"))
            {
                TriggerImpact();
                return;
            }

            // 3. 적 타격
            if (target != null)
            {
                target.TakeDamage(damage, caster);

                if (TelemetryManager.Instance != null)
                    TelemetryManager.Instance.RecordSkillHit(caster.classId, skillId, damage, false);

                TriggerImpact();
                return;
            }
        }

        // 사거리 초과 시 자동 소멸 로직
        if (Vector2.Distance(startPos, transform.position) >= maxDistance)
        {
            TriggerImpact();
        }
    }

    private void TriggerImpact()
    {
        // 초기화되지 않은 비정상 객체 강제 파괴
        if (string.IsNullOrEmpty(poolKey))
        {
            Destroy(gameObject);
            return;
        }

        if (leaveFieldOnHit)
        {
            GameObject fieldGo = SimplePoolManager.Spawn("DamageField");
            fieldGo.transform.position = transform.position;

            DamageFieldHelper field = fieldGo.GetComponent<DamageFieldHelper>();
            if (field == null) field = fieldGo.AddComponent<DamageFieldHelper>();
            field.Initialize("DamageField", caster, 2.0f, 5f, skillId);
        }
        SimplePoolManager.ReturnToPool(poolKey, gameObject);
    }
}

// 2. 장판 스킬용 헬퍼
public class DamageFieldHelper : MonoBehaviour
{
    public ArenaAgent caster { get; private set; }
    private float durationTimer;
    private float tickDamage;
    private float tickTimer = 0.5f;
    private string poolKey;
    private float fieldRadius = 1.5f;
    private int skillId;

    public void Initialize(string _poolKey, ArenaAgent _caster, float _duration, float _tickDamage, int _skillId)
    {
        poolKey = _poolKey;
        caster = _caster;
        durationTimer = _duration;
        tickDamage = _tickDamage;
        tickTimer = 0.5f; // 재사용 타이머
        VisualEffectHelper.CreateShape(transform.position, Vector2.right, fieldRadius, 360f, _duration, new Color(0, 1, 0, 0.3f));
        skillId = _skillId;
    }

    private void FixedUpdate()
    {
        durationTimer -= Time.fixedDeltaTime;
        if (durationTimer <= 0f)
        {
            SimplePoolManager.ReturnToPool(poolKey, gameObject);
            return;
        }

        tickTimer -= Time.fixedDeltaTime;
        if (tickTimer <= 0f)
        {
            // 범위 내 적 검색 후 데미지
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, fieldRadius, LayerMask.GetMask("Agent"));
            foreach (Collider2D hit in hits)
            {
                ArenaAgent target = hit.GetComponent<ArenaAgent>();

                if (target != null && target != caster)
                {
                    target.TakeDamage(tickDamage, caster);

                    if (TelemetryManager.Instance != null)
                        TelemetryManager.Instance.RecordSkillHit(caster.classId, skillId, tickDamage, false);
                
                }
            }
        }
    }
}

    // 3. 궤도 회전체 헬퍼
    public class OrbitingHelper : MonoBehaviour
    {
        public ArenaAgent caster { get; private set; }
        private float durationTimer;
        private float angleOffset;
        private float radius = 1.5f;
        private float rotationSpeed = 360f;
        private string poolKey;
        private float hitRadius = 0.3f;
        private int skillId;

        public void Initialize(string _poolKey, ArenaAgent _caster, float _angleOffset, int _skillId)
        {
            poolKey = _poolKey;
            caster = _caster;
            angleOffset = _angleOffset;
            durationTimer = 2.0f;
            skillId = _skillId;
    }

        private void FixedUpdate()
        {
            if (caster == null)
            {
                SimplePoolManager.ReturnToPool(poolKey, gameObject);
                return;
            }

            durationTimer -= Time.fixedDeltaTime;
            if (durationTimer <= 0f)
            {
                SimplePoolManager.ReturnToPool(poolKey, gameObject);
                return;
            }

            // 회전 이동 계산
            angleOffset += rotationSpeed * Time.fixedDeltaTime;
            float rad = angleOffset * Mathf.Deg2Rad;
            transform.position = (Vector2)caster.transform.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

            // 시각적 피드백
            if (caster.Visualizer != null) caster.Visualizer.DrawCircle(transform.position, hitRadius, 0.1f, Color.magenta);

            // 수동 충돌 판정
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius, LayerMask.GetMask("Agent"));
            foreach (Collider2D hit in hits)
            {
                ArenaAgent target = hit.GetComponent<ArenaAgent>();

                // 타겟이 존재하고, 그 타겟이 시전자 본인이 아닐 때만 데미지!
                if (target != null && target != caster)
                {
                    target.TakeDamage(10f, caster);

                    if (TelemetryManager.Instance != null)
                    TelemetryManager.Instance.RecordSkillHit(caster.classId, skillId, 10f, false);

                    SimplePoolManager.ReturnToPool(poolKey, gameObject);
                    return;
                }
            }
        }
    }