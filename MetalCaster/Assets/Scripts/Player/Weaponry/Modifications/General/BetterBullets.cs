using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Modifications/General/Better Bullets")]
public class BetterBullets : Modification.AlwaysEmpty
{
    [SerializeField] private float damageMult;
    [SerializeField] private float rateMult;

    public override void Modify(Weapon context)
    {
        context.AlteredData.damage    = Mathf.RoundToInt(damageMult * context.AlteredData.damage);
        context.AlteredData.fireTime *= rateMult;
    }
}
