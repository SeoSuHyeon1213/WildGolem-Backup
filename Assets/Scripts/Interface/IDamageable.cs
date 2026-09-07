using UnityEngine;

public interface IDamageable {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnDamage(float damage, Vector3 hitPoint, Vector3 hitDirection);
}
