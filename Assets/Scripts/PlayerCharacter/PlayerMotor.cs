using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAdminPrivileges.PlayerCharacter
{
    /// <summary>
    /// PlayerMotor is responsible for handling the physical movement of the PlayerCharacter.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;

        [Header("Movement variables")]
        [SerializeField] private float moveSpeed = 5f;
        //[SerializeField] private float maxSpeed = 10.0f;
        /*        [SerializeField] private float acceleration = 10.0f;
                [SerializeField] private float deceleration = 10.0f;*/
        //[SerializeField] private float turnSpeed = 720f;
        private Vector3 currentVelocity = Vector3.zero;
        private bool canMove = true;

        // Buff multiplier — applied on top of base moveSpeed. Reset to 1f each run.
        private float speedMultiplier = 1f;

        //exposed variables
        public Vector3 CurrentVelocity => characterController.velocity;

        /// <summary>
        /// Apply a multiplicative speed buff for the current run.
        /// e.g. 1.25f = 25% faster
        /// </summary>
        public void ApplySpeedMultiplier(float multiplier)
        {
            speedMultiplier *= multiplier;
            Debug.Log($"[PlayerMotor] Speed multiplier now {speedMultiplier:F2} (base speed {moveSpeed})");
        }

        /// <summary>
        /// Reset speed multiplier to default. Call on run start.
        /// </summary>
        public void ResetSpeedMultiplier()
        {
            speedMultiplier = 1f;
        }

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        public void Move(Vector2 movementInput)
        {
            if (!canMove) return;
            // Build direction relative to the player
            Vector3 right = transform.right * movementInput.x;
            Vector3 forward = transform.forward * movementInput.y;

            Vector3 moveDirection = right + forward;

            characterController.Move(moveDirection * (moveSpeed * speedMultiplier) * Time.deltaTime);

        }

        public void MoveRaw(Vector3 movement)
        {
            characterController.Move(movement);
        }

        public void SetCanMove(bool value)
        {
            canMove = value;
        }
    }
}