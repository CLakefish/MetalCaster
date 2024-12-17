using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : Player.PlayerComponent
{
    [Header("Viewmodel")]
    [SerializeField] private Transform viewmodelHolder;

    [Header("Weapon references")]
    [SerializeField] private List<Weapon> weapons;

    [Header("Collisions")]
    [SerializeField] private LayerMask collisionLayers;
    public LayerMask CollisionLayers => collisionLayers;

    private (Weapon Weapon, GameObject Viewmodel) Selected;
    private int selectedIndex = 0;

    private void OnEnable() => SelectWeapon();

    private void Update()
    {
        if (PlayerInput.Mouse.Left.Held)
        {
            Selected.Weapon.Fire(this);
        }

        Selected.Weapon.UpdateWeapon();
    }

    private void SelectWeapon()
    {
        if (Selected.Viewmodel != null) Destroy(Selected.Viewmodel);

        Selected.Viewmodel = Instantiate(weapons[selectedIndex].gameObject, viewmodelHolder, false);
        Selected.Weapon    = Selected.Viewmodel.GetComponent<Weapon>();
    }
}
