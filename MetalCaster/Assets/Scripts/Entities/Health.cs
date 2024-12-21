using UnityEngine;

[System.Serializable]
public class Health : MonoBehaviour
{
    [SerializeField] private int healthPoints;
    [SerializeField] private int maxHealthPoints;

    public int HealthPoints    => healthPoints;
    public int MaxHealthPoints => maxHealthPoints;

    public System.Action damaged;
    public System.Action killed;

    public void Damage(int total)
    {
        damaged?.Invoke();

        if (healthPoints - total <= 0)
        {
            healthPoints = 0;
            killed?.Invoke();
            return;
        }

        healthPoints -= total;
    }
}
