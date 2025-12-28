using UnityEngine;
using ProjectAdminPrivileges.Combat.Weapons;
using ProjectAdminPrivileges.Audio;
using ProjectAdminPrivileges.Abilities;

namespace ProjectAdminPrivileges.PlayerCharacter
{
    public class PlayerController : MonoBehaviour
    {
        //input
        PlayerInputActions inputActions;
        private Vector2 movementInput = Vector2.zero;
        private PlayerMotor playerMotor;
        private PlayerAnimatorController animatorController;
        [SerializeField]private PlayerWeaponHandler weaponHandler;
        [SerializeField]private Transform aimPoint;

        [SerializeField] private LayerMask groundMask;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private AbilityManager abilityManager;

        [SerializeField] private RectTransform crosshairUI;

        [Header("Footstep Audio")]
        [SerializeField] private AudioClipData footstepSound;
        [SerializeField] private float stepFrequency = 2.5f; 
        [SerializeField] private float velocityThreshold = 0.5f;
        private float timeSinceLastStep = 0f;
        private bool isAbilityPainting = false;
        private void Awake()
        {
            if(playerMotor == null)
            {
                playerMotor = GetComponent<PlayerMotor>();
            }
            if(animatorController == null)
            {
                animatorController = GetComponent<PlayerAnimatorController>();
            }
            if(weaponHandler == null)
            {
                weaponHandler = GetComponent<PlayerWeaponHandler>();
            }
            if (cameraController != null) 
            {
                cameraController = FindFirstObjectByType<CameraController>();
            }
            if(abilityManager == null)
            {
                abilityManager = GetComponent<AbilityManager>();
            }   
        }

        private void OnEnable()
        {
            if(inputActions == null)
            {
                inputActions = new PlayerInputActions();
            }
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        private void Update()
        {
            if (!GameManager.Instance.IsGameplayActive)
            {
                movementInput = Vector2.zero;
                return;
            }

            movementInput = inputActions.Gameplay.Move.ReadValue<Vector2>();
            animatorController?.UpdateMovementAnimation(movementInput.x, movementInput.y);

            // Get mouse screen position
            Vector2 mouseScreenPos = inputActions.Gameplay.MousePosition.ReadValue<Vector2>();

            // Raycast from camera through mouse position
            Ray mouseRay = Camera.main.ScreenPointToRay(mouseScreenPos);

            // Get weapon muzzle position
            Vector3 weaponPosition = Vector3.zero;
            if (weaponHandler?.CurrentWeapon != null)
            {
                // Get actual muzzle transform from weapon
                weaponPosition = weaponHandler.CurrentWeapon.transform.position;
            }
            else
            {
                // Fallback to player position + offset
                weaponPosition = transform.position + Vector3.up * 1f;
            }

            // Find where mouse ray intersects a plane at weapon height
            Plane aimPlane = new Plane(Vector3.up, weaponPosition);
            float rayDistance;

            if (aimPlane.Raycast(mouseRay, out rayDistance))
            {
                // Point where mouse ray hits the plane at weapon height
                Vector3 targetPoint = mouseRay.GetPoint(rayDistance);
                aimPoint.position = targetPoint;
            }
            else
            {
                // Fallback: use ground raycast
                Vector3 mousePosition = GetMouseWorldPosition(groundMask);
                aimPoint.position = new Vector3(mousePosition.x, weaponPosition.y, mousePosition.z);
            }

            // Rotate player to face aim point (only Y rotation)
            Vector3 lookDirection = aimPoint.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            TryFireWeapon();
            TryReloadCurrentWeapon();
            SwitchWeapons();
            ToggleCameraLookAhead();
            HandleFootsteps();
            HandleAbility();

            if (crosshairUI != null)
            {
                crosshairUI.position = mouseScreenPos;
            }
        }
        private void HandleAbility()
        {
            // Check if current ability supports painting interface
            var paintable = abilityManager?.GetCurrentAbility() as IPaintableAbility;

            if (paintable != null)
            {
                // PAINTABLE ABILITIES (AC-130, future drawing-based abilities)

                // Start painting on Q press
                if (inputActions.Gameplay.UseAbility.triggered && abilityManager.GetCurrentAbility().CanActivate())
                {
                    Vector3 mousePos = GetMouseWorldPosition(groundMask);
                    paintable.StartPainting(mousePos);
                    isAbilityPainting = true;
                }

                // Update painting while mouse held
                if (isAbilityPainting && inputActions.Gameplay.Shoot.ReadValue<float>() > 0.1f)
                {
                    Vector3 mousePos = GetMouseWorldPosition(groundMask);
                    paintable.UpdatePainting(mousePos);
                }

                // Finish painting when mouse released
                if (isAbilityPainting && inputActions.Gameplay.Shoot.WasReleasedThisFrame())
                {
                    Vector3 mousePos = GetMouseWorldPosition(groundMask);
                    paintable.FinishPainting(mousePos);
                    isAbilityPainting = false;
                }

                // Cancel painting with Escape
                if (isAbilityPainting && Input.GetKeyDown(KeyCode.Escape))
                {
                    paintable.CancelPainting();
                    isAbilityPainting = false;
                }
            }
            else
            {
                // STANDARD ABILITIES (Orbital Strike, Artillery, Heal Zone, etc.)
                // Instant activation on Q press

                if (inputActions.Gameplay.UseAbility.triggered)
                {
                    Vector3 targetPosition = GetMouseWorldPosition(groundMask);
                    abilityManager?.TryActivateCurrentAbility(targetPosition);
                }
            }
        }

        private void HandleFootsteps()
        {
            float currentSpeed = playerMotor.CurrentVelocity.magnitude;
            if (currentSpeed > velocityThreshold)
            {
                timeSinceLastStep += Time.deltaTime;

                float maxSpeed = 5f; // Get from PlayerMotor if exposed

                // FIX: Normalize speed to 0-1 range, then use as multiplier
                float speedRatio = currentSpeed / maxSpeed;
                float adjustedFrequency = stepFrequency * speedRatio;
                float stepInterval = 1.0f / adjustedFrequency;

                if (timeSinceLastStep >= stepInterval)
                {
                    AudioManager.Instance.PlayFootstep(footstepSound, 0.3f);
                    timeSinceLastStep = 0f;
                }
            }
            else
            {
                timeSinceLastStep = 0f;
            }
        }

        private void SwitchWeapons()
        {
            float scroll = inputActions.Gameplay.SwitchWeapon.ReadValue<float>();

            if (scroll > 0)
            {
                weaponHandler.SwitchToNextWeapon();
            }

            if (scroll < 0)
            {
                weaponHandler.SwitchToPreviousWeapon();
            }
        }

        private void FixedUpdate()
        {
            playerMotor.Move(movementInput);
        }

        public Vector3 GetMouseWorldPosition(LayerMask groundMask)
        {
            Ray ray = Camera.main.ScreenPointToRay(inputActions.Gameplay.MousePosition.ReadValue<Vector2>());

            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, groundMask))
            {
                return hitInfo.point;
            }

            return Vector3.zero;
        }

        private void TryFireWeapon()
        {
            bool shootButtonHeld = inputActions.Gameplay.Shoot.ReadValue<float>() > 0.1f;
            bool shootButtonPressed = inputActions.Gameplay.Shoot.triggered;

            if (weaponHandler.CurrentWeapon != null)
            {
                bool fired = weaponHandler.TryFireCurrentWeapon(shootButtonHeld, shootButtonPressed);
                if (fired)
                {
                    animatorController?.TriggerFire();
                }
            }
        }

        private void ToggleCameraLookAhead() 
        {
            cameraController.SetLookAhead(inputActions.Gameplay.ToggleLookAhead.IsPressed());
        }

        private void TryReloadCurrentWeapon() 
        {
            if(inputActions.Gameplay.Reload.triggered)
            weaponHandler.CurrentWeapon.StartReload();
        }

    }
}