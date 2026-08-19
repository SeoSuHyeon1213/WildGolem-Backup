using UnityEngine;

public class PartyFormationController : MonoBehaviour
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
        {
            GameObject playerObject = GameObject.FindWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        memberAnimators = new Animator[members.Length];

        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] != null)
                memberAnimators[i] = members[i].GetComponentInChildren<Animator>();
        }

        if (player != null)
            previousPlayerPosition = player.position;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 movement = player.position - previousPlayerPosition;
        movement.y = 0f;

        if (movement.sqrMagnitude > 0.000001f)
        {
            lastMoveDirection = movement.normalized;
            lastMovementTime = Time.time;
        }

        bool isMoving = Time.time - lastMovementTime <= animationStopDelay;

        MoveMembers();
        UpdateAnimations(isMoving);

        previousPlayerPosition = player.position;
    }

    private void MoveMembers()
    {
        int slotCount = Mathf.Min(members.Length, SlotDirections.Length);

        for (int i = 0; i < slotCount; i++)
        {
            if (members[i] == null)
                continue;

            members[i].position = player.position
                + SlotDirections[i] * formationDistance;
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

            if (!isMoving)
                continue;

            bool isLeft = lastMoveDirection.x < -0.01f
                || (Mathf.Abs(lastMoveDirection.x) <= 0.01f
                    && lastMoveDirection.z > 0f);

            animator.SetBool(IsLeftHash, isLeft);
        }
    }
}
