using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private PlayerInput playerInput;

    public void Awake()
    {
        playerMovement.SetPlayer(this);
        playerCamera.SetPlayer(this);
        playerInput.SetPlayer(this);
    }

    public abstract class PlayerComponent : MonoBehaviour
    {
        private Player player;

        public void SetPlayer(Player player) => this.player = player;

        protected Player Player                   => player;
        protected PlayerController playerMovement => player.playerMovement;
        protected PlayerInput playerInput         => player.playerInput;
        protected PlayerCamera playerCamera       => player.playerCamera;
    }
}
