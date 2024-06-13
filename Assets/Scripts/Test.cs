using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    [field: SerializeField] Button startButton;
    [field: SerializeField] Button joinButton;

    // Start is called before the first frame update
    void Start()
    {
        startButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();

        });
        joinButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
    }

    void Update()
    {
        if (NetworkManager.Singleton.IsHost && Input.GetKeyDown(KeyCode.Space))
        {
            NetworkManager.Singleton.SceneManager.LoadScene("TestingScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    void OnDestroy()
    {
        startButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
    }
}
