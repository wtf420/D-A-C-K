using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Modal : MonoBehaviour
{
    [SerializeField] TMP_Text messageText;
    [SerializeField] Button okButton;

    public static Modal CreateNewModalWindow(string message, Action action, Transform parent = null)
    {
        Modal modal = Instantiate(Resources.Load<Modal>("Prefabs/stuff/Modal"), parent);
        modal.messageText.text = message;
        modal.okButton.onClick.AddListener(() => {
            action.Invoke();
            Destroy(modal.gameObject);
        });
        return modal;
    }
}
