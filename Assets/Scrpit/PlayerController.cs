using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private Animator animator;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float deadZone = 0.2f;
    [SerializeField] private float moveTime = 1f;
    [SerializeField] private float waitTime = 1f;

    private Rigidbody playerRigidbody;
    private Vector3 moveDirection;
    private float phaseTimer;
    private bool canMove = true;

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
            phaseTimer = 0f;
            canMove = true;
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

        animator.SetBool("isMoving", canMove);

        bool isLeft = moveDirection.x < -0.1f
            || (Mathf.Abs(moveDirection.x) <= 0.1f
                && moveDirection.z > 0f);

        animator.SetBool("isLeft", isLeft);
    }

    private void FixedUpdate()
    {
        if (moveDirection == Vector3.zero)
            return;

        phaseTimer += Time.fixedDeltaTime;

        if (canMove)
        {
            Vector3 nextPosition = playerRigidbody.position
                + moveDirection * speed * Time.fixedDeltaTime;

            playerRigidbody.MovePosition(nextPosition);

            if (phaseTimer >= moveTime)
            {
                canMove = false;
                phaseTimer = 0f;
                //animator.SetBool("isMoving", false);
            }
            
            
        }
        else if (phaseTimer >= waitTime)
        {
            canMove = true;
            phaseTimer = 0f;
        }
        
    }
}
