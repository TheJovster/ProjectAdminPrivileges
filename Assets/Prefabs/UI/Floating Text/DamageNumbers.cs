using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro damageText;
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float fadeStartTime = 0.8f;

    private Camera mainCamera;
    private float timer;
    private bool isActive;
    private Color originalColor;

    private System.Action<DamageNumber> returnToPool;

    private void Awake()
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TextMeshPro>();
        }

        if (damageText != null)
        {
            originalColor = damageText.color;
        }
        else
        {
            Debug.LogError("[DamageNumber] No TextMeshPro found!");
        }
    }

    public void Initialize(int damage, Vector3 worldPos, Camera camera, System.Action<DamageNumber> returnCallback)
    {
        if (damageText == null) return;

        damageText.text = damage.ToString();

        // Spawn slightly above hit point
        transform.position = worldPos + Vector3.up * 0.5f;

        // Add random offset for visual variety
        transform.position += new Vector3(
            Random.Range(-0.2f, 0.2f),
            0,
            Random.Range(-0.2f, 0.2f)
        );

        mainCamera = camera;
        returnToPool = returnCallback;

        timer = 0f;
        isActive = true;
        damageText.color = originalColor;
    }

    private void Update()
    {
        if (!isActive || mainCamera == null) return;

        timer += Time.deltaTime;

        // Float upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Billboard - always face camera
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                        mainCamera.transform.rotation * Vector3.up);

        // Fade out near end of lifetime
        if (timer >= fadeStartTime)
        {
            float fadeProgress = (timer - fadeStartTime) / (lifetime - fadeStartTime);
            Color color = damageText.color;
            color.a = Mathf.Lerp(1f, 0f, fadeProgress);
            damageText.color = color;
        }

        // Return to pool when done
        if (timer >= lifetime)
        {
            isActive = false;
            returnToPool?.Invoke(this);
        }
    }
}