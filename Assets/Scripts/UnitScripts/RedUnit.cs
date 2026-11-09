using UnityEngine;

public class RedUnit : MonoBehaviour, IUnit
{
    private Transform enemytarget;

    public UnitInfo unitInfo;
    string UnitName;
    int HP;
    int SP;
    int attackPower;
    float attackSpeed;

    // 공격·피격 스프라이트 재생. 프리팹을 직접 고치지 않도록 없으면 실행 중에 붙인다.
    private GolemMotionAnimator motionAnimator;

    private void Awake()
    {
        if (!TryGetComponent(out motionAnimator))
            motionAnimator = gameObject.AddComponent<GolemMotionAnimator>();
    }

    public void Initialize(UnitInfo info)
    {
        unitInfo = info;
        UnitName = unitInfo.UnitName;
        HP = unitInfo.HP;
        SP = unitInfo.SP;
        attackPower = unitInfo.Damage;
        attackSpeed = unitInfo.AttackSpeed;
    }
    public void Attack(Transform target)
    {
        Debug.Log("RedUnit attacks " + target.name);

        if (motionAnimator != null)
            motionAnimator.PlayAttack(target.position - transform.position);

        target.GetComponent<IUnit>().takeDamage(attackPower);
    }
    public void takeDamage(int damage)
    {
        //target.HP -= damage; 로 바꿀 예정
        HP -= damage;
        Debug.Log("RedUnit takes " + damage + " damage");

        if (motionAnimator != null)
            motionAnimator.PlayHit();
    }
    public void hpHeal(int amount)
    {
        HP += amount;
        Debug.Log("RedUnit heals " + amount + " HP");
    }
    public void spHeal(int amount)
    {
        SP += amount;
        Debug.Log("RedUnit heals " + amount + " SP");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemytarget = GameObject.FindGameObjectWithTag("Enemy").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (enemytarget != null)
        {
            transform.LookAt(enemytarget);
        }
    }
}
