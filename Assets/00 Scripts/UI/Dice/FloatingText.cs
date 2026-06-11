using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float duration = 1f;
    public float blinkDuration = 0.25f;
    public float blinkSpeed = 18f;
    public float colorTransitionDuration = 0.25f;

    public TextMeshProUGUI text;
    private Color targetColor = Color.white;
    private float timer;
    private RectTransform textRectTransform;
    private RectTransform canvasRectTransform;
    private Canvas canvas;
    private Camera worldCamera;
    private Vector3 worldPosition;
    private bool useWorldPosition;
    private bool isBlinking = true;

    void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>(true);

        if (text != null)
        {
            textRectTransform = text.rectTransform;
            canvas = text.GetComponentInParent<Canvas>();

            if (canvas != null)
                canvasRectTransform = canvas.transform as RectTransform;

            targetColor = text.color;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (isBlinking && timer >= blinkDuration)
        {
            isBlinking = false;
            timer = 0f;
        }

        if (useWorldPosition)
        {
            worldPosition += Vector3.up * moveSpeed * Time.deltaTime;
            UpdateScreenPosition();
        }
        else
        {
            if (textRectTransform != null)
                textRectTransform.position += Vector3.up * moveSpeed * Time.deltaTime;
            else
                transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }

        if (text != null)
        {
            Color color;

            if (isBlinking)
            {
                float blinkPercent = Mathf.PingPong(timer * blinkSpeed, 1f);
                color = Color.Lerp(Color.white, targetColor, blinkPercent);
            }
            else
            {
                float colorPercent = colorTransitionDuration > 0f
                    ? Mathf.Clamp01(timer / colorTransitionDuration)
                    : 1f;

                color = Color.Lerp(Color.white, targetColor, colorPercent);
                color.a = Mathf.Lerp(1, 0, timer / duration);
            }

            text.color = color;
        }

        if (!isBlinking && timer >= duration)
            Destroy(gameObject);
    }

    public void SetText(string value, Color color)
    {
        if (text == null)
            return;

        text.text = value;
        targetColor = color;
        timer = 0f;
        isBlinking = true;
        text.color = Color.white;
    }

    public void SetWorldPosition(Vector3 position, Camera camera = null)
    {
        worldPosition = position;
        worldCamera = camera != null ? camera : Camera.main;
        useWorldPosition = true;
        UpdateScreenPosition();
    }

    void UpdateScreenPosition()
    {
        if (worldCamera == null)
            return;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);

        if (textRectTransform == null)
        {
            transform.position = screenPosition;
            return;
        }

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            textRectTransform.position = screenPosition;
            return;
        }

        if (canvasRectTransform != null && canvas != null)
        {
            Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera
                ? canvas.worldCamera
                : worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    screenPosition,
                    canvasCamera,
                    out Vector2 localPosition))
            {
                textRectTransform.anchoredPosition = localPosition;
                return;
            }
        }

        textRectTransform.position = screenPosition;
    }
}
