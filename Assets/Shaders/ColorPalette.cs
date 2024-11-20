using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Color Palette", fileName = "Color Palette")]
public class ColorPalette : ScriptableObject
{
    [SerializeField] public List<Color> colors = new List<Color>();
    [SerializeField] public float ditherValue;
}
