using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerInput playerInput;
    private Rigidbody playerRigidbody;
    private Animator playerAnimator;
    private float moveSpeed = 5f;
    private float defaultMoveSpeed;

    bool isMoving = false;

    public float MoveSpeed => moveSpeed;
    public float DefaultMoveSpeed => defaultMoveSpeed;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        defaultMoveSpeed = moveSpeed;
    }

    private void Update()
    {
        Vector3 input = playerInput.joystickInput;
        Vector3 movement;

        if (input.sqrMagnitude < 0.04f)
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
        moveSpeed = Mathf.Max(0f, speed);
    }

    public void ResetMoveSpeed()
    {
        moveSpeed = defaultMoveSpeed;
    }
}
