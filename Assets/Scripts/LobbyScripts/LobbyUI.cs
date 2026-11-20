// 거점 화면 UI를 실행 중에 코드로 만든다(자리표시 단계).
// 상단 재화, 방치 보상 버튼, 상호작용 버튼, 지점 이름표, 공용 패널, 짧은 알림으로 구성한다.
// 글꼴: 프로젝트에 한글 글꼴 에셋이 없어 OS 글꼴(맑은 고딕 등)을 동적으로 쓴다. 정식 글꼴은 따로 정한다.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private int sortingOrder = 10;
    [SerializeField] private float bodyRefreshInterval = 0.25f;

    private static readonly string[] SlotNames = { "앞", "앞오른쪽", "오른쪽", "뒤오른쪽", "뒤", "뒤왼쪽", "왼쪽", "앞왼쪽" };
    private static readonly string[] OsFontNames = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic", "Arial" };

    private Font font;
    private RectTransform root;
    private Text resourceText;
    private Text idleButtonText;
    private Image idleButtonImage;
    private Button interactButton;
    private Text interactText;
    private RectTransform labelLayer;
    private readonly Dictionary<LobbySite, Text> labels = new Dictionary<LobbySite, Text>();

    private GameObject panel;
    private Text panelTitle;
    private Text panelBody;
    private RectTransform panelButtons;
    private Func<string> panelBodyProvider;
    private Action panelButtonsBuilder;
    private float nextBodyRefresh;

    private Text toastText;
    private float toastUntil;

    private LobbySite currentSite;
    private int formationRuleIndex;
    private Camera cam;

    private LobbyState State => LobbyManager.Instance != null ? LobbyManager.Instance.State : null;
    private long Now => SaveSystem.NowUnix;

    private void Start()
    {
        font =Font.CreateDynamicFontFromOSFont(OsFontNames, 32);
        BuildCanvas();

        if (State != null)
            State.Changed += OnStateChanged;
        RefreshHud();
    }

    private void OnDestroy()
    {
        if (State != null)
            State.Changed -= OnStateChanged;
    }

    private void OnStateChanged()
    {
        RefreshHud();
        if (panel != null && panel.activeSelf)
            RebuildPanel();
    }

    private void Update()
    {
        if (State == null)
            return;

        RefreshIdleButton();
        UpdateInteractButton();

        if (panel.activeSelf && Time.unscaledTime >= nextBodyRefresh)
        {
            nextBodyRefresh = Time.unscaledTime + bodyRefreshInterval;
            panelBody.text = panelBodyProvider?.Invoke() ?? string.Empty;
        }

        if (toastText.gameObject.activeSelf && Time.unscaledTime >= toastUntil)
            toastText.gameObject.SetActive(false);
    }

    private void LateUpdate() => UpdateLabels();

    // ───────── HUD ─────────
    private void RefreshHud()
    {
        if (State == null || resourceText == null)
            return;
        resourceText.text =
            $"목재 {State.GetResource(ResourceType.Wood):N0}    석재 {State.GetResource(ResourceType.Stone):N0}    금속 {State.GetResource(ResourceType.Metal):N0}";
    }

    private void RefreshIdleButton()
    {
        long now = Now;
        bool full = State.IsIdleFull(now);
        idleButtonText.text =
            $"방치 보상 {FormatDuration(State.AccumulatedSeconds(now))} / {FormatDuration(State.MaxOfflineSeconds)}{(full ? " (가득)" : "")}\n" +
            $"목재 {State.PendingAmount(ResourceType.Wood, now)}  석재 {State.PendingAmount(ResourceType.Stone, now)}  금속 {State.PendingAmount(ResourceType.Metal, now)}";
        idleButtonImage.color = full ? new Color(0.85f, 0.55f, 0.15f, 0.95f) : new Color(0.15f, 0.15f, 0.2f, 0.85f);
    }

    private void UpdateInteractButton()
    {
        // 패널이 열려 있으면 상호작용 버튼을 숨긴다.
        LobbySite site = panel.activeSelf ? null : NearestSite();
        currentSite = site;
        interactButton.gameObject.SetActive(site != null);
        if (site == null)
            return;

        if (site.Kind == LobbySiteKind.Tree)
        {
            var tree = site.GetComponent<LobbyTree>();
            interactText.text = tree != null && tree.IsFelled
                ? $"다시 자라는 중 {Mathf.CeilToInt(tree.RegrowRemaining)}초"
                : $"벌목하기 (목재 +{State.Config.forest.woodPerHit})";
            interactButton.interactable = tree != null && !tree.IsFelled;
        }
        else
        {
            interactText.text = $"{site.DisplayName} 열기";
            interactButton.interactable = true;
        }
    }

    private LobbySite NearestSite()
    {
        Transform player = FindPlayer();
        LobbySite best = null;
        float bestDistance = float.MaxValue;
        foreach (var site in LobbySite.PlayerInside)
        {
            if (site == null) continue;
            float d = player != null ? (site.transform.position - player.position).sqrMagnitude : 0f;
            if (d < bestDistance) { bestDistance = d; best = site; }
        }
        return best;
    }

    private Transform playerCache;
    private Transform FindPlayer()
    {
        if (playerCache == null)
        {
            var movement = FindAnyObjectByType<PlayerMovement>();
            playerCache = movement != null ? movement.transform : null;
        }
        return playerCache;
    }

    private void UpdateLabels()
    {
        // 로비 씬의 Main Camera에 MainCamera 태그가 없어 Camera.main이 비어 있을 수 있다.
        if (cam == null) cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (cam == null || labelLayer == null) return;

        foreach (var site in LobbySite.All)
        {
            if (site == null || site.Kind == LobbySiteKind.Tree) continue;
            if (!labels.TryGetValue(site, out var label))
            {
                label = CreateText(labelLayer, site.DisplayName, 26, TextAnchor.MiddleCenter, Color.white);
                label.rectTransform.sizeDelta = new Vector2(300, 40);
                label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
                labels[site] = label;
            }

            Vector3 screen = cam.WorldToScreenPoint(site.LabelPosition);
            bool visible = screen.z > 0f && !panel.activeSelf;
            label.gameObject.SetActive(visible);
            if (visible)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(labelLayer, screen, null, out Vector2 local);
                label.rectTransform.anchoredPosition = local;
            }
        }
    }

    // ───────── 상호작용 ─────────
    private void OnInteract()
    {
        var site = currentSite;
        if (site == null || State == null) return;

        switch (site.Kind)
        {
            case LobbySiteKind.Tree:
                int wood = LobbyManager.Instance.ChopTree(site.GetComponent<LobbyTree>());
                if (wood > 0) Toast($"목재 +{wood}");
                break;
            case LobbySiteKind.Building: OpenBuildingPanel(site.BuildingId); break;
            case LobbySiteKind.IdleChest: OpenIdlePanel(); break;
            case LobbySiteKind.Research: OpenResearchPanel(); break;
            case LobbySiteKind.Training: OpenTrainingPanel(); break;
            case LobbySiteKind.ForestAssign: OpenForestPanel(); break;
            case LobbySiteKind.Gate: OpenFormationPanel(); break;
        }
    }

    private void OpenIdlePanel()
    {
        OpenPanel("방치 보상", () =>
        {
            long now = Now;
            var sb = new StringBuilder();
            sb.AppendLine($"누적 시간: {FormatDuration(State.AccumulatedSeconds(now))} / 최대 {FormatDuration(State.MaxOfflineSeconds)}");
            sb.AppendLine($"점령 지역 {State.Data.conqueredRegions.Count}곳: 생산량 +{State.RegionBonus * 100f:0.#}%");
            sb.AppendLine();
            foreach (var type in new[] { ResourceType.Wood, ResourceType.Stone, ResourceType.Metal })
                sb.AppendLine($"{ResourceName(type)}: 시간당 {State.RatePerHour(type):0.#} · 받을 양 {State.PendingAmount(type, now)}");
            return sb.ToString();
        }, () =>
        {
            AddPanelButton("수령하기", true, () =>
            {
                var got = State.ClaimIdle(Now);
                Toast($"수령: 목재 {got[(int)ResourceType.Wood]} · 석재 {got[(int)ResourceType.Stone]} · 금속 {got[(int)ResourceType.Metal]}");
            });
        });
    }

    private void OpenBuildingPanel(string id)
    {
        var def = State.Config.FindBuilding(id);
        if (def == null) { Toast("건물 정의가 없습니다"); return; }

        OpenPanel(def.displayName, () =>
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(def.description)) sb.AppendLine(def.description).AppendLine();
            if (def.locked) { sb.AppendLine("준비 중인 건물입니다."); return sb.ToString(); }

            int level = State.GetBuildingLevel(id);
            sb.AppendLine($"레벨 {level} / {def.maxLevel}");
            if (def.producesResource)
            {
                string next = level < def.maxLevel ? $" → {def.ProductionPerHour(level + 1):0.#}" : "";
                sb.AppendLine($"{ResourceName(def.producedResource)} 시간당 {def.ProductionPerHour(level):0.#}{next} (점령 보너스 전)");
            }
            if (level < def.maxLevel)
                sb.AppendLine($"업그레이드 비용: {FormatCost(def.UpgradeCost(level))}");
            return sb.ToString();
        }, () =>
        {
            if (def.locked) return;
            bool can = State.CanUpgrade(id, out string reason);
            AddPanelButton(can ? "업그레이드" : $"업그레이드 ({reason})", can, () =>
            {
                if (State.TryUpgrade(id, Now)) Toast($"{def.displayName} 레벨 {State.GetBuildingLevel(id)}");
            });
        });
    }

    private void OpenResearchPanel()
    {
        OpenPanel("연구소", () =>
        {
            var r = State.Config.research;
            var sb = new StringBuilder();
            sb.AppendLine($"연구 단계 {State.Data.researchStep} / {r.maxSteps}");
            sb.AppendLine($"골렘 레벨 상한: {State.LevelCap}");
            if (State.Data.researchStep < r.maxSteps)
                sb.AppendLine($"다음 단계: 상한 +{r.levelCapPerStep} · 비용 {FormatCost(State.ResearchCost(State.Data.researchStep + 1))}");
            return sb.ToString();
        }, () =>
        {
            bool can = State.CanResearch(out string reason);
            AddPanelButton(can ? "연구하기" : $"연구하기 ({reason})", can, () =>
            {
                if (State.TryResearch()) Toast($"골렘 레벨 상한 {State.LevelCap}");
            });
        });
    }

    private void OpenTrainingPanel()
    {
        OpenPanel("훈련소", () =>
            $"골렘 레벨업 · 상한 {State.LevelCap} (연구소에서 올림)\n비용: {ResourceName(State.Config.golemLevel.currency)} {State.Config.golemLevel.costPerLevel} × 현재 레벨",
        () =>
        {
            foreach (var golem in State.Data.golems)
            {
                string id = golem.id;
                bool can = State.CanLevelUp(id, out string reason);
                string label = $"{GolemName(id)} Lv{golem.level}" +
                    (can ? $" → Lv{golem.level + 1} ({ResourceName(State.Config.golemLevel.currency)} {State.GolemLevelUpCost(golem.level)})" : $" ({reason})");
                AddPanelButton(label, can, () => State.TryLevelUp(id));
            }
        });
    }

    private void OpenForestPanel()
    {
        OpenPanel("숲 작업장", () =>
        {
            var f = State.Config.forest;
            return $"배정 {State.Data.forestAssigned.Count} / {f.assignSlots}칸 · 골렘 1기당 목재 시간당 {f.woodPerGolemPerHour:0.#}\n" +
                   $"현재 목재 생산: 시간당 {State.RatePerHour(ResourceType.Wood):0.#}\n배정한 골렘은 원정 편성에서 빠집니다(임시 규칙).";
        }, () =>
        {
            foreach (var golem in State.Data.golems)
            {
                string id = golem.id;
                if (State.IsInForest(id))
                    AddPanelButton($"{GolemName(id)}: 배정 해제", true, () => State.UnassignForest(id, Now));
                else
                {
                    bool full = State.Data.forestAssigned.Count >= State.Config.forest.assignSlots;
                    AddPanelButton(full ? $"{GolemName(id)}: 배정 (칸 없음)" : $"{GolemName(id)}: 숲에 배정", !full,
                        () => State.TryAssignForest(id, Now));
                }
            }
        });
    }

    private void OpenFormationPanel()
    {
        OpenPanel("원정 게이트 · 편성", () =>
        {
            var rule = CurrentRule();
            var sb = new StringBuilder();
            sb.AppendLine($"{rule.displayName}: 플레이어 포함 최대 {rule.maxMembersIncludingPlayer}명 (골렘 {State.PartyGolemCount}/{State.MaxPartyGolems(rule)})");
            sb.AppendLine(State.PartySizeLabel());
            var synergy = State.SynergyPreview();
            sb.AppendLine(synergy.Count > 0 ? "시너지: " + string.Join(" / ", synergy) : "시너지: 없음");
            sb.Append("슬롯을 누르면 다음 골렘으로 바뀝니다.");
            return sb.ToString();
        }, () =>
        {
            var rule = CurrentRule();
            AddPanelButton($"콘텐츠: {rule.displayName} (눌러서 변경)", State.Config.formationRules.Length > 1, () =>
            {
                formationRuleIndex = (formationRuleIndex + 1) % State.Config.formationRules.Length;
                RebuildPanel();
            });

            for (int i = 0; i < SaveData.PartySlotCount; i++)
            {
                int slot = i;
                string current = State.Data.partySlots[slot];
                string name = string.IsNullOrEmpty(current) ? "(비어 있음)" : GolemName(current);
                AddPanelButton($"{SlotNames[slot]}: {name}", true, () => CyclePartySlot(slot));
            }

            // 원정 출발·귀환 연결은 5단계(ExpeditionSession)에서 한다.
            AddPanelButton("원정 출발 (5단계에서 연결)", false, null);
        });
    }

    private FormationRule CurrentRule()
    {
        var rules = State.Config.formationRules;
        if (rules == null || rules.Length == 0) return State.CurrentRule;
        formationRuleIndex = Mathf.Clamp(formationRuleIndex, 0, rules.Length - 1);
        return rules[formationRuleIndex];
    }

    // 빈 칸 → 편성 가능한 골렘 순서로 돌린다. 인원 상한에 걸리는 골렘은 건너뛴다.
    private void CyclePartySlot(int slot)
    {
        var options = new List<string> { string.Empty };
        options.AddRange(State.Data.golems.Select(g => g.id).Where(id => !State.IsInForest(id)));

        int start = options.IndexOf(State.Data.partySlots[slot] ?? string.Empty);
        for (int step = 1; step <= options.Count; step++)
        {
            string candidate = options[(start + step) % options.Count];
            if (State.TrySetPartySlot(slot, candidate, CurrentRule()))
                return;
        }
        Toast("편성 인원이 가득 찼습니다");
    }

    // ───────── 공용 패널 ─────────
    private void OpenPanel(string title, Func<string> body, Action buttons)
    {
        panelTitle.text = title;
        panelBodyProvider = body;
        panelButtonsBuilder = buttons;
        panel.SetActive(true);
        RebuildPanel();
    }

    private void RebuildPanel()
    {
        panelBody.text = panelBodyProvider?.Invoke() ?? string.Empty;
        for (int i = panelButtons.childCount - 1; i >= 0; i--)
            Destroy(panelButtons.GetChild(i).gameObject);
        panelButtonsBuilder?.Invoke();
    }

    private void ClosePanel() => panel.SetActive(false);

    private void AddPanelButton(string label, bool interactable, Action onClick)
    {
        var button = CreateButton(panelButtons, label, 28, new Color(0.25f, 0.3f, 0.4f, 1f));
        // 편성 패널은 버튼이 10개(콘텐츠 + 8방향 + 출발)라 610px 안에 들어가도록 높이를 잡는다.
        button.GetComponent<LayoutElement>().preferredHeight = 54;
        button.interactable = interactable;
        if (onClick != null)
            button.onClick.AddListener(() => onClick());
    }

    private void Toast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        toastUntil = Time.unscaledTime + 2f;
    }

    // ───────── UI 생성 ─────────
    private void BuildCanvas()
    {
        var canvasGo = new GameObject("LobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root = (RectTransform)canvasGo.transform;

        labelLayer = CreateRect("Labels", root);
        Stretch(labelLayer);

        // 상단 재화
        var top = CreateImage("ResourceBar", root, new Color(0, 0, 0, 0.55f));
        Anchor(top.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(760, 64));
        top.raycastTarget = false;
        resourceText = CreateText(top.rectTransform, "", 30, TextAnchor.MiddleLeft, Color.white);
        Stretch(resourceText.rectTransform, 20);

        // 방치 보상 버튼(오른쪽 위)
        var idleButton = CreateButton(root, "", 26, new Color(0.15f, 0.15f, 0.2f, 0.85f));
        Anchor((RectTransform)idleButton.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(620, 110));
        idleButtonText = idleButton.GetComponentInChildren<Text>();
        idleButtonImage = idleButton.GetComponent<Image>();
        idleButton.onClick.AddListener(OpenIdlePanel);

        // 상호작용 버튼(오른쪽 아래)
        interactButton = CreateButton(root, "", 32, new Color(0.2f, 0.45f, 0.3f, 0.95f));
        Anchor((RectTransform)interactButton.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 60), new Vector2(420, 120));
        interactText = interactButton.GetComponentInChildren<Text>();
        interactButton.onClick.AddListener(OnInteract);
        interactButton.gameObject.SetActive(false);

        // 공용 패널(화면 전체를 덮어 조이스틱 입력을 막는다)
        var dim = CreateImage("Panel", root, new Color(0, 0, 0, 0.6f));
        Stretch(dim.rectTransform);
        panel = dim.gameObject;
        var box = CreateImage("Box", dim.rectTransform, new Color(0.1f, 0.11f, 0.14f, 0.97f));
        Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 960));

        panelTitle = CreateText(box.rectTransform, "", 40, TextAnchor.MiddleLeft, Color.white);
        Anchor(panelTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -20), new Vector2(760, 70));

        var close = CreateButton(box.rectTransform, "닫기", 28, new Color(0.45f, 0.2f, 0.2f, 1f));
        Anchor((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(140, 60));
        close.onClick.AddListener(ClosePanel);

        panelBody = CreateText(box.rectTransform, "", 28, TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.9f));
        Anchor(panelBody.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -100), new Vector2(920, 220));

        panelButtons = CreateRect("Buttons", box.rectTransform);
        Anchor(panelButtons, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -330), new Vector2(920, 610));
        var layout = panelButtons.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        panel.SetActive(false);

        // 알림
        toastText = CreateText(root, "", 32, TextAnchor.MiddleCenter, Color.white);
        Anchor(toastText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(1000, 60));
        toastText.gameObject.AddComponent<Outline>().effectColor = Color.black;
        toastText.gameObject.SetActive(false);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(Transform parent, string content, int size, TextAnchor anchor, Color color)
    {
        var rect = CreateRect("Text", parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Button CreateButton(Transform parent, string label, int size, Color color)
    {
        var image = CreateImage("Button", parent, color);
        var button = image.gameObject.AddComponent<Button>();
        image.gameObject.AddComponent<LayoutElement>();
        var colors = button.colors;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        button.colors = colors;
        var text = CreateText(image.rectTransform, label, size, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform, 12);
        return button;
    }

    private static void Stretch(RectTransform rect, float padding = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding * 0.5f);
        rect.offsetMax = new Vector2(-padding, -padding * 0.5f);
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // ───────── 표시 문자열 ─────────
    private static string ResourceName(ResourceType type) => type switch
    {
        ResourceType.Wood => "목재",
        ResourceType.Stone => "석재",
        ResourceType.Metal => "금속",
        ResourceType.ManaEssence => "마력 정수",
        ResourceType.SpecialMaterial => "특수 건축 재료",
        _ => type.ToString(),
    };

    private static string FormatCost(IEnumerable<ResourceAmount> cost) =>
        string.Join(", ", cost.Select(c => $"{ResourceName(c.type)} {c.amount}"));

    private string GolemName(string id) => State.Config.FindGolem(id)?.displayName ?? id;

    private static string FormatDuration(long seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
    }
}
