using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuScreen : Screen
{
    [SerializeField] Button resumeGameButton, exitGameButton;
    CursorLockMode previousMode;

    // Start is called before the first frame update
    void Start()
    {
        resumeGameButton.onClick.AddListener(ToggleShowHide);
        exitGameButton.onClick.AddListener(() => LobbyManager.Instance.ExitGame());
    }

    void OnDestroy()
    {
        resumeGameButton.onClick.RemoveAllListeners();
        exitGameButton.onClick.RemoveAllListeners();
    }

    void ToggleShowHide()
    {
        if (isShowing)
            Hide();
        else
            Show();
    }

    public override void Show()
    {
        base.Show();
        previousMode = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void Hide()
    {
        base.Hide();
        Cursor.lockState = previousMode;
        Cursor.visible = previousMode != CursorLockMode.Locked;
    }
}
