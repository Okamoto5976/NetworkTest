using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    private bool m_isHost = false;

    private Lobby m_joinedLobby;

    //[SerializeField] private RelayTest m_relayTest;

    public const string KEY_RELAY_CODE = "RelayCode";
    private float m_pollingTimer = 0f;

    private string m_playerName;

    private async void Start()
    {
        m_playerName = "PlayerName" + Random.Range(0, 1000).ToString();

        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(m_playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in:" + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateLobby()
    {
        //try
        //{
        //    string lobbyName = "TestLobby";
        //    int maxPlayers = 4;
        //    CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions()
        //    {
        //        IsPrivate = false,
        //        Player = new Player(AuthenticationService.Instance.PlayerId)
        //        {
        //            Data = new Dictionary<string, PlayerDataObject>()
        //            {
        //                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, m_playerName)}
        //            }
        //        },
        //        Data = new Dictionary<string, DataObject>()
        //        {
        //            {KEY_START_GAME,  }
        //        }
        //    }
        //}
    }
}
