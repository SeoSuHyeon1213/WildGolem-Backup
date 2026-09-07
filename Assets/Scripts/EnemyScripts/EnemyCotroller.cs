using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyCotroller : LivingEntity{
    public LayerMask whatIsPlayer;

    private LivingEntity targetEntity;

    private NavMeshAgent NavAgent;

    public float damage = 1f;
    public float timeBetweenAttacks = 1f;
    public float lastAttackTime = 0.5f;
    private bool hasTarget
    {
        get
        {
            if(targetEntity != null && !targetEntity.dead)
            {
                return true;
            }
        
            return false;
        }
    }
    private void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
    }
    public void SetUp(LivingEntity target)
    {
        targetEntity = target;
    }
    // Update is called once per frame

    private void Start()
    {
        StartCoroutine(UpdatePath());
    }
    void Update()
    {
        
    }
    private IEnumerator UpdatePath()
    {
        while(!dead)
        {
            if(hasTarget)
            {
                NavAgent.isStopped = false;
                NavAgent.SetDestination(targetEntity.transform.position);
            }
            else
            {
                NavAgent.isStopped = true;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }
}
