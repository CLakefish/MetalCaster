using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerWeapon playerWeapon;

    public void Awake()
    {
        playerMovement.SetPlayer(this);
        playerCamera.SetPlayer(this);
        playerInput.SetPlayer(this);
        playerWeapon.SetPlayer(this);
    }

    public class PlayerComponent : MonoBehaviour
    {
        private Player player;

        public void SetPlayer(Player player) => this.player = player;
        public PlayerController GetController() { return player.playerMovement; }
        public PlayerCamera     GetCamera()     { return player.playerCamera; }

        protected Player           Player          => player;
        protected PlayerController PlayerMovement  => player.playerMovement;
        protected PlayerInput      PlayerInput     => player.playerInput;
        protected PlayerCamera     PlayerCamera    => player.playerCamera;
        protected PlayerWeapon     PlayerWeapon    => player.playerWeapon;
    }
}
