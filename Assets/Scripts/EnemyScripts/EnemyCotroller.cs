// 적 AI: NavMeshAgent로 대상까지의 경로를 구하고, 가까우면 공격 동작을 재생한다.
// 이동은 Player와 같이 8방향(45° 단위)으로만 한다. 에이전트의 자동 이동은 끄고(isStopped),
// 경로의 다음 꺾임점 방향을 45° 단위로 맞춰 NavMeshAgent.Move로 직접 옮긴다(NavMesh 밖으로 나가지 않는다).
// 피해는 LivingEntity.TakeDamage 경로로 받는다(PlayerDash.DealDamage가 이 경로를 쓴다).
// 스프라이트 동작(GolemMotionAnimator)이 있으면 이동·공격·피격·사망을 재생한다.
// 대상이 지정되지 않으면 씬의 Player(PlayerMovement)를 쫓는다. Player는 아직 체력(LivingEntity)이 없어
// 이 경우 공격은 동작만 재생하고 피해를 주지 않는다.
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyCotroller : LivingEntity{
    public LayerMask whatIsPlayer;

    private LivingEntity targetEntity;
    private Transform targetTransform;

    private NavMeshAgent NavAgent;
    private GolemMotionAnimator motionAnimator;

    public float damage = 1f;
    public float timeBetweenAttacks = 1f;
    public float lastAttackTime = 0.5f;

    [SerializeField] private float attackRange = 1.2f;      // 임시값: 이 거리 안이면 멈춰서 공격한다
    [SerializeField] private float hitStunTime = 0.3f;      // 임시값: 피격 후 멈추는 시간(초)
    [SerializeField] private float corpseLifetime = 3f;     // 임시값: 사망 후 사라지기까지 시간(초). 0 이하면 남겨 둔다
    // 임시값: 방향을 바꾸는 여유 각도. 목표 방향이 지금 방향에서 22.5°+이 값 넘게 벗어나야 바꾼다.
    // 경계 각도에서 매 프레임 방향이 바뀌어 떨리는 것을 막는다(대신 계단 모양으로 걷는다).
    [SerializeField] private float directionHysteresis = 10f;

    private float stunnedUntil;
    private bool chasing;
    private Vector3 moveDirection;

    private bool hasTarget
    {
        get
        {
            if (targetEntity != null)
                return !targetEntity.dead;

            return targetTransform != null;
        }
    }

    private Transform Target => targetEntity != null ? targetEntity.transform : targetTransform;

    private void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        motionAnimator = GetComponent<GolemMotionAnimator>();

        // 스프라이트는 카메라를 향한 채로 두고 방향은 GolemMotionAnimator가 그림으로 바꾼다.
        if (NavAgent != null)
            NavAgent.updateRotation = false;
    }

    public void SetUp(LivingEntity target)
    {
        targetEntity = target;
    }

    private void Start()
    {
        if (targetEntity == null && targetTransform == null)
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

            if (player != null)
                targetTransform = player.transform;
        }

        StartCoroutine(UpdatePath());
    }

    void Update()
    {
        if (dead || !hasTarget || Time.time < stunnedUntil)
            return;

        MoveAlongPath();

        Vector3 toTarget = Target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude > attackRange || Time.time - lastAttackTime < timeBetweenAttacks)
            return;

        lastAttackTime = Time.time;

        if (motionAnimator != null)
            motionAnimator.PlayAttack(toTarget);

        if (targetEntity != null)
            targetEntity.TakeDamage(damage);
    }

    private IEnumerator UpdatePath()
    {
        while(!dead)
        {
            if (NavAgent != null && NavAgent.isOnNavMesh)
            {
                bool inRange = hasTarget && Vector3.Distance(Target.position, transform.position) <= attackRange;
                chasing = hasTarget && !inRange && Time.time >= stunnedUntil;

                // 에이전트는 경로 계산만 한다. 실제 이동은 MoveAlongPath가 8방향으로 한다.
                NavAgent.isStopped = true;

                if (chasing)
                    NavAgent.SetDestination(Target.position);
            }

            yield return new WaitForSeconds(0.25f);
        }
    }

    private void MoveAlongPath()
    {
        if (!chasing || NavAgent == null || !NavAgent.isOnNavMesh || NavAgent.pathPending)
            return;

        Vector3[] corners = NavAgent.path.corners;

        if (corners.Length < 2)
            return;

        // 경로의 첫 점은 현재 위치이므로 그다음 꺾임점을 향한다. 꺾임점에 거의 닿았으면 그다음 점을 쓴다.
        Vector3 position = NavAgent.nextPosition;
        Vector3 toCorner = Vector3.zero;

        for (int i = 1; i < corners.Length; i++)
        {
            toCorner = corners[i] - position;
            toCorner.y = 0f;

            if (toCorner.sqrMagnitude > 0.01f)
                break;
        }

        if (toCorner.sqrMagnitude <= 0.0001f)
            return;

        float targetAngle = Mathf.Atan2(toCorner.x, toCorner.z) * Mathf.Rad2Deg;

        if (moveDirection == Vector3.zero ||
            Mathf.Abs(Mathf.DeltaAngle(targetAngle, Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg)) > 22.5f + directionHysteresis)
        {
            float snapped = Mathf.Round(targetAngle / 45f) * 45f * Mathf.Deg2Rad;
            moveDirection = new Vector3(Mathf.Sin(snapped), 0f, Mathf.Cos(snapped));
        }

        // 마지막 꺾임점(목적지)을 지나치지 않게 남은 거리만큼만 움직인다.
        float step = Mathf.Min(NavAgent.speed * Time.deltaTime, toCorner.magnitude);
        NavAgent.Move(moveDirection * step);
    }

    public override void TakeDamage(float damage)
    {
        if (dead)
            return;

        base.TakeDamage(damage);

        if (dead)
            return;

        stunnedUntil = Time.time + hitStunTime;

        if (NavAgent != null && NavAgent.isOnNavMesh)
            NavAgent.isStopped = true;

        if (motionAnimator != null)
            motionAnimator.PlayHit();
    }

    protected override void Die()
    {
        base.Die();

        if (NavAgent != null)
        {
            if (NavAgent.isOnNavMesh)
                NavAgent.isStopped = true;

            NavAgent.enabled = false;
        }

        // 쓰러진 적이 길을 막거나 다시 돌격 충돌을 일으키지 않게 한다.
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (motionAnimator != null)
            motionAnimator.PlayDeath();

        if (corpseLifetime > 0f)
            Destroy(gameObject, corpseLifetime);
    }
}
