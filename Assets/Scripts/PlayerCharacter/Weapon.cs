using ProjectAdminPrivileges.PlayerCharacter;
using ProjectAdminPrivileges.Audio;
using System.Collections;
using UnityEngine;

namespace ProjectAdminPrivileges.Combat.Weapons
{
    public class Weapon : MonoBehaviour
    {
        public enum WeaponType { Pistol, Rifle, Shotgun, SMG }

        [SerializeField] private WeaponData weaponData;
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private ParticleSystem muzzleParticleFX;
        [SerializeField] private ProjectilePoolManager projectilePoolManager;

        [Header("IK Grips")]
        [SerializeField] private Transform leftHandGrip;
        [SerializeField] private Transform rightHandGrip;
        [SerializeField] private Transform leftHandHint;
        [SerializeField] private Transform rightHandHint;

        [Header("Aiming")]
        [SerializeField] private Transform aimPoint;

        private float timeSinceLastShot = 0f;
        private int currentAmmoInMag;
        private bool isReloading = false;
        private Coroutine reloadCoroutine;
        private PlayerAnimatorController animController;

        public float TimeSinceLastShot => timeSinceLastShot;
        public int CurrentAmmoInMag => currentAmmoInMag;
        public WeaponData GetWeaponData() => weaponData;

        private Camera camera;
        private CameraController cameraController;

        private void Awake()
        {
            animController = GetComponentInParent<PlayerAnimatorController>();
            camera = Camera.main.GetComponent<Camera>();
            cameraController = camera.gameObject.GetComponentInParent<CameraController>();
            projectilePoolManager = FindFirstObjectByType<ProjectilePoolManager>();
        }

        private void OnEnable()
        {
            isReloading = false;
        }

        private void Start()
        {
            currentAmmoInMag = weaponData.magazineSize;
            projectilePoolManager.Prewarm(100);
        }

        public bool TryFireWeapon(bool buttonHeld, bool buttonTriggered)
        {
            if (currentAmmoInMag <= 0) return false;
            if (Time.time < timeSinceLastShot + (1f / weaponData.fireRate)) return false;
            if (!weaponData.isAutomatic && !buttonTriggered) return false;
            if (weaponData.isAutomatic && !buttonHeld) return false;

            Vector3 aimDirection = CalculateAimDirection();
            int pelletsToFire = (weaponData.weaponType == WeaponType.Shotgun) ? weaponData.pelletsPerShot : 1;

            for (int i = 0; i < pelletsToFire; i++)
            {
                Vector3 direction = ApplySpread(aimDirection, weaponData.spreadAngle);
                Projectile projectile = projectilePoolManager.Get();

                projectile.transform.position = muzzleTransform.position;
                projectile.transform.rotation = Quaternion.LookRotation(direction);

                // Clear trail AFTER positioning to remove pool→muzzle gap
                projectile.ClearTrail();

                ProjectileTrail trail = projectile.GetComponent<ProjectileTrail>();
                if (trail != null && weaponData.tracerGradient != null)
                {
                    trail.SetGradient(weaponData.tracerGradient);
                    trail.SetWidth(weaponData.tracerStartWidth, weaponData.tracerEndWidth);
                }

                projectile.Fire(direction, weaponData.damage);
            }

            currentAmmoInMag--;
            muzzleParticleFX.Play();
            timeSinceLastShot = Time.time;

            if (camera != null)
            {
                float shakeAmount = weaponData.weaponType == WeaponType.Shotgun ? 0.2f : 0.1f;
                cameraController.Shake(shakeAmount);
            }

            if (weaponData.audioData != null && weaponData.audioData.FireSound != null)
            {
                AudioManager.Instance.PlayAudio(weaponData.audioData.FireSound, muzzleTransform.position);
            }

            return true;
        }

        private Vector3 CalculateAimDirection()
        {
            if (aimPoint == null) return muzzleTransform.forward;
            return (aimPoint.position - muzzleTransform.position).normalized;
        }

        private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            if (spreadAngle == 0f) return direction;
            Quaternion spreadRotation = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0f
            );
            return spreadRotation * direction;
        }

        public void StartReload()
        {
            if (isReloading || currentAmmoInMag >= weaponData.magazineSize) return;
            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);

            reloadCoroutine = (weaponData.reloadType == ReloadType.Magazine)
                ? StartCoroutine(ReloadMagazine())
                : StartCoroutine(ReloadIndividual());
        }

        private IEnumerator ReloadMagazine()
        {
            isReloading = true;
            animController?.TriggerReload();
            yield return new WaitForSeconds(weaponData.reloadTime);
            currentAmmoInMag = weaponData.magazineSize;
            isReloading = false;
            reloadCoroutine = null;
        }

        private IEnumerator ReloadIndividual()
        {
            isReloading = true;
            animController?.TriggerReload(); // FIXED: Use field, not GetComponent again

            while (currentAmmoInMag < weaponData.magazineSize)
            {
                yield return new WaitForSeconds(weaponData.reloadTimePerRound);
                currentAmmoInMag++;
            }

            isReloading = false;
            reloadCoroutine = null;
        }

        public void SetAimPoint(Transform newAimPoint) => aimPoint = newAimPoint;
        public Transform GetLeftHandGrip() => leftHandGrip;
        public Transform GetRightHandGrip() => rightHandGrip;
        public Transform GetLeftHandHint() => leftHandHint;
        public Transform GetRightHandHint() => rightHandHint;

        private void OnDrawGizmos()
        {
            if (leftHandGrip != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(leftHandGrip.position, 0.05f);
                Gizmos.DrawLine(leftHandGrip.position, leftHandGrip.position + leftHandGrip.forward * 0.1f);
            }
            if (rightHandGrip != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(rightHandGrip.position, 0.05f);
            }

            if (muzzleTransform != null && aimPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(muzzleTransform.position, aimPoint.position);

                Vector3 aimDir = CalculateAimDirection();
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(muzzleTransform.position, aimDir * 10f);
            }
        }

    }
}