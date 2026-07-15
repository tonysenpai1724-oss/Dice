using UnityEngine;

public class ItemPreviewGenerator : MonoBehaviour
{
    public Camera previewCamera;
    public RenderTexture renderTexture;
    public Transform previewRoot;
    public Vector3 previewLocalPosition = Vector3.zero;
    public Vector3 previewLocalScale = Vector3.one;
    public Vector3 previewLocalEulerAngles = new(-30f, 48f, -30f);

    [Header("Crop")]
    public bool cropTransparentPixels = true;
    [Range(0f, 0.5f)] public float cropPaddingPercent = 0.08f;
    [Range(0f, 1f)] public float alphaThreshold = 0.02f;

    InventoryItem pooledPreviewItem;
    InventoryItem pooledPreviewPrefab;

    public Texture2D Capture(GameObject previewPrefab)
    {
        if (previewPrefab == null || previewCamera == null || renderTexture == null)
            return null;

        Transform root = previewRoot != null ? previewRoot : transform;
        GameObject previewContainer = new GameObject($"{previewPrefab.name} Preview");
        previewContainer.transform.SetParent(root, false);
        previewContainer.transform.localPosition = previewLocalPosition;
        //previewContainer.transform.localScale = previewLocalScale;
        previewContainer.transform.localRotation = Quaternion.Euler(previewLocalEulerAngles);

        GameObject previewObject = Instantiate(previewPrefab, previewContainer.transform);
        previewObject.SetActive(true);
        previewObject.transform.localPosition = Vector3.zero;
        //  previewObject.transform.localScale = previewLocalScale;
        // previewObject.transform.localRotation = Quaternion.identity;

        SetLayerRecursively(previewObject, GetPreviewLayer());
        DisablePreviewPhysics(previewObject);
        CenterPreviewObject(previewContainer.transform, root);

        Texture2D texture = Capture();
        previewContainer.SetActive(false);
        // Destroy(previewContainer);
        return texture;
    }

    public Texture2D Capture(Component previewPrefab)
    {
        return previewPrefab != null ? Capture(previewPrefab.gameObject) : null;
    }

    void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    int GetPreviewLayer()
    {
        int cameraLayer = previewCamera.gameObject.layer;
        if ((previewCamera.cullingMask & (1 << cameraLayer)) != 0)
            return cameraLayer;

        for (int layer = 0; layer < 32; layer++)
        {
            if ((previewCamera.cullingMask & (1 << layer)) != 0)
                return layer;
        }

        return cameraLayer;
    }

    void DisablePreviewPhysics(GameObject target)
    {
        if (target == null)
            return;

        foreach (Rigidbody rb in target.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void CenterPreviewObject(Transform previewContainer, Transform root)
    {
        if (previewContainer == null || !TryGetRendererBounds(previewContainer, out Bounds bounds))
            return;

        Vector3 desiredCenter = root != null ? root.TransformPoint(previewLocalPosition) : transform.TransformPoint(previewLocalPosition);
        previewContainer.position += desiredCenter - bounds.center;
    }

    bool TryGetRendererBounds(Transform target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bounds = default;

        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    public Texture2D Capture(InventoryItem itemPrefab, DiceData diceData)
    {
        if (itemPrefab == null || diceData == null || previewCamera == null || renderTexture == null)
            return null;

        InventoryItem item = GetOrCreatePreviewItem(itemPrefab);
        if (item == null)
            return null;

        PreparePreviewItem(item, diceData);
        return Capture();
    }

    public Texture2D Capture()
    {
        if (previewCamera == null || renderTexture == null)
            return null;

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture currentCameraRT = previewCamera.targetTexture;
        CameraClearFlags currentClearFlags = previewCamera.clearFlags;
        Color currentBackgroundColor = previewCamera.backgroundColor;

        previewCamera.targetTexture = renderTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(
            currentBackgroundColor.r,
            currentBackgroundColor.g,
            currentBackgroundColor.b,
            0f
        );
        RenderTexture.active = renderTexture;

        previewCamera.Render();

        Texture2D tex = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGBA32,
            false
        );

        tex.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );

        tex.Apply();
        RenderTexture.active = currentRT;
        previewCamera.targetTexture = currentCameraRT;
        previewCamera.clearFlags = currentClearFlags;
        previewCamera.backgroundColor = currentBackgroundColor;

        if (pooledPreviewItem != null)
            pooledPreviewItem.gameObject.SetActive(false);

        NormalizeCapturedAlpha(tex, previewCamera.backgroundColor);
        return cropTransparentPixels ? CropToVisiblePixels(tex) : tex;
    }

    void NormalizeCapturedAlpha(Texture2D texture, Color backgroundColor)
    {
        if (texture == null)
            return;

        Color32[] pixels = texture.GetPixels32();
        byte alphaThresholdByte = (byte)Mathf.RoundToInt(alphaThreshold * 255f);
        bool hasVisibleAlpha = false;

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > alphaThresholdByte)
            {
                hasVisibleAlpha = true;
                break;
            }
        }

        if (hasVisibleAlpha)
            return;

        Color32 background = backgroundColor;
        bool changed = false;
        const int colorThreshold = 4;

        for (int i = 0; i < pixels.Length; i++)
        {
            int diff = Mathf.Abs(pixels[i].r - background.r)
                + Mathf.Abs(pixels[i].g - background.g)
                + Mathf.Abs(pixels[i].b - background.b);

            pixels[i].a = diff > colorThreshold ? (byte)255 : (byte)0;
            changed = true;
        }

        if (!changed)
            return;

        texture.SetPixels32(pixels);
        texture.Apply();
    }

    InventoryItem GetOrCreatePreviewItem(InventoryItem itemPrefab)
    {
        if (pooledPreviewItem != null && pooledPreviewPrefab == itemPrefab)
            return pooledPreviewItem;

        ReleasePreviewItem();

        Transform root = previewRoot != null ? previewRoot : transform;
        pooledPreviewItem = Instantiate(itemPrefab, root);
        pooledPreviewPrefab = itemPrefab;
        return pooledPreviewItem;
    }

    void PreparePreviewItem(InventoryItem item, DiceData diceData)
    {
        if (item == null)
            return;

        item.gameObject.SetActive(true);
        item.transform.localPosition = previewLocalPosition;
        item.transform.localScale = previewLocalScale;
        item.transform.localRotation = Quaternion.Euler(previewLocalEulerAngles);
        item.Setup(diceData);
    }

    void ReleasePreviewItem()
    {
        if (pooledPreviewItem != null)
            Destroy(pooledPreviewItem.gameObject);

        pooledPreviewItem = null;
        pooledPreviewPrefab = null;
    }

    Texture2D CropToVisiblePixels(Texture2D source)
    {
        if (source == null)
            return null;

        Color32[] pixels = source.GetPixels32();
        int width = source.width;
        int height = source.height;
        byte threshold = (byte)Mathf.RoundToInt(alphaThreshold * 255f);

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                if (pixels[row + x].a <= threshold)
                    continue;

                if (x < minX)
                    minX = x;
                if (x > maxX)
                    maxX = x;
                if (y < minY)
                    minY = y;
                if (y > maxY)
                    maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
            return source;

        int contentWidth = maxX - minX + 1;
        int contentHeight = maxY - minY + 1;
        int padding = Mathf.RoundToInt(Mathf.Max(contentWidth, contentHeight) * cropPaddingPercent);
        int cropSize = Mathf.Max(contentWidth, contentHeight) + padding * 2;
        cropSize = Mathf.Min(cropSize, Mathf.Max(width, height));

        int centerX = Mathf.RoundToInt((minX + maxX) * 0.5f);
        int startX = centerX - cropSize / 2;
        int startY = minY - padding;

        Texture2D cropped = new Texture2D(cropSize, cropSize, TextureFormat.RGBA32, false);
        Color32[] clearPixels = new Color32[cropSize * cropSize];
        cropped.SetPixels32(clearPixels);

        for (int y = 0; y < cropSize; y++)
        {
            int sourceY = startY + y;
            if (sourceY < 0 || sourceY >= height)
                continue;

            for (int x = 0; x < cropSize; x++)
            {
                int sourceX = startX + x;
                if (sourceX < 0 || sourceX >= width)
                    continue;

                cropped.SetPixel(x, y, source.GetPixel(sourceX, sourceY));
            }
        }

        cropped.Apply();
        Destroy(source);
        return cropped;
    }

    void OnDestroy()
    {
        ReleasePreviewItem();
    }
}
