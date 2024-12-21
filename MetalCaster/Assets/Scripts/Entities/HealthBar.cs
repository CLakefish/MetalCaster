using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text text;

    public void Start()
    {
        slider.maxValue = health.MaxHealthPoints;
        slider.value    = health.HealthPoints;

        text.text = health.HealthPoints.ToString();

        health.damaged += ModifyValue;
    }

    public virtual void ModifyValue()
    {
        slider.value = health.HealthPoints;
        text.text    = health.HealthPoints.ToString();
    }
}
