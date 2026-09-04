using UnityEngine;

public interface IPlayerAttack 
{
    void Attack(Transform target);

    void takeDamage(int damage);

    void hpHeal(int amount);

    void spHeal(int amount);
    
}
