using UnityEngine;

public class Placeable : MonoBehaviour
{
    protected Bullet bullet;
    protected Player player;

    public void ProvideBullet(ref Bullet bullet, Player player)
    {
        this.bullet = bullet;
        this.player = player;
    }
}
