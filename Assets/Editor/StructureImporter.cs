// 우편함(mailbox/deliveries)에 납품된 부서지는 구조물(kind = destructible_structure)을 게임 에셋과 프리팹으로 가져온다.
// - done.json이 있는 작업만 처리한다(지침 10장). 원본은 우편함에 그대로 두고 사본만 Assets로 옮긴다.
// - 텍스처는 픽셀이 뭉개지지 않도록 Point 필터·무압축·밉맵 없음으로 설정한다(약 67px/유닛, 골렘 픽셀 크기와 맞춤).
// - 프리팹 구성: 루트 BoxCollider(온전한 모양 크기) + DestructibleStructure, 조각마다 Rigidbody + 볼록 MeshCollider,
//   fractured_state는 꺼 둔다.
// 같은 메뉴를 다시 실행하면 바뀐 파일만 다시 복사하고, 재질·프리팹은 덮어써서 갱신한다.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class StructureImporter
{
    private const string DeliveriesFolder = "mailbox/deliveries"; // 프로젝트 루트 기준
    private const string ModelRoot = "Assets/Models/Structures";
    private const string PrefabRoot = "Assets/PreFab/Structures";
    private const string Kind = "destructible_structure";
    private const float PieceMass = 20f; // 임시값: 조각 하나의 질량(kg)

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
        public string kind;
        public string file;
        public string texture;
    }

    [MenuItem("Tools/Wild Golem/우편함 구조물 가져오기")]
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
                continue;

            DoneNote note = JsonUtility.FromJson<DoneNote>(File.ReadAllText(donePath));
            if (note?.items == null)
                continue;

            foreach (DoneItem item in note.items)
            {
                if (item.kind == Kind && !string.IsNullOrEmpty(item.name))
                    latest[item.name] = (item, jobDir);
            }
        }

        if (latest.Count == 0)
        {
            Debug.LogWarning("가져올 구조물이 없습니다(done.json에 kind = destructible_structure 항목 없음).");
            return;
        }

        int imported = 0;

        foreach (var pair in latest)
        {
            if (ImportStructure(pair.Value.item, pair.Value.source))
                imported++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"구조물 {imported}/{latest.Count}개를 가져왔습니다. 프리팹: {PrefabRoot}");
    }

    private static bool ImportStructure(DoneItem item, string sourceDir)
    {
        string sourceModel = Path.Combine(sourceDir, item.file);
        string sourceTexture = Path.Combine(sourceDir, item.texture);

        if (!File.Exists(sourceModel) || !File.Exists(sourceTexture))
        {
            Debug.LogError($"'{item.name}'의 납품 파일이 없습니다: {sourceModel}, {sourceTexture}");
            return false;
        }

        string folder = $"{ModelRoot}/{item.name}";
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(PrefabRoot);

        string modelPath = $"{folder}/{Path.GetFileName(item.file)}";
        string texturePath = $"{folder}/{Path.GetFileName(item.texture)}";

        CopyIfChanged(sourceModel, modelPath);
        CopyIfChanged(sourceTexture, texturePath);
        AssetDatabase.ImportAsset(modelPath);
        AssetDatabase.ImportAsset(texturePath);

        ConfigureTexture(texturePath);
        ConfigureModel(modelPath);

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        Material material = CreateOrUpdateMaterial($"{folder}/{item.name}.mat", texture);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

        if (model == null)
        {
            Debug.LogError($"'{item.name}' 모델을 불러오지 못했습니다: {modelPath}");
            return false;
        }

        return BuildPrefab(item.name, model, material);
    }

    private static void CopyIfChanged(string source, string destination)
    {
        if (File.Exists(destination) && File.ReadAllBytes(destination).SequenceEqual(File.ReadAllBytes(source)))
            return;

        File.Copy(source, destination, true);
    }

    private static void ConfigureTexture(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    private static void ConfigureModel(string path)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.materialImportMode = ModelImporterMaterialImportMode.None; // 재질은 아래에서 직접 만든다
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.addCollider = false;
        importer.isReadable = true; // 조각 MeshCollider용
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial(string path, Texture2D texture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static bool BuildPrefab(string name, GameObject model, Material material)
    {
        GameObject root = (GameObject)UnityEngine.Object.Instantiate(model);
        root.name = name;

        try
        {
            Transform intact = FindDeep(root.transform, $"{name}_intact_state");
            Transform fractured = FindDeep(root.transform, $"{name}_fractured_state");

            if (intact == null || fractured == null)
            {
                Debug.LogError($"'{name}' 모델에 {name}_intact_state 또는 {name}_fractured_state가 없습니다.");
                return false;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;

            // 루트 충돌체: 온전한 모양의 범위(루트 기준)
            Bounds bounds = LocalBounds(root.transform, intact);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;

            // 조각: 메시에 볼록 MeshCollider, 그 부모(offset, 조립 위치 보관)에 Rigidbody
            foreach (MeshFilter filter in fractured.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = true;

                GameObject body = filter.transform.parent != fractured ? filter.transform.parent.gameObject : filter.gameObject;
                if (!body.TryGetComponent(out Rigidbody rigidbody))
                    rigidbody = body.AddComponent<Rigidbody>();

                rigidbody.mass = PieceMass;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }

            fractured.gameObject.SetActive(false);

            DestructibleStructure structure = root.AddComponent<DestructibleStructure>();
            var serialized = new SerializedObject(structure);
            serialized.FindProperty("intactState").objectReferenceValue = intact.gameObject;
            serialized.FindProperty("fracturedState").objectReferenceValue = fractured.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/{name}.prefab");
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Bounds LocalBounds(Transform root, Transform target)
    {
        bool hasBounds = false;
        var bounds = new Bounds();

        foreach (MeshFilter filter in target.GetComponentsInChildren<MeshFilter>(true))
        {
            Bounds meshBounds = filter.sharedMesh.bounds;
            Vector3 min = meshBounds.min;
            Vector3 max = meshBounds.max;

            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                Vector3 local = root.InverseTransformPoint(filter.transform.TransformPoint(corner));

                if (!hasBounds)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }
        }

        return bounds;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null)
                return found;
        }

        return null;
    }
}
