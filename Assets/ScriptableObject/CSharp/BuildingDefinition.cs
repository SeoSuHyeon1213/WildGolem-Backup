using UnityEngine;

// 거점 건물 하나의 정의. 수치는 모두 임시값(skills/CHECKLIST.md 로비 L-6)이며 인스펙터에서 조정한다.
[CreateAssetMenu(fileName = "BuildingDefinition", menuName = "Scriptable Objects/Lobby/BuildingDefinition")]
public class BuildingDefinition : ScriptableObject
{
    public string id = "building";
    public string displayName = "건물";
    [TextArea] public string description;

    [Tooltip("아직 기능이 없는 건물은 잠금으로 표시만 한다")]
    public bool locked;
    public int startLevel = 1;
    public int maxLevel = 10;                       // 임시값

    [Header("방치 생산 (생산 건물만)")]
    public bool producesResource;
    public ResourceType producedResource = ResourceType.Stone;
    public float productionPerHourAtLevel1 = 60f;  // 임시값
    public float productionPerHourPerLevel = 30f;  // 임시값

    [Header("업그레이드 비용 = 기본 비용 × 배율^(현재 레벨-1)")]
    public ResourceAmount[] baseUpgradeCost;
    public float upgradeCostMultiplier = 1.5f;      // 임시값

    public float ProductionPerHour(int level)
    {
        if (!producesResource || level <= 0)
            return 0f;
        return productionPerHourAtLevel1 + productionPerHourPerLevel * (level - 1);
    }

    public ResourceAmount[] UpgradeCost(int currentLevel)
    {
        float scale = Mathf.Pow(upgradeCostMultiplier, Mathf.Max(0, currentLevel - 1));
        var cost = new ResourceAmount[baseUpgradeCost?.Length ?? 0];
        for (int i = 0; i < cost.Length; i++)
            cost[i] = new ResourceAmount(baseUpgradeCost[i].type, Mathf.CeilToInt(baseUpgradeCost[i].amount * scale));
        return cost;
    }
}
