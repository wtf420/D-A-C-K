using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManagerUI : MonoBehaviour
{
    public static LevelManagerUI Instance;

    [field: SerializeField] Screen WaitingForPlayersScreen;
    [field: SerializeField] Screen GameInProgressScreen;
    [field: SerializeField] Screen GameOverScreen;

    Screen currentScreen;

    protected virtual void Awake()
    {
        if (Instance)
            Destroy(this.gameObject);
        else
            Instance = this;
        currentScreen = null;
    }

    protected virtual void Update()
    {
        currentScreen?.UpdateScreen();
    }

    public void HideAllMenu()
    {
        WaitingForPlayersScreen.Hide();
        GameInProgressScreen.Hide();
        GameOverScreen.Hide();
    }

    public void ShowWaitingForPlayersScreen()
    {
        HideAllMenu();
        WaitingForPlayersScreen.Show();
        currentScreen = WaitingForPlayersScreen;
    }

    public void ShowGameInProgressScreen()
    {
        HideAllMenu();
        GameInProgressScreen.Show();
        currentScreen = GameInProgressScreen;
    }

    public void ShowGameOverScreen()
    {
        HideAllMenu();
        GameOverScreen.Show();
        currentScreen = GameOverScreen;
    }
}
