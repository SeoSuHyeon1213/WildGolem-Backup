using UnityEngine;

public interface IEnemy
{
    void Attack(Transform target);

    void takeDamage(int damage);

    void hpHeal(int amount);

    void spHeal(int amount);
}
