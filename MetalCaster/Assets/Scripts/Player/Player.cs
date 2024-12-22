using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private PlayerCamera     playerCamera;
    [SerializeField] private PlayerInput      playerInput;
    [SerializeField] private PlayerWeapon     playerWeapon;
    [SerializeField] private PlayerHealth     playerHealth;
    [SerializeField] private PlayerHUD        playerHUD;

    [Header("Menus")]
    [SerializeField] private List<Menu> menus;

    [Header("Physics")]
    [SerializeField] public Rigidbody rb;

    public static Player Instance;

    public Camera GetCamera() {
        return Instance.playerCamera.Camera;
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    public void Awake()
    {
        playerMovement.SetPlayer(this);
        playerCamera.SetPlayer(this);
        playerInput.SetPlayer(this);
        playerWeapon.SetPlayer(this);
        playerHUD.SetPlayer(this);

        foreach (var menu in menus) menu.SetPlayer(this);
    }

    public class PlayerComponent : MonoBehaviour
    {
        private Player player;

        public void SetPlayer(Player player)    => this.player = player;
        public PlayerController GetController() { return player.playerMovement; }
        public PlayerCamera     GetCamera()     { return player.playerCamera; }
        public PlayerWeapon     GetWeapon()     { return player.playerWeapon; }

        protected Player           Player          => player;
        protected PlayerController PlayerMovement  => player.playerMovement;
        protected PlayerInput      PlayerInput     => player.playerInput;
        protected PlayerCamera     PlayerCamera    => player.playerCamera;
        protected PlayerWeapon     PlayerWeapon    => player.playerWeapon;
        protected PlayerHealth     PlayerHealth    => player.playerHealth;
        protected PlayerHUD        PlayerHUD       => player.playerHUD;
    }
}
