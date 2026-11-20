// 거점 진행 상태를 JSON 파일로 저장·불러온다(Application.persistentDataPath).
using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "wildgolem_save.json";

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave => File.Exists(FilePath);

    // 저장 파일이 없거나 읽을 수 없으면 null을 돌려준다. 호출한 쪽에서 새 데이터를 만든다.
    public static SaveData Load()
    {
        if (!HasSave)
            return null;

        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            data?.Normalize();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 파일을 읽지 못했습니다: {FilePath}\n{e}");
            return null;
        }
    }

    // 임시 파일에 먼저 쓰고 바꿔 넣어, 저장 중 종료돼도 기존 파일이 깨지지 않게 한다.
    public static void Save(SaveData data)
    {
        try
        {
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(FilePath))
                File.Replace(temp, FilePath, null);
            else
                File.Move(temp, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"저장하지 못했습니다: {FilePath}\n{e}");
        }
    }

    public static void Delete()
    {
        if (HasSave)
            File.Delete(FilePath);
    }

    public static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
