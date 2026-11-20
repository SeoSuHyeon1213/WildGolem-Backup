using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerInput playerInput;
    private Rigidbody playerRigidbody;
    private Animator playerAnimator;
    private float moveSpeed = 5f;
    private float defaultMoveSpeed;

    bool isMoving = false;
    private Vector3 lastMoveDirection = Vector3.forward;
    private Vector3 forcedDirection;
    private bool hasForcedDirection;

    public float MoveSpeed => moveSpeed;
    public float DefaultMoveSpeed => defaultMoveSpeed;
    public bool IsMoving => isMoving;
    public Vector3 LastMoveDirection => lastMoveDirection;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        defaultMoveSpeed = moveSpeed;

        // 7방향 대기·걷기·공격·피격 스프라이트 재생. 씬에 붙어 있지 않으면 실행 중에 붙인다(RedUnit과 같은 방식).
        if (!TryGetComponent(out GolemMotionAnimator _))
            gameObject.AddComponent<GolemMotionAnimator>();
    }

    private void Update()
    {
        Vector3 input = playerInput.joystickInput;
        Vector3 movement;

        if (hasForcedDirection)
        {
            movement = forcedDirection;
        }
        else if (input.sqrMagnitude < 0.04f)
        {
            movement = Vector3.zero;
        }
        else
        {
            float angle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg;
            float snappedAngle = Mathf.Round(angle / 45f) * 45f;
            float radians = snappedAngle * Mathf.Deg2Rad;

            movement = new Vector3(
                Mathf.Sin(radians),
                0f,
                Mathf.Cos(radians)
            );
        }

        playerRigidbody.MovePosition(
            playerRigidbody.position
            + movement * moveSpeed * Time.deltaTime
        );

        isMoving = movement.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            lastMoveDirection = movement;
            playerAnimator.SetBool("isMoving", true);

            if (movement.x < 0)
                playerAnimator.SetBool("isLeft", true);
            else
                playerAnimator.SetBool("isLeft", false);
        }
        else
        {
            playerAnimator.SetBool("isMoving", false);
            playerAnimator.SetBool("isLeft", false);
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void ResetMoveSpeed()
    {
        moveSpeed = defaultMoveSpeed;
    }

    // 질주 돌격처럼 조이스틱 입력과 무관하게 한 방향으로만 이동시킬 때 사용한다.
    public void SetForcedDirection(Vector3 direction)
    {
        direction.y = 0f;
        forcedDirection = direction.normalized;
        hasForcedDirection = true;
    }

    public void ClearForcedDirection()
    {
        hasForcedDirection = false;
    }
}
