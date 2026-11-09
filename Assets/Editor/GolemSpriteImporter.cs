// 우편함(mailbox/deliveries)에 납품된 골렘 스프라이트 시트를 게임 에셋으로 가져온다.
// - done.json이 있는 작업만 처리한다(지침 10장). 원본 시트는 우편함에 그대로 두고 사본만 Assets로 옮긴다.
// - 픽셀 그림이 뭉개지지 않도록 Point 필터·무압축·큰 최대 크기로 설정하고, done.json의 cell_size 칸 격자로 자른다(없으면 1028).
//   칸 해상도가 달라도 PPU를 맞춰 게임 화면 속 크기는 같게 유지한다.
// - done.json의 프레임별 tick으로 애니메이션 클립을 만들고, 대상 골렘의 Animator에 "<모션>_<방향>" 상태를 추가한다.
// 같은 메뉴를 다시 실행하면 바뀐 시트만 다시 복사하고, 슬라이스·클립·상태는 덮어써서 갱신한다.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class GolemSpriteImporter
{
    private const string DeliveriesFolder = "mailbox/deliveries"; // 프로젝트 루트 기준
    private const string SpriteRoot = "Assets/Sprites/Golems";
    private const string ClipRoot = "Assets/Animation/Golems";
    private const int DefaultCellSize = 1028; // done.json에 cell_size가 없을 때(20261109-001 납품분)
    private const int MaxTextureSize = 16384;

    // 칸 하나가 게임 월드에서 차지하는 크기(유닛). 기존 스프라이트(1028px 칸, PPU 800)와 같은 크기를 유지한다.
    // 칸 해상도가 달라도(예: 12px 픽셀용 86px 칸) PPU = 칸 크기 / 이 값으로 맞춰 화면 크기가 바뀌지 않는다.
    private const float WorldCellSize = 1028f / 800f;

    // 납품 이름의 대상 → 그 골렘이 쓰는 Animator Controller
    private static readonly Dictionary<string, string> ControllerByTarget = new Dictionary<string, string>
    {
        { "wildgolem", "Assets/Animation/Square.controller" },
        { "red", "Assets/Animation/New Animator Controller.controller" },
    };

    private static readonly HashSet<string> Motions = new HashSet<string> { "idle", "walk", "attack", "hit" };

    // 반복 재생하는 동작. 끝나도 기본 상태로 돌아가지 않는다.
    private static readonly HashSet<string> LoopMotions = new HashSet<string> { "idle", "walk" };

    private static readonly HashSet<string> Directions = new HashSet<string>
    {
        "front", "left", "right", "side_left", "side_right", "back_left", "back_right"
    };

    [Serializable]
    private class DoneNote
    {
        public string order;
        public DoneItem[] items;
    }

    [Serializable]
    private class DoneItem
    {
        public string name;
        public string file;
        public int frames;
        public int[] frame_duration_ticks;
        public int tick_rate;
        public int[] cell_size;

        public int CellSize => cell_size != null && cell_size.Length > 0 && cell_size[0] > 0 ? cell_size[0] : DefaultCellSize;
    }

    [MenuItem("Tools/Wild Golem/우편함 골렘 스프라이트 가져오기")]
    public static void ImportAll()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string deliveries = Path.Combine(projectRoot, DeliveriesFolder);

        if (!Directory.Exists(deliveries))
        {
            Debug.LogError($"우편함 납품 폴더가 없습니다: {deliveries}");
            return;
        }

        // 작업 번호 순으로 처리해 같은 이름이면 나중 작업이 이긴다.
        var latest = new Dictionary<string, (DoneItem item, string source)>();

        foreach (string jobDir in Directory.GetDirectories(deliveries).OrderBy(d => d, StringComparer.Ordinal))
        {
            string donePath = Path.Combine(jobDir, "done.json");

            if (!File.Exists(donePath))
            {
                Debug.Log($"[골렘 스프라이트] {Path.GetFileName(jobDir)}: done.json이 없어 건너뜁니다(저장 중으로 봅니다).");
                continue;
            }

            DoneNote note = JsonUtility.FromJson<DoneNote>(File.ReadAllText(donePath));

            if (note?.items == null)
                continue;

            foreach (DoneItem item in note.items)
            {
                if (item == null || string.IsNullOrEmpty(item.file))
                    continue;

                latest[item.name] = (item, Path.Combine(jobDir, item.file));
            }
        }

        int done = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var (item, source) in latest.Values)
            {
                if (CopySheet(item, source, projectRoot, out _))
                    done++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        // 임포트가 끝난 뒤에 설정·슬라이스·클립·상태를 만든다.
        foreach (var (item, source) in latest.Values)
        {
            if (!TryParseName(item.name, out string target, out string motion, out string direction))
                continue;

            if (!ControllerByTarget.ContainsKey(target))
                continue;

            string sheetPath = $"{SpriteRoot}/{target}/{item.name}.png";

            if (AssetImporter.GetAtPath(sheetPath) == null)
                continue;

            bool loop = LoopMotions.Contains(motion);
            ConfigureSheet(sheetPath, item);
            AnimationClip clip = BuildClip(sheetPath, item, target, loop);

            if (clip != null)
                AddState(target, $"{motion}_{direction}", clip, loop);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[골렘 스프라이트] 처리 완료: {latest.Count}개 항목, 복사·갱신 {done}개.");
    }

    // 납품 이름 "<대상>_<모션>_<방향>"을 나눈다. 방향은 side_left처럼 밑줄을 포함할 수 있다.
    private static bool TryParseName(string name, out string target, out string motion, out string direction)
    {
        target = motion = direction = null;
        string[] parts = name.Split(new[] { '_' }, 3);

        if (parts.Length < 3 || !Motions.Contains(parts[1]) || !Directions.Contains(parts[2]))
        {
            Debug.LogWarning($"[골렘 스프라이트] 이름 규칙에 맞지 않아 건너뜁니다: {name}");
            return false;
        }

        target = parts[0];
        motion = parts[1];
        direction = parts[2];
        return true;
    }

    private static bool CopySheet(DoneItem item, string source, string projectRoot, out string assetPath)
    {
        assetPath = null;

        if (!TryParseName(item.name, out string target, out _, out _))
            return false;

        // 컨트롤러를 지정하지 않은 대상(새 골렘)은 가져오지 않는다. 검수·그림체 승인 뒤 ControllerByTarget에 추가한다.
        if (!ControllerByTarget.ContainsKey(target))
        {
            Debug.Log($"[골렘 스프라이트] '{target}'은 가져오기 대상으로 지정되지 않아 건너뜁니다: {item.name}");
            return false;
        }

        if (!File.Exists(source))
        {
            Debug.LogError($"[골렘 스프라이트] done.json에 있는 파일이 없습니다: {source}");
            return false;
        }

        int cell = item.CellSize;

        if (!ReadPngSize(source, out int width, out int height) || height != cell || width != cell * item.frames)
        {
            Debug.LogError($"[골렘 스프라이트] 시트 크기가 {cell}×{cell} 칸 {item.frames}개와 맞지 않습니다: {source} ({width}×{height})");
            return false;
        }

        if (item.frame_duration_ticks == null || item.frame_duration_ticks.Length != item.frames || item.tick_rate <= 0)
        {
            Debug.LogError($"[골렘 스프라이트] 프레임 재생 간격 정보가 프레임 수와 맞지 않습니다: {item.name}");
            return false;
        }

        assetPath = $"{SpriteRoot}/{target}/{item.name}.png";
        string destination = Path.Combine(projectRoot, assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));

        if (File.Exists(destination) && File.ReadAllBytes(destination).SequenceEqual(File.ReadAllBytes(source)))
            return true;

        File.Copy(source, destination, true);
        return true;
    }

    // PNG 헤더(IHDR)에서 크기만 읽는다. 큰 시트를 통째로 불러오지 않기 위해서다.
    private static bool ReadPngSize(string path, out int width, out int height)
    {
        width = height = 0;
        byte[] header = new byte[24];

        using (FileStream stream = File.OpenRead(path))
        {
            if (stream.Read(header, 0, header.Length) != header.Length)
                return false;
        }

        width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        return true;
    }

    private static void ConfigureSheet(string assetPath, DoneItem item)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Multiple;
        // 칸 전체를 그대로 쓰도록 사각형 메시로 둔다. 프레임마다 그리는 영역이 달라지지 않게 하기 위해서다.
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = false;
        // 스프라이트는 2의 거듭제곱 크기 조정을 쓸 수 없다. 기본값(ToNearest)이 남으면 경고가 난다.
        settings.npotScale = TextureImporterNPOTScale.None;
        importer.SetTextureSettings(settings);

        int cell = item.CellSize;
        importer.spritePixelsPerUnit = cell / WorldCellSize;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = MaxTextureSize;

        TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
        platform.maxTextureSize = MaxTextureSize;
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(platform);

        // 칸 격자로 자른다. 피벗은 칸 중앙으로 두어 기존 Idle·Walk(중앙 피벗)와 높이를 맞추고,
        // 모든 프레임이 같은 칸을 쓰므로 팔을 뻗어도 몸이 흔들리지 않는다.
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        SpriteRect[] previous = provider.GetSpriteRects();
        var rects = new List<SpriteRect>();

        for (int i = 0; i < item.frames; i++)
        {
            string spriteName = $"{item.name}_{i}";
            // 다시 실행해도 같은 스프라이트 ID를 유지해야 클립 참조가 끊기지 않는다.
            SpriteRect rect = previous.FirstOrDefault(r => r.name == spriteName)
                              ?? new SpriteRect { name = spriteName, spriteID = GUID.Generate() };
            rect.rect = new Rect(i * cell, 0, cell, cell);
            rect.alignment = SpriteAlignment.Center;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.border = Vector4.zero;
            rects.Add(rect);
        }

        provider.SetSpriteRects(rects.ToArray());

        var nameFileIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameFileIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

        provider.Apply();
        importer.SaveAndReimport();
    }

    private static AnimationClip BuildClip(string sheetPath, DoneItem item, string target, bool loop)
    {
        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .ToDictionary(s => s.name);

        var keys = new List<ObjectReferenceKeyframe>();
        float time = 0f;
        Sprite last = null;

        for (int i = 0; i < item.frames; i++)
        {
            if (!sprites.TryGetValue($"{item.name}_{i}", out Sprite sprite))
            {
                Debug.LogError($"[골렘 스프라이트] 잘라 낸 스프라이트를 찾지 못했습니다: {item.name}_{i}");
                return null;
            }

            keys.Add(new ObjectReferenceKeyframe { time = time, value = sprite });
            time += item.frame_duration_ticks[i] / (float)item.tick_rate;
            last = sprite;
        }

        // Unity는 스프라이트 커브의 길이를 "마지막 키 + 1프레임(1/frameRate)"으로 잡는다.
        // 마지막 프레임이 1 tick보다 길 때만, 끝에서 1 tick 앞에 같은 스프라이트를 한 번 더 둔다.
        float frameStep = 1f / item.tick_rate;
        float lastKeyTime = keys[keys.Count - 1].time;

        if (time - frameStep > lastKeyTime + 0.0001f)
            keys.Add(new ObjectReferenceKeyframe { time = time - frameStep, value = last });

        string folder = $"{ClipRoot}/{target}";
        Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName, folder));
        AssetDatabase.Refresh();

        string clipPath = $"{folder}/{item.name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        // 납품 기준(24 tick/초)을 그대로 써서 키가 프레임 경계에 정확히 놓이게 한다.
        clip.frameRate = item.tick_rate;

        // 기존 클립과 같이 Animator와 같은 오브젝트의 SpriteRenderer를 바꾼다.
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

        AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    // 상태는 코드(GolemMotionAnimator)가 이름으로 직접 재생한다. 공격·피격은 끝나면 기본 상태(Idle)로 돌아가고,
    // 반복 동작(idle·walk)은 돌아가는 전환을 두지 않는다.
    private static void AddState(string target, string stateName, AnimationClip clip, bool loop)
    {
        if (!ControllerByTarget.TryGetValue(target, out string controllerPath))
        {
            Debug.LogWarning($"[골렘 스프라이트] '{target}'의 Animator Controller가 지정되지 않아 상태를 만들지 않았습니다.");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            Debug.LogError($"[골렘 스프라이트] Animator Controller를 찾지 못했습니다: {controllerPath}");
            return;
        }

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState defaultState = machine.defaultState;
        AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName);

        if (state == null)
        {
            int index = machine.states.Length;
            state = machine.AddState(stateName, new Vector3(600f, 40f * index, 0f));

            // 기본 상태가 비어 있으면 AddState가 새 상태를 기본으로 만들므로 원래 값으로 되돌린다.
            if (machine.defaultState != defaultState)
                machine.defaultState = defaultState;
        }

        state.motion = clip;

        if (!loop && defaultState != null && state.transitions.All(t => t.destinationState != defaultState))
        {
            AnimatorStateTransition back = state.AddTransition(defaultState);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.hasFixedDuration = true;
            back.duration = 0f;
        }

        EditorUtility.SetDirty(controller);
    }
}
