using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class AnimationDataEntry
{
    public List<AnimationData> data;
    public AnimationClip animation;
}

[RequireComponent(typeof(Animator))]
public class AnimationDataHolder : MonoBehaviour
{
    public Animator Animator { get; private set; }

    [SerializeField] public List<AnimationDataEntry> dataList  = new();
    [SerializeField] public List<SceneReference> referenceList = new();

    private readonly Dictionary<AnimationClip, List<AnimationData>> dataDictionary      = new();
    private readonly Dictionary<string, GameObject>                 referenceDictionary = new();

    private HashSet<AnimationData> dataHash = new();
    private float internalTime = 0;

    private void OnEnable()
    {
        Animator = GetComponent<Animator>();
        SyncDictionary();
        SyncReferences();
        ReassignNames();
    }

    private void Update() => CheckEvents();

    public void ReassignNames()
    {
        SyncReferences();

        if (referenceDictionary == null || referenceDictionary.Count == 0) return;

        for (int j = 0; j < dataList.Count; ++j)
        {
            for (int i = 0; i < dataList[j].data.Count; i++)
            {
                var r = dataList[j].data[i].objRef;

                if (r == null || r.obj == null) continue;

                if (referenceDictionary.TryGetValue(r.objectName, out var obj))
                {
                    string name = r.objectName;
                    r.Set(obj);
                    r.objectName = name;
                }
            }
        }
    }

    public void Assign(AnimationData data, Object obj)
    {
        data.HandleAssignment(obj);

        if (obj is GameObject objTemp)
        {
            referenceList.Add(new SceneReference(objTemp));
            referenceDictionary[objTemp.name] = objTemp;
        }
    }

    public void Add(AnimationClip clip, AnimationData data)
    {
        bool found = false;

        for (int i = 0; i < dataList.Count; ++i)
        {
            if (dataList[i].animation == clip)
            {
                dataList[i].data.Add(data);
                found = true;
                break;
            }
        }

        if (!found)
        {
            dataList.Add(new()
            {
                animation = clip,
                data = new() { data }
            });
        }

        SyncDictionary();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public List<AnimationData> Get(AnimationClip clip)
    {
        if (clip == null) return null;

        SyncDictionary();

        dataDictionary.TryGetValue(clip, out var data);

        return data;
    }

    public void Remove(AnimationClip clip, AnimationData data)
    {
        for (int i = 0; i < referenceList.Count; ++i)
        {
            if (referenceList[i].objectName == data.objRef.objectName)
            {
                referenceList.RemoveAt(i);
                break;
            }
        }

        for (int i = 0; i < dataList.Count; ++i)
        {
            if (dataList[i].animation != clip) continue;

            dataList[i].data.Remove(data);

            break;
        }

        if (data.objRef.obj != null) data.objRef.obj.SetActive(true);
        data.objRef.obj        = null;
        data.objRef.objectName = "";

        SyncDictionary();
        SyncReferences();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void Clear(AnimationClip clip)
    {
        ReloadData(clip);

        for (int i = 0; i < dataList.Count; ++i)
        {
            if (dataList[i].animation == clip)
            {
                for (int j = 0; j < dataList[i].data.Count; ++j)
                {
                    var r = dataList[i].data[j].objRef;
                    if (r == null || string.IsNullOrEmpty(r.objectName)) continue;

                    if (referenceDictionary.ContainsKey(r.objectName))
                    {
                        referenceList.RemoveAt(referenceList.IndexOf(r));
                        referenceDictionary.Remove(r.objectName);
                    }
                }

                dataList[i].data.Clear();
                break;
            }
        }

        dataDictionary[clip].Clear();
        SyncDictionary();
        SyncReferences();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void ClearAll()
    {
        dataList.Clear();
        referenceList.Clear();
        SyncDictionary();
        SyncReferences();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void ReloadData(AnimationClip clip)
    {
        var clips = Get(clip);

        foreach (var c in clips) c.Reload();
    }

    public void SyncDictionary()
    {
        dataDictionary.Clear();

        foreach (var entry in dataList)
        {
            dataDictionary[entry.animation] = entry.data;
        }
    }

    public void SyncReferences()
    {
        referenceDictionary.Clear();

        foreach (var entry in referenceList)
        {
            if (entry.obj == null || string.IsNullOrEmpty(entry.objectName)) continue;
            referenceDictionary[entry.objectName] = entry.obj;
        }
    }

    public void Play(float position)
    {
        Animator = GetComponent<Animator>();

        AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);

        Animator.Play(state.shortNameHash, 0, Mathf.Clamp01(position));
        Animator.Update(0);
        CheckEvents();
    }

    private void CheckEvents()
    {
        AnimatorClipInfo[] info = Animator.GetCurrentAnimatorClipInfo(0);
        AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
        HashSet<AnimationData> active = new();

        foreach (var i in info)
        {
            AnimationClip clip = i.clip;

            var list = Get(clip);

            if (list != null)
            {
                internalTime = state.normalizedTime % 1.0f * clip.length;

                foreach (var entry in list)
                {
                    if (entry.start <= internalTime && entry.end >= internalTime)
                    {
                        if (!entry.isPlaying)
                        {
                            entry.Enter();
                            entry.isPlaying = true;
                        }
                        else
                        {
                            entry.Update();
                        }

                        active.Add(entry);
                    }
                }
            }
        }

        foreach (var entry in dataHash)
        {
            if (!active.Contains(entry) && entry.isPlaying)
            {
                entry.isPlaying = false;
                entry.Exit();
            }
        }

        dataHash = active;
    }
}
