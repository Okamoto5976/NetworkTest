using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyManager : MonoBehaviour
{
    private bool m_isHost = false;

    private Lobby m_joinedLobby;

    //[SerializeField] private RelayTest m_relayTest;
    [SerializeField] private LobbyListView m_lobbyListView;

    public const string KEY_RELAY_CODE = "RelayCode";
    private float m_heartbeatTimer;
    private float m_heartbeatInterval = 15f;

    private string m_playerName;

    private async void Start()
    {
        LobbyBanner.OnJoinLobby += (sender, lobby) =>
        {
            Debug.Log("JoinLobby LobbyID=" + lobby.Id.ToString());
            JoinLobby(lobby);
        };

        LobbyListView.OnRefreshLobbyListRequest += (sender, e) =>
        {
            RefreshLobbies();
        };


        m_playerName = "PlayerName" + UnityEngine.Random.Range(0, 1000).ToString();

        //Profileの設定
        //マルチプレイ開発で同じプロジェクト内でも別々の認証環境としてプレイできるもの
        //５つ分用意して使えるようにしたい（別PCでは同じProfileでも問題ない）
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(m_playerName);

        //Profileをもとに初期化
        await UnityServices.InitializeAsync(initializationOptions);

        //SignedInはログインが成功時に発生する通知
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in:" + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update()
    {
        RefreshLobbyHeatbeat();

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (m_joinedLobby == null)
            {
                CreateLobby();
            }
        }
    }

    private async void RefreshLobbies()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies Count:" + queryResponse.Results.Count);
            foreach(var lobby in queryResponse.Results)
            {
                Debug.Log($"Lobby:name {lobby.Name} id {lobby.Id} code {lobby.LobbyCode} maxPlayers {lobby.MaxPlayers}");
            }
            m_lobbyListView.Refresh(queryResponse.Results);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log("Exception:" + e.Message);
        }
    }

    private async void JoinLobby(Lobby lobby)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = new Player(AuthenticationService.Instance.PlayerId)
                {
                    Data = new Dictionary<string, PlayerDataObject>()
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, m_playerName)}
                    }
                },
            };
            m_joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
            Debug.Log("Lobby joined:" + m_joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Exception:" + e.Message);
        }
    }

    private async void RefreshLobbyHeatbeat()
    {
        if(m_joinedLobby != null)
        {
            m_heartbeatTimer += Time.deltaTime;
            if(m_heartbeatTimer > m_heartbeatInterval)
            {
                m_heartbeatTimer = 0.0f;
                await LobbyService.Instance.SendHeartbeatPingAsync(m_joinedLobby.Id);
            }
        }
    }

    //optionsについて
    //そのロビーがPrivateかPublicか　IsPrivate
    //そのロビーに入るPlayerDataの情報
    //VisibilityOptions　public, member, private
    //それぞれ参加せずに読み取れる　参加した人物のみ読み取れる　Dataを所持するもののみ読みとれる
    //
    public async void CreateLobby()
    {
        try
        {
            string lobbyName = "TestLobby";
            int maxPlayers = 4;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = false,
                //PlayerIdとはAuthenticationで取得したホストのPlayerID
                Player = new Player(AuthenticationService.Instance.PlayerId)
                {
                    Data = new Dictionary<string, PlayerDataObject>()
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, m_playerName)}
                    }
                }
                //,
                //Data = new Dictionary<string, DataObject>()
                //{
                //    {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0")}
                //}
            };
            m_isHost = true;
            m_joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            //lobby.id lobby.name lobby.code lobbymaxPlayers lobby.Players(参加してるプレイヤー一覧） lobby.IsPrivate
            Debug.Log("Lobby created: " + m_joinedLobby.Id);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log("Error creating lobby: " + e.Message);
        }
    }
}
