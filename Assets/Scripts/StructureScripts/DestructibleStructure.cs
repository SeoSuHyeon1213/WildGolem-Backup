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

    [Header("조각")]
    [SerializeField] private float scatterImpulse = 4f;   // 임시값: 부딪힌 방향으로 조각을 미는 힘
    [SerializeField] private float upwardImpulse = 1.5f;  // 임시값: 조각을 위로 띄우는 힘
    [SerializeField] private float randomImpulse = 1f;    // 임시값: 조각마다 다르게 흩어지도록 더하는 힘
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

    // breaker: 부순 쪽(Player). 흩어진 조각이 부순 쪽을 다시 밀거나 돌격 충돌로 잡히지 않도록 서로 충돌을 끈다.
    public void Break(Vector3 hitDirection, GameObject breaker = null)
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
        Collider[] pieceColliders = fracturedState.GetComponentsInChildren<Collider>();

        foreach (Collider piece in pieceColliders)
        {
            foreach (Collider other in breakerColliders)
                Physics.IgnoreCollision(piece, other);
        }

        foreach (Rigidbody piece in fracturedState.GetComponentsInChildren<Rigidbody>())
        {
            Vector3 impulse = hitDirection * scatterImpulse
                + Vector3.up * upwardImpulse
                + UnityEngine.Random.insideUnitSphere * randomImpulse;
            piece.AddForce(impulse, ForceMode.Impulse);
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
