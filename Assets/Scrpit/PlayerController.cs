using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private Animator animator;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float deadZone = 0.2f;

    private Rigidbody playerRigidbody;
    private Vector3 moveDirection;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        Vector2 input = new Vector2(
            joystick.Horizontal,
            joystick.Vertical
        );

        if (input.sqrMagnitude < deadZone * deadZone)
        {
            moveDirection = Vector3.zero;
            animator.SetBool("isMoving", false);
            return;
        }

        float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        float radians = snappedAngle * Mathf.Deg2Rad;

        moveDirection = new Vector3(
            Mathf.Sin(radians),
            0f,
            Mathf.Cos(radians)
        );

        

        if (moveDirection.x < 0f)
        {
            animator.SetBool("isMoving", true);
            animator.SetBool("isLeft", true);
        }
        else if (moveDirection.x > 0f)
        {   animator.SetBool("isMoving", true);
            animator.SetBool("isLeft", false);
        }
    }

    private void FixedUpdate()
    {
        if (moveDirection == Vector3.zero)
            return;

        Vector3 nextPosition = playerRigidbody.position
            + moveDirection * speed * Time.fixedDeltaTime;

        playerRigidbody.MovePosition(nextPosition);
    }
}
