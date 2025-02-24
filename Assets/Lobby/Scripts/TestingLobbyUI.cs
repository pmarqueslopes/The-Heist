using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TestingLobbyUI : MonoBehaviour
{
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button joinGameButton;
    
    
    private void Awake()
    {
        createGameButton.onClick.AddListener(() => 
        { 
            TheHeistGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
            Debug.Log("HOST");
        });
        joinGameButton.onClick.AddListener(() => 
        { 
            TheHeistGameMultiplayer.Instance.StartClient();
            Debug.Log("CLIENT");
        });
    }
}
