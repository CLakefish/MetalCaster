using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor(typeof(DitherShader))]
public class DitherShaderEditor : Editor
{
    private DitherShader shader;

    public override void OnInspectorGUI()
    {
        shader = (DitherShader)target;

        base.OnInspectorGUI();
        GUILayout.Space(15);

        bool isLoaded = shader.IsLoaded;

        if (!isLoaded)
        {
            EditorGUILayout.HelpBox("No color palette assigned, Scene will not render!", MessageType.Warning);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            string paletteLoad = !isLoaded ? "Load Palette" : "Reload Palette";
            if (GUILayout.Button(paletteLoad, GUILayout.Height(25), GUILayout.ExpandWidth(true))) shader.Load();

            if (GUILayout.Button("Refresh Shader", GUILayout.Height(25), GUILayout.ExpandWidth(true))) shader.Refresh();

            string enableShader = shader.Enabled ? "Disable Shader" : "Enable Shader";
            if (GUILayout.Button(enableShader, GUILayout.Height(25), GUILayout.ExpandWidth(true))) shader.Enabled = !shader.Enabled;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save Palette", GUILayout.Height(25), GUILayout.ExpandWidth(true))) shader.Save();
        }
    }
}

#endif

// https://discussions.unity.com/t/migrating-screen-ripple-effect-from-onrenderimage-to-universal-render-pipeline-urp/807569/3
// https://github.com/JBMiller/Unity-Ripple-Effect-Example/tree/master/RippleExample/Assets/RippleEffectAssets

public class DitherShader : MonoBehaviour
{
    [Header("Current Palette")]
    [SerializeField] private ColorPalette paletteReference;
    [SerializeField] public List<Color>   currentPalette;

    [Header("Shader Parameters")]
    [SerializeField] private Shader ditherer;
    [SerializeField] private float  spread;
    [SerializeField] private int    downSamples;

    public bool IsLoaded 
    {
        get { 
            return (currentPalette != null && currentPalette.Count > 0) || (paletteReference != null); 
        }
    }
    public bool Enabled { get; set; }

    private ComputeBuffer colorBuffer;
    private Material      ditherMaterial;

    private void OnEnable()
    {
        ditherMaterial           = new Material(ditherer);
        ditherMaterial.hideFlags = HideFlags.HideAndDontSave;

        Enabled = true;

        SetPalette(paletteReference);
    }

    private void OnDestroy()
    {
        if (colorBuffer != null) colorBuffer.Release();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Debug.Log("rendered");

        if (currentPalette.Count <= 0 || !Enabled)
        {
            Graphics.Blit(source, destination);
            return;
        }

        ditherMaterial.SetFloat("_Spread", spread);

        int width  = source.width;
        int height = source.height;

        RenderTexture[] textures    = new RenderTexture[8];
        RenderTexture currentSource = source;

        for (int i = 0; i < downSamples; ++i)
        {
            width  /= 2;
            height /= 2;

            if (height < 2) break;

            RenderTexture currentDestination = textures[i] = RenderTexture.GetTemporary(width, height, 0, source.format);
            Graphics.Blit(currentSource, currentDestination);
            currentSource = currentDestination;
        }

        RenderTexture dither = RenderTexture.GetTemporary(width, height, 0, source.format);
        Graphics.Blit(currentSource, dither, ditherMaterial, 0);
        Graphics.Blit(dither, destination, ditherMaterial, 1);
        RenderTexture.ReleaseTemporary(dither);

        for (int i = 0; i < downSamples; ++i) RenderTexture.ReleaseTemporary(textures[i]);
    }

    public void SetPalette(ColorPalette palette)
    {
        paletteReference = palette;
        Load();
    }

    public void Refresh()
    {
        if (colorBuffer != null) colorBuffer.Release();

        int colorCount = paletteReference.colors.Count;

        colorBuffer = new ComputeBuffer(colorCount, sizeof(float) * 4);
        Vector4[] colors = new Vector4[colorCount];

        for (int i = 0; i < colors.Length; ++i)
        {
            colors[i] = currentPalette[i];
        }

        colorBuffer.SetData(colors);
        spread = paletteReference.ditherValue;

        if (ditherMaterial == null) return;

        ditherMaterial.SetBuffer("_ColorPalette", colorBuffer);
        ditherMaterial.SetFloat("_NumLevels", colorCount);
    }

    public void Save()
    {
        paletteReference.colors      = currentPalette;
        paletteReference.ditherValue = spread;

    #if UNITY_EDITOR
        EditorUtility.SetDirty(paletteReference);
        AssetDatabase.SaveAssetIfDirty(paletteReference);
        EditorUtility.ClearDirty(paletteReference);
    #endif

        Refresh();
    }

    public void Load()
    {
        currentPalette = paletteReference.colors;
        spread         = paletteReference.ditherValue;
        Refresh();
    }
}
