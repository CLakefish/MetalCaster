using TMPro;
using UnityEngine;

public class Popup : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;

    public void Set(string title, string description)
    {
        this.title.text       = title;
        this.description.text = description;
    }
}
