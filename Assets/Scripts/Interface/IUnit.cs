using UnityEngine;

public interface IUnit{
    void Attack(IUnit target);

    void TakeDamage(int damage);
}
