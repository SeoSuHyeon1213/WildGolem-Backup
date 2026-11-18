// 질주 돌격으로 부서지는 전투 구조물(벽·기둥 등).
// 모델은 같은 자리에 겹친 두 상태를 가진다(우편함 20261109-007 납품 구성).
//   <이름>_intact_state    : 온전한 모양. 처음에 보인다.
//   <이름>_fractured_state : 미리 조각낸 모양. 처음에는 꺼 두고, 부서질 때 켜서 조각을 물리로 흩뿌린다.
// 프리팹은 에디터 메뉴 Tools/Wild Golem/우편함 구조물 가져오기가 만든다(루트 BoxCollider, 조각별 Rigidbody·볼록 MeshCollider).
using System;
using System.Collections;
using UnityEngine;

public class DestructibleStructure : MonoBehaviour
{
    [SerializeField] private GameObject intactState;
    [SerializeField] private GameObject fracturedState;

    // 조각 속도는 질량과 무관하게 정한다(ForceMode.VelocityChange). 넓고 낮은 벽 조각도 기둥처럼 확실히 날아가게 하기 위함이다.
    [Header("조각 속도 (m/s, 모두 임시값)")]
    [SerializeField] private float launchSpeed = 4f;    // 부딪힌 방향으로
    [SerializeField] private float upwardSpeed = 2.5f;  // 위로
    [SerializeField] private float randomSpeed = 1.5f;  // 조각마다 다르게 흩어지도록 더하는 무작위 속도
    [SerializeField] private float maxSpinSpeed = 8f;   // 조각을 굴리는 최대 회전 속도(rad/s)

    [Header("부딪힌 지점과의 거리 (임시값)")]
    [SerializeField] private float nearMultiplier = 1.5f; // 부딪힌 지점에 가장 가까운 조각의 속도 배율
    [SerializeField] private float farMultiplier = 0.6f;  // 가장 먼 조각의 속도 배율

    [Header("돌격 속도 (임시값)")]
    [SerializeField] private float referenceImpactSpeed = 10f; // 이 속도로 부딪히면 배율 1(PlayerDash 최대 속도와 같은 값)
    [SerializeField] private float minImpactMultiplier = 0.75f;
    [SerializeField] private float maxImpactMultiplier = 1.5f;

    [SerializeField] private float debrisLifetime = 4f;   // 임시값: 부서진 뒤 조각이 사라지기까지의 시간(초). 0 이하면 남겨 둔다

    private Collider intactCollider;

    public bool IsBroken { get; private set; }
    public event Action onBroken;

    private void Awake()
    {
        intactCollider = GetComponent<Collider>();

        if (fracturedState != null)
            fracturedState.SetActive(false);
    }

    public void Break(Vector3 hitDirection, GameObject breaker = null)
    {
        Break(hitDirection, transform.position + Vector3.up, referenceImpactSpeed, breaker);
    }

    // hitPoint: 부딪힌 지점(가까운 조각일수록 세게 날아간다). impactSpeed: 부딪힐 때 속도(빠를수록 세게 부서진다).
    // breaker: 부순 쪽(Player). 흩어진 조각이 부순 쪽을 다시 밀거나 돌격 충돌로 잡히지 않도록 서로 충돌을 끈다.
    public void Break(Vector3 hitDirection, Vector3 hitPoint, float impactSpeed, GameObject breaker = null)
    {
        if (IsBroken)
            return;

        IsBroken = true;

        if (intactCollider != null)
            intactCollider.enabled = false;

        if (intactState != null)
            intactState.SetActive(false);

        if (fracturedState == null)
        {
            onBroken?.Invoke();
            return;
        }

        fracturedState.SetActive(true);

        hitDirection.y = 0f;
        hitDirection = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector3.zero;

        Collider[] breakerColliders = breaker != null ? breaker.GetComponentsInChildren<Collider>() : Array.Empty<Collider>();

        foreach (Collider piece in fracturedState.GetComponentsInChildren<Collider>())
        {
            foreach (Collider other in breakerColliders)
                Physics.IgnoreCollision(piece, other);
        }

        Rigidbody[] pieces = fracturedState.GetComponentsInChildren<Rigidbody>();
        float impactMultiplier = Mathf.Clamp(impactSpeed / Mathf.Max(referenceImpactSpeed, 0.01f), minImpactMultiplier, maxImpactMultiplier);

        float nearest = float.MaxValue;
        float farthest = 0f;

        foreach (Rigidbody piece in pieces)
        {
            float distance = Vector3.Distance(piece.worldCenterOfMass, hitPoint);
            nearest = Mathf.Min(nearest, distance);
            farthest = Mathf.Max(farthest, distance);
        }

        foreach (Rigidbody piece in pieces)
        {
            float t = farthest > nearest ? Mathf.InverseLerp(nearest, farthest, Vector3.Distance(piece.worldCenterOfMass, hitPoint)) : 0f;
            float multiplier = Mathf.Lerp(nearMultiplier, farMultiplier, t) * impactMultiplier;

            Vector3 velocity = hitDirection * launchSpeed
                + Vector3.up * upwardSpeed
                + UnityEngine.Random.insideUnitSphere * randomSpeed;

            piece.AddForce(velocity * multiplier, ForceMode.VelocityChange);
            piece.AddTorque(UnityEngine.Random.insideUnitSphere * maxSpinSpeed * multiplier, ForceMode.VelocityChange);
        }

        onBroken?.Invoke();

        if (debrisLifetime > 0f)
            StartCoroutine(RemoveDebris());
    }

    private IEnumerator RemoveDebris()
    {
        yield return new WaitForSeconds(debrisLifetime);
        fracturedState.SetActive(false);
    }

    [ContextMenu("부수기 (테스트)")]
    private void BreakForTest()
    {
        Break(transform.forward);
    }
}
