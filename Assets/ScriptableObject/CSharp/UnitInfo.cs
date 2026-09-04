using UnityEngine;

[CreateAssetMenu(fileName = "UnitInfo", menuName = "Scriptable Objects/UnitInfo")]
public class UnitInfo : ScriptableObject
{
    public string UnitName = "New Unit";
    public int HP = 100;
    public int SP = 50;
    public int Damage = 10;
    public float AttackSpeed = 1f;
}
