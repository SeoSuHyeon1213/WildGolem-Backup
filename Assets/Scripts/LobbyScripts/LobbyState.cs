// 거점 규칙(재화·방치 누적·건물·숲·골렘·연구·편성)을 한곳에서 처리한다.
// MonoBehaviour가 아니어서 씬 없이도 시험할 수 있다. 시각은 호출하는 쪽이 넘긴다(UTC Unix 초).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LobbyState
{
    public SaveData Data { get; }
    public LobbyConfig Config { get; }

    // 값이 바뀔 때마다 알린다(UI 갱신·저장).
    public event Action Changed;

    public LobbyState(SaveData data, LobbyConfig config)
    {
        Data = data;
        Config = config;
        Data.Normalize();
    }

    // 새 게임 데이터: 시작 재화, 건물 시작 레벨, 시작 보유 골렘.
    public static SaveData CreateNew(LobbyConfig config, long now)
    {
        var data = new SaveData { lastSettleUnix = now, lastClaimUnix = now };
        data.Normalize();

        foreach (var amount in config.startResources)
            data.resources[(int)amount.type] += amount.amount;

        foreach (var building in config.buildings.Where(b => b != null))
            data.buildings.Add(new BuildingState { id = building.id, level = building.locked ? 0 : building.startLevel });

        foreach (var golem in config.golems.Where(g => g != null && g.ownedAtStart))
            data.golems.Add(new GolemState { id = golem.id, level = 1 });

        return data;
    }

    private void NotifyChanged() => Changed?.Invoke();

    // ───────── 재화 ─────────
    public long GetResource(ResourceType type) => Data.resources[(int)type];

    public bool CanAfford(IEnumerable<ResourceAmount> cost) => cost.All(c => GetResource(c.type) >= c.amount);

    private bool TrySpend(ResourceAmount[] cost)
    {
        if (!CanAfford(cost))
            return false;
        foreach (var c in cost)
            Data.resources[(int)c.type] -= c.amount;
        return true;
    }

    public void AddResource(ResourceType type, long amount)
    {
        Data.resources[(int)type] = Math.Max(0, Data.resources[(int)type] + amount);
        NotifyChanged();
    }

    // ───────── 방치 누적 ─────────
    public float RegionBonus => Data.conqueredRegions.Count * Config.bonusPerConqueredRegion;

    public float RatePerHour(ResourceType type)
    {
        float rate = 0f;
        foreach (var state in Data.buildings)
        {
            var def = Config.FindBuilding(state.id);
            if (def != null && def.producesResource && def.producedResource == type)
                rate += def.ProductionPerHour(state.level);
        }

        if (type == ResourceType.Wood)
            rate += Data.forestAssigned.Count * Config.forest.woodPerGolemPerHour;

        return rate * (1f + RegionBonus);
    }

    public long MaxOfflineSeconds => (long)(Config.maxOfflineHours * 3600f);

    // 수령 이후 상한까지 남은 누적 시간을 반영해 idlePending에 더한다.
    // 생산량이 바뀌기 직전(업그레이드·숲 배정 등)에 꼭 호출해야 이전 생산량 구간이 맞게 계산된다.
    public void Settle(long now)
    {
        // 기기 시계를 되돌린 경우: 누적하지 않고 기준 시각만 당긴다.
        if (now < Data.lastSettleUnix)
        {
            Data.lastSettleUnix = now;
            Data.lastClaimUnix = Math.Min(Data.lastClaimUnix, now);
            return;
        }

        long capEnd = Data.lastClaimUnix + MaxOfflineSeconds;
        long end = Math.Min(now, capEnd);
        long seconds = end - Data.lastSettleUnix;
        if (seconds > 0)
        {
            double hours = seconds / 3600.0;
            for (int i = 0; i < SaveData.ResourceTypeCount; i++)
                Data.idlePending[i] += RatePerHour((ResourceType)i) * hours;
        }
        Data.lastSettleUnix = now;
    }

    // 지금 수령하면 받을 양(정수). 상태를 바꾸지 않는다.
    public long PendingAmount(ResourceType type, long now)
    {
        long capEnd = Data.lastClaimUnix + MaxOfflineSeconds;
        long seconds = Math.Max(0, Math.Min(now, capEnd) - Data.lastSettleUnix);
        double pending = Data.idlePending[(int)type] + RatePerHour(type) * (seconds / 3600.0);
        return (long)Math.Floor(pending);
    }

    public long AccumulatedSeconds(long now) => Math.Clamp(now - Data.lastClaimUnix, 0, MaxOfflineSeconds);

    public bool IsIdleFull(long now) => now - Data.lastClaimUnix >= MaxOfflineSeconds;

    // 쌓인 정수만 지갑에 넣고 소수점 아래는 다음 수령으로 넘긴다.
    public long[] ClaimIdle(long now)
    {
        Settle(now);
        var claimed = new long[SaveData.ResourceTypeCount];
        for (int i = 0; i < claimed.Length; i++)
        {
            claimed[i] = (long)Math.Floor(Data.idlePending[i]);
            Data.idlePending[i] -= claimed[i];
            Data.resources[i] += claimed[i];
        }
        Data.lastClaimUnix = now;
        Data.lastSettleUnix = now;
        NotifyChanged();
        return claimed;
    }

    // ───────── 건물 ─────────
    public BuildingState GetBuildingState(string id) => Data.buildings.Find(b => b.id == id);

    public int GetBuildingLevel(string id) => GetBuildingState(id)?.level ?? 0;

    public bool CanUpgrade(string id, out string reason)
    {
        var def = Config.FindBuilding(id);
        int level = GetBuildingLevel(id);
        reason = null;
        if (def == null) reason = "정의 없음";
        else if (def.locked) reason = "준비 중";
        else if (level >= def.maxLevel) reason = "최대 레벨";
        else if (!CanAfford(def.UpgradeCost(level))) reason = "재화 부족";
        return reason == null;
    }

    public bool TryUpgrade(string id, long now)
    {
        if (!CanUpgrade(id, out _))
            return false;

        var def = Config.FindBuilding(id);
        var state = GetBuildingState(id);
        if (state == null)
        {
            state = new BuildingState { id = id, level = 0 };
            Data.buildings.Add(state);
        }

        Settle(now);  // 이전 생산량 구간을 먼저 확정
        TrySpend(def.UpgradeCost(state.level));
        state.level++;
        NotifyChanged();
        return true;
    }

    // ───────── 숲 ─────────
    public bool IsInForest(string golemId) => Data.forestAssigned.Contains(golemId);

    public bool TryAssignForest(string golemId, long now)
    {
        if (!OwnsGolem(golemId) || IsInForest(golemId) || Data.forestAssigned.Count >= Config.forest.assignSlots)
            return false;

        Settle(now);
        RemoveFromParty(golemId);  // 기본안(L-10 미결정): 숲 배정 중에는 편성 불가
        Data.forestAssigned.Add(golemId);
        NotifyChanged();
        return true;
    }

    public void UnassignForest(string golemId, long now)
    {
        if (!IsInForest(golemId))
            return;
        Settle(now);
        Data.forestAssigned.Remove(golemId);
        NotifyChanged();
    }

    // ───────── 골렘·연구 ─────────
    public bool OwnsGolem(string id) => Data.golems.Exists(g => g.id == id);

    public GolemState GetGolem(string id) => Data.golems.Find(g => g.id == id);

    public int LevelCap => Config.research.baseLevelCap + Data.researchStep * Config.research.levelCapPerStep;

    public int GolemLevelUpCost(int currentLevel) => Config.golemLevel.costPerLevel * currentLevel;

    public bool CanLevelUp(string id, out string reason)
    {
        var golem = GetGolem(id);
        reason = null;
        if (golem == null) reason = "보유하지 않음";
        else if (golem.level >= LevelCap) reason = "연구 필요";
        else if (GetResource(Config.golemLevel.currency) < GolemLevelUpCost(golem.level)) reason = "재화 부족";
        return reason == null;
    }

    public bool TryLevelUp(string id)
    {
        if (!CanLevelUp(id, out _))
            return false;
        var golem = GetGolem(id);
        Data.resources[(int)Config.golemLevel.currency] -= GolemLevelUpCost(golem.level);
        golem.level++;
        NotifyChanged();
        return true;
    }

    public ResourceAmount[] ResearchCost(int nextStep) =>
        Config.research.baseStepCost.Select(c => new ResourceAmount(c.type, c.amount * nextStep)).ToArray();

    public bool CanResearch(out string reason)
    {
        reason = null;
        if (Data.researchStep >= Config.research.maxSteps) reason = "최대 단계";
        else if (!CanAfford(ResearchCost(Data.researchStep + 1))) reason = "재화 부족";
        return reason == null;
    }

    public bool TryResearch()
    {
        if (!CanResearch(out _))
            return false;
        TrySpend(ResearchCost(Data.researchStep + 1));
        Data.researchStep++;
        NotifyChanged();
        return true;
    }

    // ───────── 편성 ─────────
    public FormationRule CurrentRule => Config.formationRules.Length > 0 ? Config.formationRules[0] : new FormationRule();

    public int PartyGolemCount => Data.partySlots.Count(s => !string.IsNullOrEmpty(s));

    public int MaxPartyGolems(FormationRule rule) => Mathf.Clamp(rule.maxMembersIncludingPlayer - 1, 0, SaveData.PartySlotCount);

    // 빈 문자열 golemId = 슬롯 비우기. 같은 골렘이 다른 슬롯에 있으면 옮긴다.
    public bool TrySetPartySlot(int slot, string golemId, FormationRule rule)
    {
        if (slot < 0 || slot >= SaveData.PartySlotCount)
            return false;

        if (string.IsNullOrEmpty(golemId))
        {
            Data.partySlots[slot] = string.Empty;
            NotifyChanged();
            return true;
        }

        if (!OwnsGolem(golemId) || IsInForest(golemId))
            return false;

        int existing = Array.IndexOf(Data.partySlots, golemId);
        bool slotWasEmpty = string.IsNullOrEmpty(Data.partySlots[slot]);
        bool addsMember = existing < 0 && slotWasEmpty;
        if (addsMember && PartyGolemCount >= MaxPartyGolems(rule))
            return false;

        if (existing >= 0)
            Data.partySlots[existing] = string.Empty;
        Data.partySlots[slot] = golemId;
        NotifyChanged();
        return true;
    }

    public void RemoveFromParty(string golemId)
    {
        for (int i = 0; i < Data.partySlots.Length; i++)
            if (Data.partySlots[i] == golemId)
                Data.partySlots[i] = string.Empty;
    }

    // README 7장 조합 표의 이름만 돌려준다. 효과 수치는 미정(L-12).
    // "다수" 기준은 같은 속성 2기 이상(임시값).
    public List<string> SynergyPreview(int manyThreshold = 2)
    {
        var counts = new Dictionary<GolemElement, int> { { GolemElement.Red, 0 }, { GolemElement.Green, 0 }, { GolemElement.Blue, 0 } };
        foreach (var id in Data.partySlots.Where(s => !string.IsNullOrEmpty(s)))
        {
            var def = Config.FindGolem(id);
            if (def != null && counts.ContainsKey(def.element))
                counts[def.element]++;
        }

        bool r = counts[GolemElement.Red] > 0, g = counts[GolemElement.Green] > 0, b = counts[GolemElement.Blue] > 0;
        var result = new List<string>();
        if (counts[GolemElement.Red] >= manyThreshold) result.Add("RED 다수: 넉백·근접 공격 강화");
        if (counts[GolemElement.Green] >= manyThreshold) result.Add("GREEN 다수: 공격 속도·원거리 공격 강화");
        if (counts[GolemElement.Blue] >= manyThreshold) result.Add("BLUE 다수: 방어·보호막 강화");
        if (r && g && b) result.Add("RED+GREEN+BLUE: 코어 공명");
        else
        {
            if (r && b) result.Add("RED+BLUE: 돌진 중 경직 저항");
            if (g && b) result.Add("GREEN+BLUE: 방어 진형 강화");
            if (r && g) result.Add("RED+GREEN: 처치 시 이동 속도 증가");
        }
        return result;
    }

    // 플레이어 포함 인원으로 1인/2인/다인 편성을 구분한다(README 6장).
    public string PartySizeLabel()
    {
        int members = PartyGolemCount + 1;
        return members switch
        {
            1 => "1인 편성: 강화 집중",
            2 => "2인 편성: 연계 효과",
            _ => $"다인 편성({members}명): 조합 보너스",
        };
    }

    // ───────── 점령 지역 ─────────
    public void AddConqueredRegion(string regionId, long now)
    {
        if (Data.conqueredRegions.Contains(regionId))
            return;
        Settle(now);
        Data.conqueredRegions.Add(regionId);
        NotifyChanged();
    }
}
