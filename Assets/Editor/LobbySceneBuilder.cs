// 로비(거점) 씬에 자리표시 도형과 거점 시스템 오브젝트를 배치한다. 메뉴: Tools/Wild Golem/로비 거점 자리표시 배치
// - 조정값 에셋(LobbyConfig·건물·골렘 정의)은 없을 때만 임시값으로 만든다. 이미 있으면 인스펙터에서 바꾼 값을 덮어쓰지 않는다.
// - 씬의 "LobbyBase" 오브젝트는 다시 실행할 때마다 지우고 새로 만든다(자리표시 위치를 손으로 옮겼다면 다시 실행하지 않는다).
// - 그림은 아직 없다(L-9). 건물 그림이 생기면 각 지점의 Visual 자식만 바꾼다.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LobbySceneBuilder
{
    private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";
    private const string DataFolder = "Assets/ScriptableObject/Lobby";
    private const string MaterialFolder = "Assets/Material/Lobby";
    private const string RootName = "LobbyBase";

    [MenuItem("Tools/Wild Golem/로비 거점 자리표시 배치")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != LobbyScenePath)
        {
            EditorUtility.DisplayDialog("로비 거점 배치", $"{LobbyScenePath}를 연 상태에서 실행하세요.", "확인");
            return;
        }

        LobbyConfig config = CreateDataAssets();

        var old = GameObject.Find(RootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "로비 거점 배치");

        // 시스템
        var systems = new GameObject("LobbySystems");
        systems.transform.SetParent(root.transform);
        var manager = systems.AddComponent<LobbyManager>();
        var so = new SerializedObject(manager);
        so.FindProperty("config").objectReferenceValue = config;
        so.ApplyModifiedPropertiesWithoutUndo();
        systems.AddComponent<LobbyUI>();

        // 바닥 크기 기준으로 배치(바닥 Plane이 없으면 20×20으로 가정)
        Bounds ground = new Bounds(Vector3.zero, new Vector3(20, 0, 20));
        var plane = GameObject.Find("Plane");
        if (plane != null && plane.TryGetComponent(out Renderer planeRenderer))
            ground = planeRenderer.bounds;
        float hx = ground.extents.x, hz = ground.extents.z;
        Vector3 P(float fx, float fz) => new Vector3(ground.center.x + fx * hx, ground.max.y, ground.center.z + fz * hz);

        var gray = Mat("Stone", new Color(0.55f, 0.55f, 0.58f));
        var orange = Mat("Smelter", new Color(0.65f, 0.35f, 0.2f));
        var blue = Mat("Research", new Color(0.3f, 0.45f, 0.75f));
        var tan = Mat("Training", new Color(0.7f, 0.6f, 0.4f));
        var locked = Mat("Locked", new Color(0.3f, 0.3f, 0.32f));
        var gold = Mat("Chest", new Color(0.9f, 0.75f, 0.2f));
        var gate = Mat("Gate", new Color(0.45f, 0.3f, 0.5f));
        var trunk = Mat("Trunk", new Color(0.4f, 0.27f, 0.15f));
        var leaf = Mat("Leaf", new Color(0.25f, 0.55f, 0.25f));
        var sign = Mat("Sign", new Color(0.6f, 0.45f, 0.25f));

        var sites = new GameObject("Sites").transform;
        sites.SetParent(root.transform);

        Site(sites, "채석장", LobbySiteKind.Building, "quarry", P(-0.6f, 0.65f), new Vector3(3, 2, 3), gray);
        Site(sites, "제련소", LobbySiteKind.Building, "smelter", P(0f, 0.65f), new Vector3(3, 2.5f, 3), orange);
        Site(sites, "대장간", LobbySiteKind.Building, "forge", P(0.6f, 0.65f), new Vector3(3, 2, 3), locked);
        Site(sites, "연구소", LobbySiteKind.Research, "", P(-0.65f, 0.05f), new Vector3(3, 3, 3), blue);
        Site(sites, "방치 보상 상자", LobbySiteKind.IdleChest, "", P(0f, 0.3f), new Vector3(1.2f, 0.9f, 0.9f), gold);
        Site(sites, "훈련소", LobbySiteKind.Training, "", P(0.65f, 0.05f), new Vector3(3, 2, 3), tan);
        Site(sites, "수용소", LobbySiteKind.Building, "prison", P(-0.6f, -0.55f), new Vector3(3, 2, 3), locked);
        Site(sites, "성벽", LobbySiteKind.Building, "wall", P(-0.3f, -0.88f), new Vector3(4, 1.5f, 0.8f), locked);
        Site(sites, "원정 게이트", LobbySiteKind.Gate, "", P(0.05f, -0.88f), new Vector3(2.5f, 3, 0.8f), gate);

        // 숲: 배정 표지판 + 나무 4그루
        var forest = new GameObject("Forest").transform;
        forest.SetParent(root.transform);
        Vector3 forestCenter = P(0.6f, -0.55f);
        Site(forest, "숲 작업장", LobbySiteKind.ForestAssign, "", forestCenter + new Vector3(-2.5f, 0, 0), new Vector3(0.8f, 1.2f, 0.3f), sign);
        Vector3[] treeOffsets = { new Vector3(0.5f, 0, 1.8f), new Vector3(2.3f, 0, 0.6f), new Vector3(0.8f, 0, -1.6f), new Vector3(2.6f, 0, -1.5f) };
        for (int i = 0; i < treeOffsets.Length; i++)
            Tree(forest, $"나무 {i + 1}", forestCenter + treeOffsets[i], trunk, leaf);

        Boundary(root.transform, ground);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"로비 거점 자리표시를 배치했습니다. 바닥 크기 {ground.size.x:0.#}×{ground.size.z:0.#}");
    }

    // 지점 = 트리거(상호작용 범위) + Visual 자식(부딪히는 도형)
    private static void Site(Transform parent, string name, LobbySiteKind kind, string buildingId, Vector3 groundPos, Vector3 size, Material mat)
    {
        var site = new GameObject(name);
        site.transform.SetParent(parent);
        site.transform.position = groundPos;

        var trigger = site.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0, size.y * 0.5f, 0);
        trigger.size = size + new Vector3(3f, 1f, 3f);

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(site.transform, false);
        visual.transform.localPosition = new Vector3(0, size.y * 0.5f, 0);
        visual.transform.localScale = size;
        visual.GetComponent<Renderer>().sharedMaterial = mat;

        site.AddComponent<LobbySite>().Setup(kind, buildingId, name, size.y + 0.8f);
    }

    private static void Tree(Transform parent, string name, Vector3 groundPos, Material trunkMat, Material leafMat)
    {
        var tree = new GameObject(name);
        tree.transform.SetParent(parent);
        tree.transform.position = groundPos;

        var trigger = tree.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0, 1f, 0);
        trigger.radius = 1.6f;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(tree.transform, false);

        var trunkGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkGo.name = "Trunk";
        trunkGo.transform.SetParent(visual.transform, false);
        trunkGo.transform.localPosition = new Vector3(0, 0.9f, 0);
        trunkGo.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
        trunkGo.GetComponent<Renderer>().sharedMaterial = trunkMat;

        var leafGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leafGo.name = "Leaf";
        leafGo.transform.SetParent(visual.transform, false);
        leafGo.transform.localPosition = new Vector3(0, 2.2f, 0);
        leafGo.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
        leafGo.GetComponent<Renderer>().sharedMaterial = leafMat;
        Object.DestroyImmediate(leafGo.GetComponent<Collider>());

        tree.AddComponent<LobbySite>().Setup(LobbySiteKind.Tree, "", name, 3.2f);
        tree.AddComponent<LobbyTree>().SetVisual(visual);
    }

    // 바닥 밖으로 떨어지지 않게 보이지 않는 벽을 두른다.
    private static void Boundary(Transform parent, Bounds ground)
    {
        var boundary = new GameObject("Boundary").transform;
        boundary.SetParent(parent);
        const float thickness = 1f, height = 3f;
        Vector3 c = ground.center;
        void Wall(string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(boundary);
            go.transform.position = pos;
            go.AddComponent<BoxCollider>().size = size;
        }
        Wall("North", new Vector3(c.x, ground.max.y + height * 0.5f, ground.max.z + thickness * 0.5f), new Vector3(ground.size.x + 2, height, thickness));
        Wall("South", new Vector3(c.x, ground.max.y + height * 0.5f, ground.min.z - thickness * 0.5f), new Vector3(ground.size.x + 2, height, thickness));
        Wall("East", new Vector3(ground.max.x + thickness * 0.5f, ground.max.y + height * 0.5f, c.z), new Vector3(thickness, height, ground.size.z + 2));
        Wall("West", new Vector3(ground.min.x - thickness * 0.5f, ground.max.y + height * 0.5f, c.z), new Vector3(thickness, height, ground.size.z + 2));
    }

    private static Material Mat(string name, Color color)
    {
        EnsureFolder(MaterialFolder);
        string path = $"{MaterialFolder}/Lobby_{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ───────── 조정값 에셋(임시값) ─────────
    private static LobbyConfig CreateDataAssets()
    {
        EnsureFolder(DataFolder);

        var quarry = Building("quarry", "채석장", "석재를 생산한다.", false, true, ResourceType.Stone, 60, 30,
            new ResourceAmount(ResourceType.Wood, 50), new ResourceAmount(ResourceType.Metal, 20));
        var smelter = Building("smelter", "제련소", "금속을 생산한다.", false, true, ResourceType.Metal, 30, 15,
            new ResourceAmount(ResourceType.Wood, 50), new ResourceAmount(ResourceType.Stone, 50));
        var forge = Building("forge", "대장간", "장비 제작 및 강화.", true, false, ResourceType.Metal, 0, 0);
        var prison = Building("prison", "수용소", "전향 마족 관리.", true, false, ResourceType.Metal, 0, 0);
        var wall = Building("wall", "성벽", "거점 방어 및 침공 대응.", true, false, ResourceType.Metal, 0, 0);

        // 시험용 시작 골렘(임시값): 편성·시너지 확인을 위해 세 속성 모두 보유로 시작한다.
        var red = Golem("red", "RED", GolemElement.Red);
        var green = Golem("green", "GREEN", GolemElement.Green);
        var blueGolem = Golem("blue", "BLUE", GolemElement.Blue);

        string path = $"{DataFolder}/LobbyConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<LobbyConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<LobbyConfig>();
            config.buildings = new[] { quarry, smelter, forge, prison, wall };
            config.golems = new[] { red, green, blueGolem };
            AssetDatabase.CreateAsset(config, path);
        }
        AssetDatabase.SaveAssets();
        return config;
    }

    private static BuildingDefinition Building(string id, string name, string description, bool isLocked, bool produces,
        ResourceType resource, float baseRate, float perLevel, params ResourceAmount[] cost)
    {
        string path = $"{DataFolder}/Building_{id}.asset";
        var def = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(path);
        if (def != null)
            return def;

        def = ScriptableObject.CreateInstance<BuildingDefinition>();
        def.id = id;
        def.displayName = name;
        def.description = description;
        def.locked = isLocked;
        def.startLevel = isLocked ? 0 : 1;
        def.producesResource = produces;
        def.producedResource = resource;
        def.productionPerHourAtLevel1 = baseRate;
        def.productionPerHourPerLevel = perLevel;
        def.baseUpgradeCost = cost;
        AssetDatabase.CreateAsset(def, path);
        return def;
    }

    private static GolemDefinition Golem(string id, string name, GolemElement element)
    {
        string path = $"{DataFolder}/Golem_{id}.asset";
        var def = AssetDatabase.LoadAssetAtPath<GolemDefinition>(path);
        if (def != null)
            return def;

        def = ScriptableObject.CreateInstance<GolemDefinition>();
        def.id = id;
        def.displayName = name;
        def.element = element;
        def.ownedAtStart = true;
        AssetDatabase.CreateAsset(def, path);
        return def;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
