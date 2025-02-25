using System.IO;
using UnityEngine;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(GameDataManager))]
public class GameDataManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        serializedObject.UpdateIfRequiredOrScript();

        var data = (GameDataManager)target;

        if (GUILayout.Button("Create Save")) data.CreateSave();

        base.OnInspectorGUI();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

public class GameDataManager : MonoBehaviour
{
    [SerializeField] private SaveData saveData;
    [SerializeField] private string saveFolderName;

    [Header("Content")]
    [SerializeField] private ContentManager contentManager;

    public static GameDataManager Instance;

    public ContentManager ContentManager => contentManager;
    public SaveData       ActiveSave     => saveData;

    private string FilePath {
        get {
            return Path.Combine(saveFolderName, saveData.SaveName);
        }
    }

    private void OnEnable() {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ActiveSave.SaveAltered += Write;
        contentManager.Enable();

        ActiveSave.RecheckSave();

        saveFolderName = Path.Combine(Application.persistentDataPath, "MetalCaster Data");

        if (!Directory.Exists(saveFolderName)) {
            Directory.CreateDirectory(saveFolderName);
        }
        else {
            Read();
        }
    }

    public void Update()
    {
        if (saveData == null) return;

        saveData.IncrementTime(Time.unscaledDeltaTime);
    }

    public bool Read()
    {
        if (File.Exists(FilePath)) {
            string fileContents = File.ReadAllText(FilePath);
            JsonUtility.FromJsonOverwrite(fileContents, saveData);

            ActiveSave.RecheckSave();

            return true;
        }

        return false;
    }

    public void Write()
    {
        string jsonTranslated = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(FilePath, jsonTranslated);
    }

    public void CreateSave()
    {
        SaveData data = ScriptableObject.CreateInstance<SaveData>();
#if UNITY_EDITOR
        AssetDatabase.CreateAsset(data, "Assets/Scripts/Utilities/SaveSystem/New Save.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.SetDirty(data);
#endif
        saveData = data;
        saveData.SetSaveName("New Save.json");
        Write();
    }
}
