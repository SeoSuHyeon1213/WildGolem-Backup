using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerInput playerInput;
    private Rigidbody playerRigidbody;
    private Animator playerAnimator;
    private float moveSpeed = 5f;
    private float dashSpeed = 10f;
    bool isMoving = false;
    float waitTime = 0f;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
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

        playerRigidbody.MovePosition(playerRigidbody.position + movement * moveSpeed * Time.deltaTime);

        isMoving = movement.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            playerAnimator.SetBool("isMoving", true);
            if(movement.x < 0)
            {
                playerAnimator.SetBool("isLeft", true);
            }
            else
            {
                playerAnimator.SetBool("isLeft", false);
            }
        }
        else
        {
            playerAnimator.SetBool("isMoving", false);
            playerAnimator.SetBool("isLeft", false);
        }
        
    }
    
    public void Dash(){
        
        
        waitTime += Time.deltaTime;
        if(waitTime < 5f)
        {
            EndDialogue();
        }
        if(waitTime >= 5f)
        {
            StartDialogue();
            SetMoveSpeed(dashSpeed);
            waitTime = 0f;
        }
        
    }
    public void SetMoveSpeed(float speed)
    {
        moveSpeed += speed;
    }

    public void StartDialogue()
    {
        playerInput.DisableMovement();
    }

    public void EndDialogue()
    {
        playerInput.EnableMovement();
    }
        
}