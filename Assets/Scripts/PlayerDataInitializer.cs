using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerDataInitializer : Screen
{
    [field: SerializeField] FlexibleColorPicker playerColorPicker;
    [field: SerializeField] TMP_InputField playerNameInput;
    [field: SerializeField] Button saveButton;

    [field: SerializeField] GameObject NetworkingUI;

    // Start is called before the first frame update
    void Awake()
    {
        saveButton.onClick.AddListener(OnSave);
    }

    private void OnSave()
    {
        PersistentPlayer.Instance.playerData.PlayerName = playerNameInput.text;
        PersistentPlayer.Instance.playerData.PlayerColor = playerColorPicker.color;
        gameObject.SetActive(false);
        if (NetworkingUI) NetworkingUI.SetActive(true);
        MainMenuUI.Instance?.NavigateToLobbyBrowser();
        if (SceneManager.GetActiveScene().name == "InitializeScene")
        SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }

    // Update is called once per frame
    void OnDestroy()
    {
        saveButton.onClick.RemoveAllListeners();
    }
}
