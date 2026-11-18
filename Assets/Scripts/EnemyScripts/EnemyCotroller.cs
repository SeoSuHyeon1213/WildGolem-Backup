// 적 AI: NavMeshAgent로 대상을 쫓고, 가까우면 공격 동작을 재생한다.
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

    private float stunnedUntil;

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
                bool stop = !hasTarget || inRange || Time.time < stunnedUntil;
                NavAgent.isStopped = stop;

                if (!stop)
                    NavAgent.SetDestination(Target.position);
            }

            yield return new WaitForSeconds(0.25f);
        }
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
