// 파티원의 배치와 복합 콜라이더, 이동 애니메이션을 관리한다.
using UnityEngine;

public class PartyController1 : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] members = new Transform[8];
    [SerializeField] private float formationDistance = 1.5f;
    [SerializeField] private float animationStopDelay = 0.1f;

    private Animator[] memberAnimators;
    private Vector3 previousPlayerPosition;
    private Vector3 lastMoveDirection = Vector3.forward;
    private float lastMovementTime = float.NegativeInfinity;

    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int IsLeftHash = Animator.StringToHash("isLeft");

    private static readonly Vector3[] SlotDirections =
    {
        Vector3.forward,
        new Vector3(1f, 0f, 1f).normalized,
        Vector3.right,
        new Vector3(1f, 0f, -1f).normalized,
        Vector3.back,
        new Vector3(-1f, 0f, -1f).normalized,
        Vector3.left,
        new Vector3(-1f, 0f, 1f).normalized
    };

    private void Awake()
    {
        if (player == null)
            player = transform;

        if (!player.TryGetComponent(out Rigidbody _))
        {
            Debug.LogError("Player에 Rigidbody가 필요합니다.", player);
            enabled = false;
            return;
        }

        memberAnimators = new Animator[members.Length];
        int slotCount = Mathf.Min(members.Length, SlotDirections.Length);

        for (int i = 0; i < slotCount; i++)
        {
            Transform member = members[i];

            if (member == null)
                continue;

            memberAnimators[i] = member.GetComponentInChildren<Animator>();

            if (member.TryGetComponent(out Rigidbody memberRigidbody))
            {
                Debug.LogWarning(
                    $"파티원 '{member.name}'의 Rigidbody를 제거해야 NPC Collider가 Player의 복합 Collider로 동작합니다.",
                    memberRigidbody);
            }

            member.SetParent(player, true);
            member.localPosition = SlotDirections[i] * formationDistance;
        }

        previousPlayerPosition = player.position;
    }

    private void LateUpdate()
    {
        Vector3 movement = player.position - previousPlayerPosition;
        movement.y = 0f;

        if (movement.sqrMagnitude > 0.000001f)
        {
            lastMoveDirection = movement.normalized;
            lastMovementTime = Time.time;
        }

        KeepFormation();
        UpdateAnimations(Time.time - lastMovementTime <= animationStopDelay);
        previousPlayerPosition = player.position;
    }

    private void KeepFormation()
    {
        int slotCount = Mathf.Min(members.Length, SlotDirections.Length);

        for (int i = 0; i < slotCount; i++)
        {
            if (members[i] != null)
                members[i].localPosition = SlotDirections[i] * formationDistance;
        }
    }

    private void UpdateAnimations(bool isMoving)
    {
        for (int i = 0; i < memberAnimators.Length; i++)
        {
            Animator animator = memberAnimators[i];

            if (animator == null)
                continue;

            animator.SetBool(IsMovingHash, isMoving);
            animator.SetBool(IsLeftHash, isMoving && lastMoveDirection.x < -0.01f);
        }
    }
}
