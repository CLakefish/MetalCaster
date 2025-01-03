using UnityEngine;

public class Bubble : Projectile
{
    protected override void OnTrigger() {
        Destroy(gameObject);
    }
}
