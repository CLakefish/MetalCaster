using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

[EditorWindowTitle(title = "Color Palette Creator")]
public class ColorPaletteCreator : EditorWindow
{
    [MenuItem("Tools/Color Palette Creator")]
    public static void ShowWindow() => GetWindow<ColorPaletteCreator>("Color Palette Creator");

    Texture2D tex;

    private void OnGUI()
    {
        tex = (Texture2D)EditorGUILayout.ObjectField("Image", tex, typeof(Texture2D), false);

        if (GUILayout.Button("Create Color Palette"))
        {
            HashSet<Color> colors = new();

            for (int x = 0; x < tex.width; ++x)
            {
                for (int y = 0; y < tex.height; ++y)
                {
                    var col = tex.GetPixel(x, y);
                    if (colors.Contains(col)) continue;

                    colors.Add(col);
                }
            }

            string baseFolder = "Assets/Palettes";
            if (!AssetDatabase.IsValidFolder(baseFolder)) AssetDatabase.CreateFolder("Assets", "Palettes");

            ColorPalette palette = ScriptableObject.CreateInstance<ColorPalette>();
            palette.colors       = colors.ToList();
            palette.ditherValue  = 0.1f;
            palette.name         = $"{tex.name} Palette";

            AssetDatabase.CreateAsset(palette, baseFolder + "/" + palette.name + ".asset");

            EditorUtility.SetDirty(palette);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}

#endif
