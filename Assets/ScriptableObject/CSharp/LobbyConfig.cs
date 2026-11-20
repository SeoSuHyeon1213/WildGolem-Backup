using System;
using UnityEngine;

// 거점 전체 조정값. 수치는 모두 임시값(skills/CHECKLIST.md 로비 L-6)이며 인스펙터에서 조정한다.
[CreateAssetMenu(fileName = "LobbyConfig", menuName = "Scriptable Objects/Lobby/LobbyConfig")]
public class LobbyConfig : ScriptableObject
{
    [Header("방치 보상")]
    public float maxOfflineHours = 12f;             // 임시값
    public float bonusPerConqueredRegion = 0.05f;   // 임시값 (지역 1곳당 +5%)

    [Header("시작 재화")]
    public ResourceAmount[] startResources =
    {
        new ResourceAmount(ResourceType.Wood, 100),
        new ResourceAmount(ResourceType.Stone, 100),
        new ResourceAmount(ResourceType.Metal, 50),
    };

    [Header("건물·골렘")]
    public BuildingDefinition[] buildings;
    public GolemDefinition[] golems;

    public ForestSettings forest = new ForestSettings();
    public ResearchSettings research = new ResearchSettings();
    public GolemLevelSettings golemLevel = new GolemLevelSettings();

    [Tooltip("콘텐츠별 최대 인원(플레이어 포함). 첫 항목이 일반 원정")]
    public FormationRule[] formationRules =
    {
        new FormationRule { contentId = "normal", displayName = "일반 원정", maxMembersIncludingPlayer = 6 },
        new FormationRule { contentId = "raid", displayName = "보스 레이드", maxMembersIncludingPlayer = 9 },
    };

    public BuildingDefinition FindBuilding(string id) => Array.Find(buildings ?? Array.Empty<BuildingDefinition>(), b => b != null && b.id == id);
    public GolemDefinition FindGolem(string id) => Array.Find(golems ?? Array.Empty<GolemDefinition>(), g => g != null && g.id == id);
}

[Serializable]
public class ForestSettings
{
    public int assignSlots = 3;                 // 임시값
    public float woodPerGolemPerHour = 20f;     // 임시값
    public int woodPerHit = 5;                  // 임시값
    public int hitsPerTree = 3;                 // 임시값
    public float regrowSeconds = 30f;           // 임시값
}

[Serializable]
public class ResearchSettings
{
    public int baseLevelCap = 10;               // 임시값
    public int levelCapPerStep = 10;            // 임시값
    public int maxSteps = 5;                    // 임시값
    [Tooltip("연구 비용 = 기본 비용 × (다음 단계 번호)")]
    public ResourceAmount[] baseStepCost =
    {
        new ResourceAmount(ResourceType.Stone, 100),
        new ResourceAmount(ResourceType.Metal, 100),
    };
}

[Serializable]
public class GolemLevelSettings
{
    public ResourceType currency = ResourceType.Metal;  // 임시값 (L-11 미결정)
    public int costPerLevel = 10;                       // 임시값 (비용 = costPerLevel × 현재 레벨)
}

[Serializable]
public class FormationRule
{
    public string contentId;
    public string displayName;
    public int maxMembersIncludingPlayer = 6;
}
