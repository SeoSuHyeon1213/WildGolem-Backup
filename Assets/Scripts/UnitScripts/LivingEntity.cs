using System;
using UnityEngine;

public class LivingEntity : MonoBehaviour
{
    public float startingHealth;
    public float health { get; protected set; }
    public bool dead { get; protected set; }
    public event Action onDeath;
    public virtual void OnEnable()
    {
        dead = false;
        health = startingHealth;
    }

    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0 && !dead)
        {
            Die();
        }
    }

    public virtual void RestoreHealth(float newHealth)
    {
        health += newHealth;
    }

    protected virtual void Die()
    {
        if (onDeath != null)
        {
            onDeath();
        }
        dead = true;
    }


}
