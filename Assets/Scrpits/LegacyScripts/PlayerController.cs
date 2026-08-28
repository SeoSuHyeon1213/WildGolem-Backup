// using UnityEngine;
// using UnityEngine.InputSystem;

// public class PlayerController : MonoBehaviour
// {
//     //[SerializeField] private Joystick joystick;
//     [SerializeField] private Animator animator;

//     [Header("Movement")]
//     [SerializeField] private float speed = 5f;
//     //[SerializeField] private float deadZone = 0.2f;
//     [SerializeField] private float moveTime = 0.5f;
//     [SerializeField] private float waitTime = 0.5f;

//     [Header("Dash")]
//     [SerializeField] private float dashSpeed = 16f;
//     [SerializeField] private float dashBuildTime = 3f;
//     [SerializeField] private float dashReleaseTime = 1f;

//     private Rigidbody playerRigidbody;
//     private Vector3 moveDirection;
//     private float phaseTimer;
//     private float dashAmount;
//     private bool canMove = true;

//     private bool IsDashing => dashAmount >= 0.999f;

//     private void Awake()
//     {
//         playerRigidbody = GetComponent<Rigidbody>();
//         playerInput = GetComponent<PlayerInput>();

//         if (animator == null)
//             animator = GetComponent<Animator>();
//     }

//     private void Update()
//     {
//         Vector2 input = new Vector2(
//             PlayerInput.joystick.Horizontal,
//             PlayerInput.joystick.Vertical
//         );

//         bool hasInput = input.sqrMagnitude >= deadZone * deadZone;
//         UpdateDash(hasInput);

//         if (!hasInput)
//         {
//             moveDirection = Vector3.zero;
//             phaseTimer = 0f;
//             canMove = true;
//             animator.SetBool("isMoving", false);
//             return;
//         }

//         float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
//         float snappedAngle = Mathf.Round(angle / 45f) * 45f;
//         float radians = snappedAngle * Mathf.Deg2Rad;

//         moveDirection = new Vector3(
//             Mathf.Sin(radians),
//             0f,
//             Mathf.Cos(radians)
//         );

//         animator.SetBool("isMoving", canMove || IsDashing);

//         bool isLeft = moveDirection.x < -0.1f
//             || (Mathf.Abs(moveDirection.x) <= 0.1f
//                 && moveDirection.z > 0f);

//         animator.SetBool("isLeft", isLeft);
//     }

//     private void UpdateDash(bool hasInput)
// {
//     bool isCharging = hasInput
//         && Keyboard.current != null
//         && Keyboard.current.fKey.isPressed;

//     float targetAmount = isCharging ? 1f : 0f;
//     float duration = isCharging ? dashBuildTime : dashReleaseTime;
//     float changeSpeed = 1f / Mathf.Max(duration, 0.01f);

//     dashAmount = Mathf.MoveTowards(
//         dashAmount,
//         targetAmount,
//         changeSpeed * Time.deltaTime
//     );

//     if (IsDashing)
//     {
//         canMove = true;
//         phaseTimer = 0f;
//     }
// }

//     private void FixedUpdate()
//     {
//         if (moveDirection == Vector3.zero)
//             return;

//         if (IsDashing)
//         {
//             MovePlayer(dashSpeed);
//             return;
//         }

//         phaseTimer += Time.fixedDeltaTime;

//         float currentSpeed = Mathf.Lerp(speed, dashSpeed, dashAmount);
//         float currentMoveTime = moveTime
//             / Mathf.Max(1f - dashAmount, 0.1f);
//         float currentWaitTime = waitTime * (1f - dashAmount);

//         if (canMove)
//         {
//             MovePlayer(currentSpeed);

//             if (phaseTimer >= currentMoveTime)
//             {
//                 canMove = false;
//                 phaseTimer = 0f;
//                 animator.SetBool("isMoving", false);
//             }
//         }
//         else if (phaseTimer >= currentWaitTime)
//         {
//             canMove = true;
//             phaseTimer = 0f;
//         }
//     }

//     private void MovePlayer(float currentSpeed)
//     {
//         Vector3 nextPosition = playerRigidbody.position
//             + moveDirection * currentSpeed * Time.fixedDeltaTime;

//         playerRigidbody.MovePosition(nextPosition);
//     }
// }
