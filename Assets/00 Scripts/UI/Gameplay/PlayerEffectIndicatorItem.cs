using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEffectIndicatorItem : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI valueText;
    public float punchScale = 1.35f;
    public float punchDuration = 0.16f;

    int currentValue;
    Coroutine punchRoutine;
    Vector3 defaultTextScale = Vector3.one;

    void Awake()
    {
        CacheDefaultTextScale();
    }

    public void Setup(Sprite sprite, int value)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        SetValue(value, false);
    }

    public void SetValue(int value)
    {
        SetValue(value, true);
    }

    public void SetValue(int value, bool animate)
    {
        bool changed = currentValue != value;
        currentValue = value;

        if (valueText != null)
            valueText.text = value.ToString();

        if (animate && changed)
            PlayPunch();
    }

    void CacheDefaultTextScale()
    {
        if (valueText != null)
            defaultTextScale = valueText.transform.localScale;
    }

    void PlayPunch()
    {
        if (valueText == null || !gameObject.activeInHierarchy)
            return;

        if (punchRoutine != null)
            StopCoroutine(punchRoutine);

        punchRoutine = StartCoroutine(PunchTextScale());
    }

    IEnumerator PunchTextScale()
    {
        Transform textTransform = valueText.transform;
        float halfDuration = Mathf.Max(0.01f, punchDuration * 0.5f);
        Vector3 targetScale = defaultTextScale * punchScale;

        yield return ScaleText(textTransform, defaultTextScale, targetScale, halfDuration);
        yield return ScaleText(textTransform, targetScale, defaultTextScale, halfDuration);

        textTransform.localScale = defaultTextScale;
        punchRoutine = null;
    }

    IEnumerator ScaleText(Transform textTransform, Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 2f);
            textTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
            yield return null;
        }
    }
}