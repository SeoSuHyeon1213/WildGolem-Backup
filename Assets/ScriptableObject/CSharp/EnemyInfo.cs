using UnityEngine;

[CreateAssetMenu(fileName = "EnemyInfo", menuName = "Scriptable Objects/EnemyInfo")]
public class EnemyInfo : ScriptableObject
{
    public string EnemyName = "New Unit";
    public int HP = 100;
    public int SP = 50;
    public int Damage = 10;
    public float AttackSpeed = 1f;
}
