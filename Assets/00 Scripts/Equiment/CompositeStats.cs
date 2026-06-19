using System.Collections.Generic;

[System.Serializable]
public class CompositeStats
{
    public float Value { get; private set; }

    readonly Dictionary<string, Dictionary<string, float>> dicValueCount =
        new Dictionary<string, Dictionary<string, float>>();

    readonly Dictionary<string, Dictionary<string, float>> dicValuePercent =
        new Dictionary<string, Dictionary<string, float>>();

    public void ApplyStats(float value, string keyGlobal, string keyLocal, bool isFlatValue)
    {
        if (isFlatValue)
            SetValueCount(value, keyGlobal, keyLocal);
        else
            SetValuePercent(value, keyGlobal, keyLocal);
    }

    public void ClearStats(string keyGlobal)
    {
        if (dicValueCount.ContainsKey(keyGlobal))
            dicValueCount[keyGlobal] = new Dictionary<string, float>();

        if (dicValuePercent.ContainsKey(keyGlobal))
            dicValuePercent[keyGlobal] = new Dictionary<string, float>();

        CalculateValue();
    }

    public void ClearStats(string keyGlobal, string keyLocal)
    {
        if (dicValueCount.ContainsKey(keyGlobal) && dicValueCount[keyGlobal].ContainsKey(keyLocal))
            dicValueCount[keyGlobal][keyLocal] = 0;

        if (dicValuePercent.ContainsKey(keyGlobal) && dicValuePercent[keyGlobal].ContainsKey(keyLocal))
            dicValuePercent[keyGlobal][keyLocal] = 0;

        CalculateValue();
    }

    public void SetValueCount(float value, string keyGlobal, string keyLocal)
    {
        if (!dicValueCount.ContainsKey(keyGlobal))
            dicValueCount.Add(keyGlobal, new Dictionary<string, float>());

        if (!dicValueCount[keyGlobal].ContainsKey(keyLocal))
            dicValueCount[keyGlobal].Add(keyLocal, 0);

        dicValueCount[keyGlobal][keyLocal] = value;
        CalculateValue();
    }

    public void SetValuePercent(float value, string keyGlobal, string keyLocal)
    {
        if (!dicValuePercent.ContainsKey(keyGlobal))
            dicValuePercent.Add(keyGlobal, new Dictionary<string, float>());

        if (!dicValuePercent[keyGlobal].ContainsKey(keyLocal))
            dicValuePercent[keyGlobal].Add(keyLocal, 0);

        dicValuePercent[keyGlobal][keyLocal] = value;
        CalculateValue();
    }

    void CalculateValue()
    {
        float valueCount = 0f;
        foreach (var item in dicValueCount)
        {
            foreach (var child in item.Value)
            {
                valueCount += child.Value;
            }
        }

        float valuePercent = 1f;
        foreach (var item in dicValuePercent)
        {
            float layerPercent = 100f;
            foreach (var child in item.Value)
            {
                layerPercent += child.Value;
            }

            valuePercent *= layerPercent / 100f;
        }

        Value = valueCount * valuePercent;
    }
}

