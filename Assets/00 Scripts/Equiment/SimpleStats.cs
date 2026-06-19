using System.Collections.Generic;

[System.Serializable]
public class SimpleStats
{
    public float Value { get; private set; }

    readonly Dictionary<string, Dictionary<string, float>> dicValue =
        new Dictionary<string, Dictionary<string, float>>();

    // Apply stats (ghi đè giá trị)
    public void ApplyStats(float value, string keyGlobal, string keyLocal)
    {
        if (!dicValue.ContainsKey(keyGlobal))
            dicValue[keyGlobal] = new Dictionary<string, float>();

        if (!dicValue[keyGlobal].ContainsKey(keyLocal))
            dicValue[keyGlobal][keyLocal] = 0f;

        dicValue[keyGlobal][keyLocal] = value;
        CalculateValue();
    }

    // Cộng thêm vào giá trị hiện tại
    public void AddValue(float value, string keyGlobal, string keyLocal)
    {
        if (!dicValue.ContainsKey(keyGlobal))
            dicValue[keyGlobal] = new Dictionary<string, float>();

        if (!dicValue[keyGlobal].ContainsKey(keyLocal))
            dicValue[keyGlobal][keyLocal] = 0f;

        dicValue[keyGlobal][keyLocal] += value;
        CalculateValue();
    }

    // Lấy giá trị theo key
    public float GetValue(string keyGlobal, string keyLocal)
    {
        if (dicValue.ContainsKey(keyGlobal) && dicValue[keyGlobal].ContainsKey(keyLocal))
            return dicValue[keyGlobal][keyLocal];

        return 0f;
    }

    public void ClearStats(string keyGlobal)
    {
        if (dicValue.ContainsKey(keyGlobal))
            dicValue[keyGlobal] = new Dictionary<string, float>();

        CalculateValue();
    }

    public void ClearStats(string keyGlobal, string keyLocal)
    {
        if (dicValue.ContainsKey(keyGlobal) && dicValue[keyGlobal].ContainsKey(keyLocal))
            dicValue[keyGlobal][keyLocal] = 0f;

        CalculateValue();
    }

    void CalculateValue()
    {
        Value = 0f;
        foreach (var item in dicValue)
        {
            foreach (var child in item.Value)
            {
                Value += child.Value;
            }
        }
    }
}

