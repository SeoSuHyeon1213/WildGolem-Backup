using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent agent;
    public float speed = 3.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindWithTag("Player").transform;

        agent.speed = speed;
    }

    void Update()
    {
        agent.SetDestination(player.position);
    }
}
