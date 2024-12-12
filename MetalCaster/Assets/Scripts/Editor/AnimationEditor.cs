using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;

// Add in better assignment
// Add in better rendering order
// Refactor collisions
// Add in custom event support 
public class AnimationEditor : EditorWindow
{
    private enum DragType { DRAGGING, START_RESIZE, END_RESIZE };

    private AnimationDataHolder selectedData;
    private GameObject selected;
    private AnimationClip selectedAnimation;
    private Animator selectedAnimator;

    private const float HEIGHT   = 25;
    private const float SPACING  = 5;
    private const float X_MARGIN = 10;
    private const float Y_MARGIN = 25;

    private const float BUTTON_WIDTH  = 200;
    private const float BUTTON_HEIGHT = 25;

    private const float SCRUB_HEIGHT = 10;
    private const float SCRUB_WIDTH  = 40;

    private const float EDGE_THRESHOLD = 10;

    private DragType dragType;
    private int draggingIndex = -1;
    private float currentProgression = 0;
    private double lastTime = 0;

    private bool playing   = false;
    private bool scrubbing = false;

    private float Scale
    {
        get
        {
            return (position.width - 2 * X_MARGIN) / selectedAnimation.length;
        }
    }

    [MenuItem("Tools/Animation Editor")]
    public static void ShowWindow() => GetWindow<AnimationEditor>("Animation Editor");

    private void OnEnable()
    {
        EditorApplication.update += UpdateAnimatorProgression;
        lastTime = EditorApplication.timeSinceStartup;
    }

    private void OnDisable() => EditorApplication.update -= UpdateAnimatorProgression;

    private void OnSelectionChange()
    {
        GetSelection();

        if (Selection.activeGameObject == null) return;

        Repaint();
        UpdateAnimatorProgression();
    }

    private void OnGUI()
    {
        if (selectedAnimator == null || selectedData == null)
        {
            EditorGUILayout.LabelField("Select a GameObject with an Animator.");
            return;
        }

        ButtonDisplay();

        Event e = Event.current;

        var data = selectedData.Get(selectedAnimation);

        if (data != null)
        {
            selectedData.ReassignNames();

            EditorGUI.DrawRect(new Rect(X_MARGIN, SPACING + HEIGHT + Y_MARGIN, position.width - (2.0f * X_MARGIN), HEIGHT), Color.blue);
            ShowScrubber(data, e);
            DisplayAnimationData(data, e);
            HandleDrops(data, e);
        }

        switch (e.type)
        {
            case EventType.ContextClick:
                ShowGeneric();
                e.Use();
                break;
        }
    }

    private void GetSelection()
    {
        GameObject selectedObj = Selection.activeGameObject;

        if (selectedObj == null) return;

        Animator anim = selectedObj.GetComponent<Animator>();

        if (anim != null)
        {
            selected         = selectedObj;
            selectedAnimator = anim;
            selectedData     = selected.GetComponent<AnimationDataHolder>();
            if (selectedData == null) selectedData = Undo.AddComponent<AnimationDataHolder>(selected);

            EditorGUIUtility.PingObject(selected);
        }
    }

    private void ShowGeneric()
    {
        GenericMenu menu = new();
        menu.AddDisabledItem(new GUIContent("Create animation data here!"));

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Create Game Object Animation Data"), false,     () => CreateAnimationDataAsset<GameObjectAnimationData>());
        menu.AddItem(new GUIContent("Create Sound Effect Animation Data"), false,    () => CreateAnimationDataAsset<SoundEffectAnimationData>());
        menu.AddItem(new GUIContent("Create Unity Event Animation Data"), false,     () => CreateAnimationDataAsset<UnityEventAnimationData>());
        menu.AddItem(new GUIContent("Create Particle System Animation Data"), false, () => CreateAnimationDataAsset<ParticleSystemAnimationData>());

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Clear Current Animation data"), false, () => DeleteAssets());
        menu.AddItem(new GUIContent("Clear All Animation Data"), false,     () => selectedData.ClearAll());

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Reset Scrubber"), false, () => { currentProgression = 0; });

        menu.ShowAsContext();
    }

    private void UpdateAnimatorProgression()
    {
        if (playing && selectedAnimator != null && selectedData != null && selectedAnimation != null)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            double DT = currentTime - lastTime;
            lastTime = currentTime;

            float length = selectedAnimation.length;

            currentProgression += (float)DT;
            currentProgression %= length;

            selectedData.Play(currentProgression / length);

            Repaint();
        }
    }

    private void ButtonDisplay()
    {
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Select Animation:");
        var animationClips = selectedAnimator.runtimeAnimatorController.animationClips;
        var animationNames = new string[animationClips.Length];
        for (int i = 0; i < animationClips.Length; i++)
            animationNames[i] = animationClips[i].name;

        int selectedIndex = Array.IndexOf(animationClips, selectedAnimation);
        selectedIndex     = EditorGUILayout.Popup(selectedIndex, animationNames);

        if (selectedIndex >= 0 && selectedIndex < animationClips.Length)
        {
            selectedAnimation = animationClips[selectedIndex];
        }

        if (selectedAnimation == null)
        {
            EditorGUILayout.LabelField("No animation selected.");
            GUILayout.EndHorizontal();
            return;
        }

        if (GUILayout.Button("Clear Animation Data", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT))) DeleteAssets();

        string playingLabel = playing ? "Stop" : "Play";
        if (GUILayout.Button(playingLabel, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
        {
            playing = !playing;
            selectedData.ReloadData(selectedAnimation);
            lastTime = EditorApplication.timeSinceStartup;
            EditorUtility.SetDirty(selectedData);
        }

        GUILayout.EndHorizontal();
    }

    private string GenerateName()
    {
        string name = "0";

        if (selectedData.Get(selectedAnimation) != null) name = selectedData.Get(selectedAnimation).Count.ToString();

        return "AnimData" + name + ".asset";
    }

    private void AddAsset<T>(T obj, string name) where T : AnimationData
    {
        if (selectedAnimation == null)
        {
            Debug.LogWarning("Selected animation has been cleared!");
            return;
        }

        string objectName    = selected.name;
        string animationName = selectedAnimation.name;

        string baseFolder    = "Assets/Animation Data";
        if (!AssetDatabase.IsValidFolder(baseFolder)) AssetDatabase.CreateFolder("Assets", "Animation Data");

        string folderPath    = $"{baseFolder}/{objectName}";
        if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder(baseFolder, objectName);

        string animPath      = $"{folderPath}/{animationName}";
        if (!AssetDatabase.IsValidFolder(animPath)) AssetDatabase.CreateFolder(folderPath, animationName);

        AssetDatabase.CreateAsset(obj, animPath + "/" + name);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        selectedData.Add(selectedAnimation, obj);

        EditorUtility.SetDirty(selectedData);
        EditorUtility.SetDirty(obj);
    }

    private void DeleteAsset(string name)
    {
        string objectName = selected.name;
        string animationName = selectedAnimation.name;
        string fullPath = $"Assets/Animation Data/{objectName}/{animationName}";

        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            Debug.LogWarning("Folder with path: " + fullPath + " is not found!");
            return;
        }

        string[] assets = AssetDatabase.FindAssets("", new[] { fullPath });

        foreach (var asset in assets)
        {
            string path = AssetDatabase.GUIDToAssetPath(asset);
            if (System.IO.Path.GetFileName(path).Equals(name)) {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return;
            }
        }
    }

    private void DeleteAssets()
    {
        string objectName    = selected.name;
        string animationName = selectedAnimation.name;
        string fullPath      = $"Assets/Animation Data/{objectName}/{animationName}";

        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            Debug.LogWarning("Folder with path: " + fullPath + " is not found!");
            return;
        }

        string[] assets = AssetDatabase.FindAssets("", new[] { fullPath });

        foreach (var asset in assets)
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(asset));
        }

        if (AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.DeleteAsset(fullPath);
        }

        selectedData.Clear(selectedAnimation);
        EditorUtility.SetDirty(selectedData);
    }

    private void HandleDrops(List<AnimationData> data, Event e)
    {
        if (e.type == EventType.DragUpdated || e.type == EventType.DragExited)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.AcceptDrag();

            if (e.type == EventType.DragExited)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    float start = data[i].start * Scale;
                    float end = data[i].end * Scale;
                    float width = Mathf.Abs(end - start);
                    Rect test = new(X_MARGIN + start, (SPACING + HEIGHT) * (i + 1) + SPACING + BUTTON_HEIGHT + Y_MARGIN, width, HEIGHT);

                    if (test.Contains(e.mousePosition))
                    {
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            selectedData.Assign(data[i], obj);
                        }
                    }
                }
            }
        }
    }

    private void DisplayAnimationData(List<AnimationData> data, Event e)
    {
        Vector2 mousePos = e.mousePosition;

        for (int i = 0; i < data.Count; ++i)
        {
            float start = data[i].start * Scale;
            float end   = data[i].end * Scale;
            float width = Mathf.Abs(end - start);

            Rect rect = new(X_MARGIN + start, (SPACING + HEIGHT) * (i + 1) + SPACING + BUTTON_HEIGHT + Y_MARGIN, width, HEIGHT);
            Rect bg   = new(X_MARGIN, (SPACING + HEIGHT) * (i + 1) + SPACING + BUTTON_HEIGHT + Y_MARGIN, position.width - (X_MARGIN * 2), HEIGHT);

            EditorGUI.DrawRect(bg,   new Color(0,0,0,0.25f));
            EditorGUI.DrawRect(rect, data[i].Visual());
            EditorGUI.LabelField(rect, data[i].name, new GUIStyle(GUIStyle.none) { normal = new GUIStyleState() { textColor = Color.black } });

            if (rect.Contains(mousePos))
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (e.shift)
                    {
                        DeleteAsset(data[i].animName);
                        selectedData.Remove(selectedAnimation, data[i]);
                        e.Use();
                    }
                    else
                    {
                        draggingIndex = i;

                        if (Mathf.Abs(mousePos.x - rect.xMin) <= EDGE_THRESHOLD) dragType = DragType.START_RESIZE;
                        else if (Mathf.Abs(mousePos.x - rect.xMax) <= EDGE_THRESHOLD) dragType = DragType.END_RESIZE;
                        else dragType = DragType.DRAGGING;

                        e.Use();
                    }
                }
            }

            if (draggingIndex == i)
            {
                EditorGUI.DrawRect(rect, new Color(1, 1, 1, 1));
            }
        }

        if (draggingIndex != -1 && e.type == EventType.MouseDrag)
        {
            AnimationData animData = data[draggingIndex];
            float deltaX = e.delta.x / Scale;

            switch (dragType)
            {
                case DragType.DRAGGING:
                    if (animData.start + deltaX >= 0)
                    {
                        animData.start += deltaX;
                        animData.end += deltaX;
                    }
                    else
                    {
                        float diff = deltaX + animData.start;

                        animData.start = 0;
                        animData.end += deltaX - diff;
                    }
                    break;

                case DragType.START_RESIZE:
                    animData.start = Mathf.Min(animData.start + deltaX, animData.end - 0.01f);
                    break;

                case DragType.END_RESIZE:
                    animData.end += deltaX;
                    break;
            }

            EditorUtility.SetDirty(animData);
            EditorUtility.SetDirty(selectedData);

            Repaint();
            e.Use();
        }

        if (draggingIndex != -1 && e.type == EventType.MouseUp)
        {
            draggingIndex = -1;
            e.Use();
        }
    }

    private void ShowScrubber(List<AnimationData> data, Event e)
    {
        Vector2 mousePos = e.mousePosition;

        EditorGUI.DrawRect(new Rect(
        new Vector2(X_MARGIN + currentProgression * Scale, Y_MARGIN + SCRUB_HEIGHT),
        new Vector2(1, position.size.y - (2.0f * Y_MARGIN))),
        Color.yellow);

        Rect handleRect = new(
            new Vector2(X_MARGIN + currentProgression * Scale - (SCRUB_WIDTH / 2.0f), Y_MARGIN + SCRUB_HEIGHT),
            new Vector2(SCRUB_WIDTH, SCRUB_HEIGHT));

        EditorGUI.DrawRect(handleRect, Color.yellow);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (handleRect.Contains(mousePos) && e.button == 0)
                {
                    if (data != null)
                    {
                        foreach (var d in data)
                        {
                            if (!d.isPlaying) d.Exit();
                        }
                    }

                    scrubbing = true;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (scrubbing)
                {
                    float deltaX = e.delta.x / Scale;
                    currentProgression += deltaX;

                    currentProgression = Mathf.Clamp(currentProgression, 0, (position.width - (X_MARGIN * 2.0f)) * Scale);

                    selectedData.Play(currentProgression / selectedAnimation.length);

                    e.Use();
                    Repaint();
                }
                break;

            case EventType.MouseUp:
                if (scrubbing && e.button == 0)
                {
                    scrubbing = false;
                    selectedData.ReloadData(selectedAnimation);
                    e.Use();
                }
                break;
        }
    }

    private void CreateAnimationDataAsset<T>() where T : AnimationData
    {
        T data = CreateInstance<T>();

        string name = GenerateName();
        data.Init(0, 1, name);
        AddAsset(data, name);
    }
}

#endif
