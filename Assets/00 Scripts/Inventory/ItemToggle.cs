using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemToggle : MonoBehaviour
{
    public DiceData data;
    public Button btn;
    public void Start()
    {
        btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClick);
    }
    public void OnClick()
    {
        Debug.Log(data.diceName);
    }

}