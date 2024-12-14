using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using UnityEngine.Events;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(UnityEventAnimationData))]
public class UnityEventAnimationEditor : Editor
{
    private UnityEventAnimationData r;

    public override void OnInspectorGUI()
    {
        r = (UnityEventAnimationData)target;

        base.OnInspectorGUI();

        if (GUILayout.Button("Reload")) r.Reload();
    }
}

#endif

[System.Serializable]
public enum ScriptReferenceType { ENTER, UPDATE, EXIT, ALL }

[System.Serializable]
public class ScriptReference : SceneReference
{
    public ScriptReference(UnityEngine.Object obj) { 

        if (obj is not GameObject)
        {
            if (obj is MonoBehaviour mono) {
                scriptName = mono.GetType().Name;
                Set(mono.gameObject);
                return;
            }

            Debug.LogError("Not valid assignment given!");
        }
        else
        {
            Set(obj as GameObject);

            Debug.Log("Script not assigned!");
        }
    }

    [SerializeField] private string scriptName;
    [SerializeField] private string methodName;
    [SerializeField] public ScriptReferenceType type;

    public string ScriptName { get { return scriptName; }  set { scriptName = value; } }
    public string MethodName { get { return methodName; }  set { methodName = value; } }
}


[System.Serializable]
[CreateAssetMenu(menuName = "Animation Data/Unity Event")]
public class UnityEventAnimationData : AnimationData
{
    [SerializeField] private UnityEvent enter;
    [SerializeField] private UnityEvent update;
    [SerializeField] private UnityEvent exit;

    [SerializeField] private List<ScriptReference> references = new();

    public override Color Visual()
    {
        return Color.cyan;
    }

    public override void Reload()
    {
        enter.RemoveAllListeners();
        update.RemoveAllListeners();
        exit.RemoveAllListeners();

        foreach (var reference in references)
        {
            UnityAction action = GetAction(reference);

            if (action == null)
            {
                Debug.LogWarning("Object referenced is null!");
                continue;
            }

            switch (reference.type)
            {
                case ScriptReferenceType.ENTER:
                    enter.AddListener(action);
                    break;

                case ScriptReferenceType.UPDATE:
                    update.AddListener(action);
                    break;

                case ScriptReferenceType.EXIT:
                    exit.AddListener(action);
                    break;

                case ScriptReferenceType.ALL:
                    enter.AddListener(action);
                    update.AddListener(action);
                    exit.AddListener(action);
                    break;
            }
        }
    }

    private UnityAction GetAction(ScriptReference r)
    {
        if (r == null || string.IsNullOrEmpty(r.ScriptName) || string.IsNullOrEmpty(r.MethodName)) return null;

        GameObject obj = r.Get();
        if (obj == null) return null;

        Type scriptType = Type.GetType(r.ScriptName);
        if (scriptType == null) return null;

        Component c = obj.GetComponent(scriptType);
        if (c == null) return null;

        MethodInfo method = scriptType.GetMethod(r.MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) return null;

        return () => method.Invoke(c, null);
    }

    public override void HandleAssignment(UnityEngine.Object obj)
    {
        ScriptReference r = new(obj);
        for (int i = 0; i < references.Count; ++i)
        {
            if (references[i].objectName.Equals(r.objectName))
            {
                references[i] = r;
                return;
            }
        }

        references.Add(r);
    }

    public override void Enter() => enter?.Invoke();
    public override void Update() => update?.Invoke();
    public override void Exit() => exit?.Invoke();
}
