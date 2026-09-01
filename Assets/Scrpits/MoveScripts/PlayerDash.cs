using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [SerializeField] private float maxDashSpeed = 15f;
    [SerializeField] private float accelerationTime = 2f;

    private PlayerMovement playerMovement;
    private bool isDashButtonHeld;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (!isDashButtonHeld)
            return;

        float targetSpeed = Mathf.Max(
            maxDashSpeed,
            playerMovement.DefaultMoveSpeed
        );

        float acceleration =
            (targetSpeed - playerMovement.DefaultMoveSpeed)
            / Mathf.Max(accelerationTime, 0.01f);

        float nextSpeed = Mathf.MoveTowards(
            playerMovement.MoveSpeed,
            targetSpeed,
            acceleration * Time.deltaTime
        );

        playerMovement.SetMoveSpeed(nextSpeed);
    }

    public void BeginDash()
    {
        isDashButtonHeld = true;
    }

    public void EndDash()
    {
        isDashButtonHeld = false;
        playerMovement.ResetMoveSpeed();
    }

    private void OnDisable()
    {
        isDashButtonHeld = false;

        if (playerMovement != null)
            playerMovement.ResetMoveSpeed();
    }
}
