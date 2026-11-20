using UnityEngine;

// 거점에서 관리하는 골렘 하나의 정의. 전투 기본 수치는 기존 UnitInfo를 그대로 참조한다.
[CreateAssetMenu(fileName = "GolemDefinition", menuName = "Scriptable Objects/Lobby/GolemDefinition")]
public class GolemDefinition : ScriptableObject
{
    public string id = "golem";
    public string displayName = "골렘";
    public GolemElement element = GolemElement.Red;
    public UnitInfo baseStats;

    [Tooltip("새 저장 데이터를 만들 때 처음부터 보유하는지 (임시값)")]
    public bool ownedAtStart = true;
}
