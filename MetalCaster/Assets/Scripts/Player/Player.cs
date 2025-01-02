using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public PlayerController PlayerMovement;
    [SerializeField] public PlayerCamera     PlayerCamera;
    [SerializeField] public PlayerInput      PlayerInput;
    [SerializeField] public PlayerWeapon     PlayerWeapon;
    [SerializeField] public PlayerHealth     PlayerHealth;

    [Header("UI")]
    [SerializeField] public PlayerHUD PlayerHUD;
    [SerializeField] public PauseMenu PauseMenu;

    [Header("Physics")]
    [SerializeField] public Rigidbody rb;
    [SerializeField] public CapsuleCollider capsuleCollider;
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public LayerMask hittableLayer;

    public static Player Instance;

    private void OnEnable() {
        if (Instance == null) Instance = this;

        PlayerMovement.SetPlayer(this);
        PlayerCamera.SetPlayer(this);
        PlayerInput.SetPlayer(this);
        PlayerWeapon.SetPlayer(this);
        PlayerHUD.SetPlayer(this);
        PauseMenu.SetPlayer(this);
    } 

    public class PlayerComponent : MonoBehaviour
    {
        private Player player;

        public void SetPlayer(Player player)    => this.player = player;

        protected Player           Player          => player;
        protected PlayerController PlayerMovement  => player.PlayerMovement;
        protected PlayerInput      PlayerInput     => player.PlayerInput;
        protected PlayerCamera     PlayerCamera    => player.PlayerCamera;
        protected PlayerWeapon     PlayerWeapon    => player.PlayerWeapon;
        protected PlayerHealth     PlayerHealth    => player.PlayerHealth;
        protected PlayerHUD        PlayerHUD       => player.PlayerHUD;


        protected Rigidbody rb                    => player.rb;
        protected CapsuleCollider CapsuleCollider => player.capsuleCollider;
        protected LayerMask GroundLayer           => player.groundLayer;
        protected LayerMask HittableLayer         => player.hittableLayer;
    }
}
