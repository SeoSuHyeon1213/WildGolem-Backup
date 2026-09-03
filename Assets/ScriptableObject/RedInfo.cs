using UnityEngine;

[CreateAssetMenu(fileName = "RedInfo", menuName = "Scriptable Objects/RedInfo")]
public class RedInfo : ScriptableObject
{
    public string RedName;
    public GameObject prefab;
    public int HP = 100;
    public int SP = 50;
    public int Damage = 10;
    public float AttackSpeed = 1f;
    public int numberOfPrefabsToCreate;
    public Vector3[] spawnPoints;
}
