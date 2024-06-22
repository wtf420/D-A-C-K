using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screen : MonoBehaviour
{
    public bool isShowing { get; protected set; }

    public virtual void Show() {
        gameObject.SetActive(true);
        isShowing = true;
        UpdateScreen();
    }
    public virtual void Hide() {
        gameObject.SetActive(false);
        isShowing = false;
    }
    public virtual void UpdateScreen() {}
}
