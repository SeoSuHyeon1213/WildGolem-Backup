using UnityEngine;
using UnityEngine.AI;

public class Partycontroller : MonoBehaviour
{
    [Header("Party")]
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent[] members;

    [Header("Follow")]
    [SerializeField] private float followSpacing = 1.5f;

    [Header("Stopped Formation")]
    [SerializeField] private float surroundRadius = 2f;
    [SerializeField] private float stopDelay = 0.3f;

    [Header("Movement Detection")]
    [SerializeField] private float movingSpeedThreshold = 0.05f;
    [SerializeField] private float pathUpdateInterval = 0.1f;
    [SerializeField] private float navMeshSampleRadius = 1f;

    private Vector3 previousPlayerPosition;
    private Vector3 lastMoveDirection = Vector3.forward;
    private float stoppedTime;
    private float nextPathUpdateTime;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
            previousPlayerPosition = player.position;
    }

    private void Update()
    {
        if (player == null || members == null || members.Length == 0)
        {
            Debug.Log("Player or party members are not set up correctly");
            return;
        }

        Vector3 movement = player.position - previousPlayerPosition;
        movement.y = 0f;

        float playerSpeed = movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        bool isPlayerMoving = playerSpeed > movingSpeedThreshold;

        if (isPlayerMoving)
        {
            lastMoveDirection = movement.normalized;
            stoppedTime = 0f;
        }
        else
        {
            stoppedTime += Time.deltaTime;
        }

        previousPlayerPosition = player.position;

        if (Time.time < nextPathUpdateTime)
        {
            Debug.Log("Waiting for next path update time");
            return;
        }
        nextPathUpdateTime = Time.time + pathUpdateInterval;

        if (stoppedTime >= stopDelay)
            ArrangeAroundPlayer();
        else
            FollowPlayer();
        
        float angle = Mathf.Atan2(lastMoveDirection.x, lastMoveDirection.z) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        float radians = snappedAngle * Mathf.Deg2Rad;
    }

    private void FollowPlayer()
    {
        for (int i = 0; i < members.Length; i++)
        {
            Vector3 targetPosition = player.position
                - lastMoveDirection * followSpacing * (i + 1);

            SetDestination(members[i], targetPosition);
        }
    }

    private void ArrangeAroundPlayer()
    {
        float angleStep = 360f / members.Length;

        for (int i = 0; i < members.Length; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Sin(angle),
                0f,
                Mathf.Cos(angle)
            ) * surroundRadius;

            SetDestination(members[i], player.position + offset);
        }
    }

    private void SetDestination(NavMeshAgent member, Vector3 targetPosition)
    {
        if (member == null || !member.isOnNavMesh)
        {
            Debug.Log("Member is null or not on NavMesh");
            return;
        }

        if (NavMesh.SamplePosition(
                targetPosition,
                out NavMeshHit hit,
                navMeshSampleRadius,
                NavMesh.AllAreas))
        {
            member.SetDestination(hit.position);
        }
    }
}
