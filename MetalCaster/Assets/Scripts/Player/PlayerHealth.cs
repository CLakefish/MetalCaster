using UnityEngine;

public class PlayerHealth : Health
{
    public override void OnHit(int damage)
    {
        base.OnHit(damage);
    }

    public override void OnDeath()
    {
        
    }
}
