using Sirenix.OdinInspector;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
[ExecuteAlways]
public class OrthoPlaneViewportFitter : MonoBehaviour
{
    public enum FitMode
    {
        WidthOnly,
        HeightOnly,
        UniformMin,
        UniformMax
    }

    [SerializeField] Camera targetCamera;
    [SerializeField] Vector2 referenceResolution = new Vector2(1080f, 1920f);
    [SerializeField] Vector3 referenceScale = new Vector3(3.5f, 3f, 3f);
    [SerializeField] FitMode fitMode = FitMode.WidthOnly;
    [SerializeField] bool fitOnEnable = true;
    [SerializeField] bool updateContinuously;

    Vector2Int lastScreenSize;
    float lastOrthoSize;
    float lastAspect;

    void Awake()
    {
        ApplyReferenceFit();
    }

    void OnEnable()
    {
        if (fitOnEnable)
            ApplyReferenceFit();
    }

    void Update()
    {
        if (!updateContinuously)
            return;

        if (NeedsRefit())
            ApplyReferenceFit();
    }

    [Button("Capture Current Scale As Reference")]
    public void CaptureCurrentScaleAsReference()
    {
        referenceScale = transform.localScale;
        referenceResolution = new Vector2(Screen.width, Screen.height);
    }

    [Button("Apply Reference Fit")]
    public void ApplyReferenceFit()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        float referenceAspect = referenceResolution.y <= 0f
            ? cam.aspect
            : referenceResolution.x / referenceResolution.y;

        float currentAspect = cam.aspect;
        if (referenceAspect <= 0f || currentAspect <= 0f)
            return;

        float widthRatio = currentAspect / referenceAspect;
        float heightRatio = referenceAspect / currentAspect;

        Vector3 scale = referenceScale;
        switch (fitMode)
        {
            case FitMode.WidthOnly:
                scale.x = referenceScale.x * widthRatio;
                scale.z = referenceScale.z;
                break;
            case FitMode.HeightOnly:
                scale.x = referenceScale.x;
                scale.z = referenceScale.z * heightRatio;
                break;
            case FitMode.UniformMin:
                float minRatio = Mathf.Min(widthRatio, heightRatio);
                scale.x = referenceScale.x * minRatio;
                scale.z = referenceScale.z * minRatio;
                break;
            case FitMode.UniformMax:
                float maxRatio = Mathf.Max(widthRatio, heightRatio);
                scale.x = referenceScale.x * maxRatio;
                scale.z = referenceScale.z * maxRatio;
                break;
        }

        scale.y = referenceScale.y;
        transform.localScale = scale;

        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrthoSize = cam.orthographicSize;
        lastAspect = cam.aspect;
    }

    bool NeedsRefit()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
            return false;

        return lastScreenSize.x != Screen.width ||
               lastScreenSize.y != Screen.height ||
               !Mathf.Approximately(lastOrthoSize, cam.orthographicSize) ||
               !Mathf.Approximately(lastAspect, cam.aspect);
    }
}


