using UnityEngine;

public class RedUnit : MonoBehaviour, IUnit
{

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
    // 살아 있는 적 중 가장 가까운 적을 찾는다. 없으면 null.
    // 태그("Enemy")는 프로젝트에 정의되어 있지 않아 쓰지 않는다. 바라보는 방향은 GolemMotionAnimator가 그림으로 바꾸므로
    // transform을 돌리지 않는다(RedUnit은 Player 복합 콜라이더의 일부라 돌리면 스프라이트와 충돌 범위가 함께 돌아간다).
    public Transform FindNearestEnemy()
    {
        Transform nearest = null;
        float best = float.PositiveInfinity;

        foreach (EnemyCotroller enemy in FindObjectsByType<EnemyCotroller>(FindObjectsInactive.Exclude))
        {
            if (enemy.dead)
                continue;

            float distance = (enemy.transform.position - transform.position).sqrMagnitude;

            if (distance < best)
            {
                best = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }
}
