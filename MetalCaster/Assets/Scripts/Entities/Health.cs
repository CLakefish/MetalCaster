using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int healthPoints;
    [SerializeField] private int maxHealthPoints;

    public int HealthPoints    => healthPoints;
    public int MaxHealthPoints => maxHealthPoints;

    public void Damage(int total)
    {
        if (healthPoints - total <= 0)
        {
            healthPoints = 0;
            OnDeath();
            return;
        }

        OnHit(total);
    }

    public virtual void OnHit(int total) { 
        healthPoints -= total;
    }

    public virtual void OnDeath() {
        Destroy(gameObject);
    }
}
