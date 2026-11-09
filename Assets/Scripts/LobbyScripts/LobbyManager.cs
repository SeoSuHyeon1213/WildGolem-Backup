// 거점 씬의 진입점: 저장 데이터를 불러와 LobbyState를 만들고, 바뀌면 저장한다.
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private LobbyConfig config;

    public static LobbyManager Instance { get; private set; }
    public LobbyState State { get; private set; }
    public LobbyConfig Config => config;
    public long Now => SaveSystem.NowUnix;

    private bool isDirty;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("LobbyManager에 LobbyConfig가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        Instance = this;

        SaveData data = SaveSystem.Load();
        if (data == null)
        {
            data = LobbyState.CreateNew(config, Now);
            Debug.Log($"새 거점 저장 데이터를 만들었습니다: {SaveSystem.FilePath}");
        }

        State = new LobbyState(data, config);
        State.Settle(Now);  // 꺼져 있던 동안의 방치 누적
        State.Changed += () => isDirty = true;
        SaveSystem.Save(State.Data);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SaveNow();
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        if (isDirty)
            SaveNow();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();

    public void SaveNow()
    {
        if (State == null)
            return;
        State.Settle(Now);
        SaveSystem.Save(State.Data);
        isDirty = false;
    }

    // 나무를 한 번 친다. 쓰러지지 않은 나무만 목재를 준다.
    public int ChopTree(LobbyTree tree)
    {
        if (State == null || tree == null || !tree.TryHit())
            return 0;
        int wood = config.forest.woodPerHit;
        State.AddResource(ResourceType.Wood, wood);
        return wood;
    }

    // ───────── 시험용 (인스펙터 컴포넌트 메뉴) ─────────
    [ContextMenu("시험: 1시간 경과")]
    private void DebugAdvanceHour() => DebugShiftTime(3600);

    [ContextMenu("시험: 13시간 경과 (상한 넘김)")]
    private void DebugAdvance13Hours() => DebugShiftTime(13 * 3600);

    // 저장된 기준 시각을 과거로 당겨 시간이 흐른 것처럼 만든다.
    private void DebugShiftTime(long seconds)
    {
        if (State == null) return;
        State.Settle(Now);
        State.Data.lastSettleUnix -= seconds;
        State.Data.lastClaimUnix -= seconds;
        SaveNow();
        Debug.Log($"시험: {seconds / 3600f:0.#}시간 경과 처리");
    }

    [ContextMenu("시험: 점령 지역 +1")]
    private void DebugAddRegion()
    {
        if (State == null) return;
        State.AddConqueredRegion($"test_region_{State.Data.conqueredRegions.Count + 1}", Now);
    }

    [ContextMenu("시험: 재화 +1000")]
    private void DebugAddResources()
    {
        if (State == null) return;
        State.AddResource(ResourceType.Wood, 1000);
        State.AddResource(ResourceType.Stone, 1000);
        State.AddResource(ResourceType.Metal, 1000);
    }

    [ContextMenu("시험: 저장 초기화 (다음 Play에 새로 시작)")]
    private void DebugResetSave()
    {
        SaveSystem.Delete();
        State = null;
        Debug.Log("저장 파일을 지웠습니다. Play 모드를 다시 시작하면 새 데이터로 시작합니다.");
    }
}
