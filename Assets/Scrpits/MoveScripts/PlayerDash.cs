using UnityEngine;
using System.Collections;

public class PlayerDash : MonoBehaviour
{
    [SerializeField] private float maxDashSpeed = 15f;
    [SerializeField] private float accelerationTime = 2f;
    private int dashCount = 0;

    private PlayerMovement playerMovement;
    private bool isDashButtonHeld;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (isDashButtonHeld)
        {
            BeginDash();
            StartCoroutine(DashCoroutine());
        }
        else
        {
           EndDash();
        }
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

    public IEnumerator DashCoroutine()
    {
        if(dashCount < 10)
        {
            playerMovement.SetMoveSpeed(maxDashSpeed);
            dashCount++;
            yield return new WaitForSeconds(accelerationTime);
        }
    }
}
