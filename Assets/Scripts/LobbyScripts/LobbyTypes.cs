// 거점(로비)에서 쓰는 공통 자료형과 저장 데이터.
// JsonUtility로 저장하므로 Dictionary 대신 List/배열을 쓴다.
using System;
using System.Collections.Generic;

public enum ResourceType { Wood, Stone, Metal, ManaEssence, SpecialMaterial }

public enum GolemElement { None, Red, Green, Blue }

[Serializable]
public struct ResourceAmount
{
    public ResourceType type;
    public int amount;

    public ResourceAmount(ResourceType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}

[Serializable]
public class BuildingState
{
    public string id;
    public int level;
}

[Serializable]
public class GolemState
{
    public string id;
    public int level = 1;
}

[Serializable]
public class SaveData
{
    public int version = 1;

    // 재화 보유량. ResourceType 순서대로 들어간다.
    public long[] resources = new long[ResourceTypeCount];

    // 방치 누적: 수령 전까지 쌓인 양(소수 포함)과 시각(UTC Unix 초).
    // lastSettleUnix = 마지막으로 누적을 계산한 시각, lastClaimUnix = 마지막 수령 시각(상한 기준).
    public double[] idlePending = new double[ResourceTypeCount];
    public long lastSettleUnix;
    public long lastClaimUnix;

    public List<BuildingState> buildings = new List<BuildingState>();
    public List<GolemState> golems = new List<GolemState>();

    // 플레이어 주변 8방향 슬롯별 골렘 id. 빈 칸은 빈 문자열. 순서는 PartyController1.SlotDirections(0=앞, 시계 방향).
    public string[] partySlots = new string[PartySlotCount];

    public List<string> forestAssigned = new List<string>();
    public int researchStep;
    public List<string> conqueredRegions = new List<string>();

    public const int PartySlotCount = 8;
    public static readonly int ResourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;

    // 이전 버전 저장 파일이나 손상된 배열 길이를 맞춘다.
    public void Normalize()
    {
        if (resources == null || resources.Length != ResourceTypeCount)
            resources = Resize(resources, ResourceTypeCount);
        if (idlePending == null || idlePending.Length != ResourceTypeCount)
            idlePending = Resize(idlePending, ResourceTypeCount);
        if (partySlots == null || partySlots.Length != PartySlotCount)
            partySlots = Resize(partySlots, PartySlotCount);
        for (int i = 0; i < partySlots.Length; i++)
            partySlots[i] ??= string.Empty;

        buildings ??= new List<BuildingState>();
        golems ??= new List<GolemState>();
        forestAssigned ??= new List<string>();
        conqueredRegions ??= new List<string>();
    }

    private static T[] Resize<T>(T[] source, int length)
    {
        var result = new T[length];
        if (source != null)
            Array.Copy(source, result, Math.Min(source.Length, length));
        return result;
    }
}
