using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private PlayerCamera     playerCamera;
    [SerializeField] private PlayerInput      playerInput;
    [SerializeField] private PlayerWeapon     playerWeapon;
    [SerializeField] private PlayerHealth     playerHealth;
                     
    [Header("UI")]
    [SerializeField] private PlayerHUD PlayerHUD;
    [SerializeField] private PauseMenu PauseMenu;

    [Header("Physics")]
    [SerializeField] public Rigidbody rb;
    [SerializeField] public CapsuleCollider capsuleCollider;
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public LayerMask hittableLayer;
    [SerializeField] public LayerMask enemyLayer;

    public PlayerWeapon PlayerWeapon => playerWeapon;
    public PlayerCamera PlayerCamera => playerCamera;

    private void OnEnable() {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        playerMovement.SetPlayer(this);
        playerCamera.SetPlayer(this);
        playerInput.SetPlayer(this);
        PlayerWeapon.SetPlayer(this);
        PlayerHUD.SetPlayer(this);
        PauseMenu.SetPlayer(this);
    } 

    public class PlayerComponent : MonoBehaviour
    {
        private Player player;

        public void SetPlayer(Player player) => this.player = player;

        protected Player           Player          => player;
        protected PlayerController PlayerMovement  => player.playerMovement;
        protected PlayerInput      PlayerInput     => player.playerInput;
        protected PlayerCamera     PlayerCamera    => player.playerCamera;
        protected PlayerWeapon     PlayerWeapon    => player.PlayerWeapon;
        protected PlayerHealth     PlayerHealth    => player.playerHealth;
        protected PlayerHUD        PlayerHUD       => player.PlayerHUD;


        protected Rigidbody rb                    => player.rb;
        protected CapsuleCollider CapsuleCollider => player.capsuleCollider;
        protected LayerMask GroundLayer           => player.groundLayer;
        protected LayerMask HittableLayer         => player.hittableLayer;
    }
}
