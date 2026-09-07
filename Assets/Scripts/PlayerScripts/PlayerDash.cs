using UnityEngine;
using System.Collections;

public class PlayerDash : MonoBehaviour
{
    private float maxDashSpeed = 5f;
    private float accelerationTime = 2f;
    private int dashCount = 0;

    private PlayerMovement playerMovement;
    private bool isDashButtonHeld;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void Dash()
    {
        isDashButtonHeld = true;
        StartCoroutine(ChargeDash());
        if(!isDashButtonHeld)
        {
            EndDash();
        }
        
    }

    public void EndDash()
    {
        isDashButtonHeld = false;
        playerMovement.ResetMoveSpeed();
    }

    public IEnumerator ChargeDash()
    {
        if(dashCount < 10)
        {
            playerMovement.SetMoveSpeed(maxDashSpeed);
            yield return new WaitForSeconds(accelerationTime);
            dashCount++;
        }
    }
}
