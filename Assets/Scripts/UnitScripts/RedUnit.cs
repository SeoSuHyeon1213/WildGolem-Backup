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
        target.GetComponent<IUnit>().takeDamage(attackPower);
    }
    public void takeDamage(int damage)
    {
        //target.HP -= damage; 로 바꿀 예정
        HP -= damage;
        Debug.Log("RedUnit takes " + damage + " damage");
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
