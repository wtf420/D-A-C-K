using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerDataInitializer : MonoBehaviour
{
    [field: SerializeField] FlexibleColorPicker playerColorPicker;
    [field: SerializeField] TMP_InputField playerNameInput;
    [field: SerializeField] Button saveButton;

    [field: SerializeField] GameObject NetworkingUI;

    // Start is called before the first frame update
    void Start()
    {
        saveButton.onClick.AddListener(OnSave);
    }

    private void OnSave()
    {
        PersistentPlayer.Instance.playerData.PlayerName = playerNameInput.text;
        PersistentPlayer.Instance.playerData.PlayerColor = playerColorPicker.color;
        gameObject.SetActive(false);
        NetworkingUI?.SetActive(true);
        MainMenuUI.Instance?.NavigateToLobbyBrowser();
    }

    // Update is called once per frame
    void OnDestroy()
    {
        saveButton.onClick.RemoveAllListeners();
    }
}
