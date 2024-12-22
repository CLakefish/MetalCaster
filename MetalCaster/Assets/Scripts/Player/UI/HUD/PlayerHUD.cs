using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerHUD : Player.PlayerComponent
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform HUD;
    [SerializeField] private HealthBar healthBar;

    [Header("Weapon HUD")]
    [SerializeField] private RectTransform weaponHUDHolder;
    [SerializeField] private RectTransform weaponHUDPrefab;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color deselectedColor;

    [Header("Ammo")]
    [SerializeField] private TMP_Text ammoCount;

    private readonly Dictionary<Weapon, RectTransform> equippedWeaponHUD = new();
    private Weapon selectedWeapon;

    private void Start()
    {
        PlayerWeapon.Fire           += SetAmmoCount;
        PlayerWeapon.ReloadStart    += () => ammoCount.text = "RELOADING";
        PlayerWeapon.ReloadFinished += SetAmmoCount;

        PlayerWeapon.Added += (Weapon weapon) =>
        {
            RectTransform rect = Instantiate(weaponHUDPrefab, weaponHUDHolder);
            equippedWeaponHUD.Add(weapon, rect);
            CheckSelected(weapon);
        };

        PlayerWeapon.Removed += (Weapon weapon) =>
        {
            Destroy(equippedWeaponHUD[weapon].gameObject);
            equippedWeaponHUD.Remove(weapon);
        };

        PlayerWeapon.Swap += (Weapon weapon) =>
        {
            SetAmmoCount();
            CheckSelected(weapon);
        };
    }

    private void SetAmmoCount()
    {
        if (PlayerWeapon.Weapon == null)
        {
            ammoCount.text = "...";
            return;
        }

        ammoCount.text = PlayerWeapon.Weapon.WeaponData.shotCount.ToString();
    }

    private void CheckSelected(Weapon weapon)
    {
        if (!equippedWeaponHUD.TryGetValue(weapon, out RectTransform rect)) return;

        if (selectedWeapon != null && equippedWeaponHUD.TryGetValue(selectedWeapon, out RectTransform prevRect))
        {
            prevRect.GetComponent<RawImage>().color = deselectedColor;
        }

        selectedWeapon = weapon;
        rect.GetComponent<RawImage>().color = selectedColor;
    }
}
