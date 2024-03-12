using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ButtonPrompt : MonoBehaviour
{
    private TextMeshProUGUI text;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public static ButtonPrompt Create()
    {
        ButtonPrompt buttonPrompt = Instantiate(Resources.Load<ButtonPrompt>("Prefabs/ButtonPrompt"));
        return buttonPrompt;
    }

    public void SetText(string str = "")
    {
        text.text = str;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
}
