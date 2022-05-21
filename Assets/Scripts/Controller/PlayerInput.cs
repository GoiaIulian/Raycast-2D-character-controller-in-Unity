using System;
using UnityEngine;

namespace Controller
{
    [RequireComponent(typeof(Player))]
    public class PlayerInput : MonoBehaviour
    {
        private Player _player;
        
        //========================================================================================//
        private void Start()
        {
            _player = GetComponent<Player>();
        }
        
        //========================================================================================//
        private void Update()
        {
            var directionalInput =  new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            _player.SetDirectionalInput(directionalInput);

            if (Input.GetKeyDown(KeyCode.Space)) _player.OnJumpInputDown();
            if (Input.GetKeyUp(KeyCode.Space)) _player.OnJumpInputUp();
        }
    }
}