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

    public Texture2D Capture(InventoryItem itemPrefab, DiceData diceData)
    {
        if (itemPrefab == null || diceData == null || previewCamera == null || renderTexture == null)
            return null;

        Transform root = previewRoot != null ? previewRoot : transform;
        InventoryItem item = Instantiate(itemPrefab, root);
        item.transform.localPosition = previewLocalPosition;
        item.transform.localScale = previewLocalScale;
        item.transform.localRotation = Quaternion.Euler(previewLocalEulerAngles);
        item.Setup(diceData);
        // foreach (Transform t in item.GetComponentsInChildren<Transform>(true))
        // {
        //     Debug.Log($"{t.name} | Layer = {LayerMask.LayerToName(t.gameObject.layer)}");
        // }

        Texture2D texture = Capture();

        item.gameObject.SetActive(false);
        Destroy(item.gameObject);

        return texture;
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

        return cropTransparentPixels ? CropToVisiblePixels(tex) : tex;
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
        int centerY = Mathf.RoundToInt((minY + maxY) * 0.5f);
        int startX = centerX - cropSize / 2;
        int startY = centerY - cropSize / 2;

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
}