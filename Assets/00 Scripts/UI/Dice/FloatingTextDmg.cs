using TMPro;
using UnityEngine;

public class FloatingTextDmg : MonoBehaviour
{
    [SerializeField] float moveSpeed = 120f;
    [SerializeField] float duration = 0.8f;
    [SerializeField] Color defaultColor = new Color(1f, 0.12f, 0.05f, 1f);
    [SerializeField] TextMeshProUGUI text;

    float timer;
    RectTransform textRectTransform;
    RectTransform canvasRectTransform;
    Canvas canvas;
    Color startColor;
    Vector2 screenPosition;

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
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        screenPosition += Vector2.up * moveSpeed * Time.deltaTime;
        ApplyScreenPosition(screenPosition);

        if (text != null)
        {
            Color color = startColor;
            color.a = Mathf.Lerp(1f, 0f, duration > 0f ? timer / duration : 1f);
            text.color = color;
        }

        if (timer >= duration)
            Destroy(gameObject);
    }

    public void SetText(string value, Color color)
    {
        if (text == null)
            return;

        text.text = value;
        startColor = color;
        text.color = color;
        timer = 0f;
    }

    public void SetScreenPosition(Vector2 position)
    {
        screenPosition = position;
        ApplyScreenPosition(screenPosition);
    }

    public void SetText(string value)
    {
        SetText(value, defaultColor);
    }

    void ApplyScreenPosition(Vector2 position)
    {
        if (textRectTransform == null)
        {
            transform.position = position;
            return;
        }

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            textRectTransform.position = position;
            return;
        }

        if (canvasRectTransform != null && canvas != null)
        {
            Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    position,
                    canvasCamera,
                    out Vector2 localPosition))
            {
                textRectTransform.anchoredPosition = localPosition;
                return;
            }
        }

        textRectTransform.position = position;
    }
}