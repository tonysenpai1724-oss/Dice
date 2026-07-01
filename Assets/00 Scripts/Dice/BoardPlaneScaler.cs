using UnityEngine;

[ExecuteAlways]
public class BoardPlaneScaler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera targetCamera;
    [SerializeField] Collider boardCollider;
    [SerializeField] DiceManager diceManager;

    [Header("Reference Aspect")]
    [SerializeField] float referenceAspect = 16f / 9f;
    [SerializeField] bool scaleWidthOnly = true;

    Vector3 initialScale;
    bool hasInitialScale;

    void Awake()
    {
        CacheInitialScale();
        ApplyScale();
    }

    void OnEnable()
    {
        CacheInitialScale();
        ApplyScale();
    }

    void OnValidate()
    {
        referenceAspect = Mathf.Max(0.01f, referenceAspect);
        CacheInitialScale();
        ApplyScale();
    }

    void Update()
    {
        if (!Application.isPlaying)
            ApplyScale();
    }

    [ContextMenu("Apply Board Scale")]
    public void ApplyScale()
    {
        CacheInitialScale();

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
            return;

        float currentAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float aspectRatio = currentAspect / referenceAspect;

        Vector3 nextScale = initialScale;
        if (scaleWidthOnly)
            nextScale.x = 3f * initialScale.x * aspectRatio;
        else
            nextScale = new Vector3(3 * initialScale.x * aspectRatio, 4.5f * initialScale.y * aspectRatio, 4.5f * initialScale.z * aspectRatio);

        transform.localScale = nextScale;

        if (diceManager != null && boardCollider != null)
            diceManager.SetBoardCollider(boardCollider);
    }

    void CacheInitialScale()
    {
        if (hasInitialScale)
            return;

        initialScale = transform.localScale;
        hasInitialScale = true;
    }
}
