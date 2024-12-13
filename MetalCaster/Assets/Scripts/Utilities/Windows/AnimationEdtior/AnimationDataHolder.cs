using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[System.Serializable]
public class AnimationDataEntry
{
    [SerializeField] public List<AnimationData> data;
    [SerializeField] public AnimationClip animation;
}

[RequireComponent(typeof(Animator))]
public class AnimationDataHolder : MonoBehaviour
{
    public Animator Animator { get; private set; }

    [HideInInspector, SerializeField] private List<AnimationDataEntry> dataList  = new();
    [HideInInspector, SerializeField] private List<SceneReference> referenceList = new();

    private readonly Dictionary<AnimationClip, List<AnimationData>> dataDictionary      = new();
    private readonly Dictionary<string, GameObject>                 referenceDictionary = new();

    private HashSet<AnimationData> dataHash = new();
    private float internalTime = 0;

    private void OnEnable()
    {
        Animator = GetComponent<Animator>();
        SyncDictionary();
        SyncReferences();
    }

    private void Update() => CheckEvents();

    public void EnableAll(AnimationClip clip) {
        var list = Get(clip);

        if (list == null) return;

        foreach (var data in list) {
            data.Reload();
        }
    }

    public void Assign(AnimationData data, Object obj)
    {
        data.HandleAssignment(obj);

        if (obj is GameObject objTemp)
        {
            referenceList.Add(new SceneReference(objTemp));
            SyncReferences();
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void Add(AnimationClip clip, AnimationData data)
    {
        bool found = false;

        for (int i = 0; i < dataList.Count; ++i)
        {
            if (dataList[i].animation.Equals(clip))
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
        EnableAll(clip);

        int index = dataList.FindIndex(indexData => indexData.animation == clip);

        if (index >= 0)
        {
            var data = dataList[index];

            foreach (var d in data.data)
            {
                if (d == null) continue;
                if (d.objRef != null && d.objRef.obj != null)
                {
                    d.objRef.obj.SetActive(true);
                    referenceList.RemoveAll(item => item.objectName.Equals(d.objRef.objectName));
                }
            }

            data.data.Clear();
            dataList.RemoveAt(index);
        }

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

    public void Play(float position, AnimationClip clip)
    {
        Animator = GetComponent<Animator>();

        RuntimeAnimatorController controller = Animator.runtimeAnimatorController;
        string name = "";
        foreach (var i in controller.animationClips)
        {
            if (i == clip)
            {
                name = i.name;
                break;
            }
        }

        if (name == "")
        {
            Debug.LogWarning("Unable to find clip with name: " + clip.name);
            return;
        }

        Animator.Play(name, 0, position);
        Animator.Update(Time.deltaTime);
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
