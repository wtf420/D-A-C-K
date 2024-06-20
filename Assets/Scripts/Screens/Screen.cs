using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screen : MonoBehaviour
{
    public virtual void Show() {
        gameObject.SetActive(true);
        UpdateScreen();
    }
    public virtual void Hide() {
        gameObject.SetActive(false);
    }
    public virtual void UpdateScreen() {}
}
