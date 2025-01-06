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

    private readonly Dictionary<string, RectTransform> equippedWeaponHUD = new();
    private Weapon selectedWeapon;


    private void OnEnable() {
        canvas.enabled = true;
    }

    public void Update()
    {
        SetAmmoCount();
    }

    private void SetAmmoCount()
    {
        if (PlayerWeapon.Selected.Weapon == null)
        {
            ammoCount.text = "...";
            return;
        }

        ammoCount.text = PlayerWeapon.Selected.Weapon.AlteredData.shotCount.ToString();
    }

    public void AddWeapon(Weapon weapon)
    {
        if (equippedWeaponHUD.ContainsKey(weapon.WeaponName)) return;

        RectTransform rect = Instantiate(weaponHUDPrefab, weaponHUDHolder);
        equippedWeaponHUD.Add(weapon.WeaponName, rect);
    }

    public void SelectWeapon(Weapon weapon)
    {
        if (!equippedWeaponHUD.TryGetValue(weapon.WeaponName, out RectTransform rect)) return;

        if (selectedWeapon != null && equippedWeaponHUD.TryGetValue(selectedWeapon.WeaponName, out RectTransform prevRect)) {
            prevRect.GetComponent<RawImage>().color = deselectedColor;
        }

        selectedWeapon = weapon;
        rect.GetComponent<RawImage>().color = selectedColor;
    }

    public void RemoveWeapon(Weapon weapon)
    {
        if (equippedWeaponHUD.ContainsKey(weapon.WeaponName))
        {
            Destroy(equippedWeaponHUD[weapon.WeaponName].gameObject);
            equippedWeaponHUD.Remove(weapon.WeaponName);
        }
    }
}
