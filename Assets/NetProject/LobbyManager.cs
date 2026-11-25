using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("RoomSelect")]
    [SerializeField] private Button m_hostButton;
    [SerializeField] private Button m_publicButton;
    [SerializeField] private Button m_privateButton;

    [Header("CreateRoom")]
    [SerializeField] private GameObject m_createRoomCanvas;

    [SerializeField] private Button m_maxPlayer2Button;
    [SerializeField] private Button m_maxPlayer3Button;
    [SerializeField] private Button m_maxPlayer4Button;

    [SerializeField] private Button m_roomPublicButton;
    [SerializeField] private Button m_roomPrivateButton;
    [SerializeField] private TMP_InputField m_lobbyInputField;
    [SerializeField] private Button m_createRoomButton;

    [SerializeField] private Button m_createRoomBackButton;
 
    [Header("SearchPublicRoom")]
    [SerializeField] private GameObject m_searchPublicRoomCanvas;
    [SerializeField] private Button m_searchPublicBackButton;

    [Header("LobbyDisplay")]
    [SerializeField] private GameObject m_lobbyCanvas;
    [SerializeField] private GameObject m_playerIconPrefab;
    [SerializeField] private Transform m_PlayerListContent;

    [SerializeField] private Button m_readyButton;

    private int m_maxPlayers;
    private bool m_hasPrivate;

    private bool m_isHost = false;

    private Lobby m_joinedLobby;

    //[SerializeField] private RelayTest m_relayTest;
    [SerializeField] private LobbyListView m_lobbyListView;

    public const string KEY_RELAY_CODE = "RelayCode";
    private float m_heartbeatTimer;
    private float m_heartbeatInterval = 15f;
    private float m_lobbyUpdateTimer;

    private string m_playerName;

    //LobbyDisplay Related
    private bool m_isUpdatingLobby = false;

    private bool m_isCheckingReady = false;

    private bool m_isGameStarting = false;

    private bool m_isConnecting = false;
    private bool m_isSceneLoading;

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback
            += OnClientConnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback
                -= OnClientConnected;
        }
    }
    private void Awake()
    {
        m_hostButton.onClick.AddListener(() => ViewCreateRoomCanvas(true));
        m_publicButton.onClick.AddListener(() => ViewSearchPublicRoomCanvas(true));
        //createRoom
        m_maxPlayer2Button.onClick.AddListener(() => MaxPlayer(2));
        m_maxPlayer3Button.onClick.AddListener(() => MaxPlayer(3));
        m_maxPlayer4Button.onClick.AddListener(() => MaxPlayer(4));
        m_roomPrivateButton.onClick.AddListener(() => SetHasPlayer(true));
        m_roomPublicButton.onClick.AddListener(() => SetHasPlayer(false));
        m_createRoomButton.onClick.AddListener(() => CreateLobby());
        m_createRoomBackButton.onClick.AddListener(() => ViewCreateRoomCanvas(false));
        //searchPublicRoom
        m_searchPublicBackButton.onClick.AddListener(() => ViewSearchPublicRoomCanvas(false));

        m_readyButton.onClick.AddListener(() => SetReady(true));
    }

    private async void Start()
    {
        LobbyBanner.OnJoinLobby += (sender, lobby) =>
        {
            Debug.Log("JoinLobby LobbyID=" + lobby.Id.ToString());
            JoinPublicLobby(lobby);
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
        //匿名ログイン
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update()
    {
        RefreshLobbyHeatbeat();
        //HandleLobbyPollForUpdates();

        if(m_joinedLobby == null) return;

        m_lobbyUpdateTimer += Time.deltaTime;

        if(m_lobbyUpdateTimer >= 0)
        {
            m_lobbyUpdateTimer = -2.1f;
            UpdateLobby();
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

    //private void GetPlayerData(Lobby lobby)
    //{
    //    foreach (Player player in lobby.Players)
    //    {
    //        string playerId = player.Id;

    //        if (player.Data.TryGetValue(
    //            "PlayerName",
    //            out PlayerDataObject data))
    //        {
    //            Debug.Log($"PlayerID : {playerId}");
    //            Debug.Log($"PlayerName : {data.Value}");
    //        }
    //    }
    //}

    public async void CreateLobby()
    {
        string lobbyName = m_lobbyInputField.text;

        if (lobbyName == null)
        {
            Debug.LogWarning("ロビーの名前を入力してください");
            return;
        }

        try
        {
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions()
            {
                IsPrivate = m_hasPrivate,
                //PlayerIdとはAuthenticationで取得したホストのPlayerID
                Player = new Player(AuthenticationService.Instance.PlayerId)
                {
                    Data = new Dictionary<string, PlayerDataObject>()
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, m_playerName)},
                        {
                            "Ready",
                            new PlayerDataObject(
                            PlayerDataObject.VisibilityOptions.Member,
                            "0"
                            )
                        }
                        
                    }
                }
                ,
                Data = new Dictionary<string, DataObject>()
                {
                    {"StartGame", new DataObject(DataObject.VisibilityOptions.Member, "0")},
                    {
                        KEY_RELAY_CODE,
                        new DataObject(DataObject.VisibilityOptions.Member,
                        ""
                        )
                    }
                }
            };
            m_isHost = true;
            m_joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, m_maxPlayers, createLobbyOptions);
            //lobby.id lobby.name lobby.code lobbymaxPlayers lobby.Players(参加してるプレイヤー一覧） lobby.IsPrivate
            Debug.Log("Lobby created: " + m_joinedLobby.Id);

            LobbyDisplay();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Error creating lobby: " + e.Message);
        }
    }

    //private async void JoinPrivateLobby(string name)
    //{

    //}

    private async void JoinPublicLobby(Lobby lobby)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = new Player(AuthenticationService.Instance.PlayerId)
                {
                    Data = new Dictionary<string, PlayerDataObject>()
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, m_playerName)},
                        {
                            "Ready",
                            new PlayerDataObject(
                            PlayerDataObject.VisibilityOptions.Member,
                            "0"
                            )
                        }
                    }

                },
            };
            m_joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
            Debug.Log("Lobby joined:" + m_joinedLobby.Id);

            LobbyDisplay();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Exception:" + e.Message);
        }
    }

    private async void RefreshLobbyHeatbeat()
    {

        if (!m_isHost) return;

        if (m_joinedLobby != null)
        {
            m_heartbeatTimer += Time.deltaTime;
            if(m_heartbeatTimer > m_heartbeatInterval)
            {
                m_heartbeatTimer = 0.0f;
                await LobbyService.Instance.SendHeartbeatPingAsync(m_joinedLobby.Id);
            }
        }
    }

    private async void UpdateLobby()
    {
        if (m_isUpdatingLobby) return;

        m_isUpdatingLobby = true;

        try
        {
            //最新状態を取得
            m_joinedLobby = await LobbyService.Instance.GetLobbyAsync(m_joinedLobby.Id);

            ShowPlayers();

            if(m_isHost)
            {
                if(IsEveryoneReady() && !m_isGameStarting)
                {
                    Debug.Log("ゲームを開始します");
                    await StartGame();

                }

            }
            else
            {
                await CheckGameStart();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Exception:" + e.Message);
        }
        finally
        {
            m_isUpdatingLobby = false;
        }
    }

    private void ShowPlayers()
    {
        int num = m_joinedLobby.Players.Count;

        foreach(Transform child in m_PlayerListContent)
        {
            Destroy(child.gameObject);
        }

        foreach(Player player in m_joinedLobby.Players)
        {
            //Debug.Log($"Player ID : {player.Id}");
            var obj = Instantiate(m_playerIconPrefab, m_PlayerListContent);
            player.Data.TryGetValue("PlayerName", out PlayerDataObject data);
            string name = data.Value;

            obj.GetComponent<LobbyPlayer>().SetPlayerText(name);
        }
    }

    private bool IsHost()
    {
        return m_joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private void LobbyDisplay()
    {
        //ホスト側　人数に応じたPrefabの生成と　Listでの保持
        //更新ごとにListと照らし合わせて　変化があれば変更
        //それぞれのUIとPlayerIdと照らし合わせて　キックを導入
        //自身の準備OKをLobbyDataに格納する
        //Lobbyデータの更新で変化する準備OKや　コスチュームを更新する
        ViewLobbyCanvas(true);

        //クライアント側　参加時に人数に応じたPrefabの生成と　Listの保持
        //キック以外はホストと同じ

        //ようはLobbyDataを更新し続けて変更を反映しつづける
    }

    private async void SetReady(bool isReady)
    {
        string playerId = AuthenticationService.Instance.PlayerId;

        //PlayerのDataに入れるものをあらかじめnewして作る
        var options = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {
                    "Ready",
                    new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member,
                        isReady ? "1" : "0"
                    )
                }
            }
        };

        //自身のPlayerのDataにnew Dataを更新　
        await LobbyService.Instance.UpdatePlayerAsync(
            m_joinedLobby.Id,
            playerId,
            options
        );
    }

    private bool IsEveryoneReady()
    {
        foreach(Player player in m_joinedLobby.Players)
        {
            if(!player.Data.TryGetValue("Ready", out PlayerDataObject readyData))
            {
                return false;
            }

            if(readyData.Value != "1")
            {
                return false;
            }
        }

        Debug.Log("全員準備OK");
        return true;
    }

    private async Task CheckGameStart()
    {

        if(m_isConnecting) return;

        if(!m_joinedLobby.Data.TryGetValue(
            "StartGame", out DataObject startData))
        {
            return;
        }

        if (startData.Value != "1") return;

        if(!m_joinedLobby.Data.TryGetValue(
            KEY_RELAY_CODE, out DataObject readyData))
        {
            return;
        }

        if(string.IsNullOrEmpty(readyData.Value)) return;

        m_isConnecting = true;

        try
        {
            Debug.Log("JoinRelay開始");

            await JoinRelay(readyData.Value);
        }
        catch (Exception e)
        {
            m_isConnecting = false;
            Debug.LogError(e.Message);
        }

    }

    private async Task StartGame()
    {
        if (m_isGameStarting) return;

        m_isGameStarting = true;

        try
        {
            string relayCode = await CreateRelay();

            bool success = NetworkManager.Singleton.StartHost();

            if (!success)
            {
                throw new Exception("Hostの開始に失敗しました");
            }

            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "StartGame",
                        new DataObject(DataObject.VisibilityOptions.Member,
                        "1"
                        )
                    },
                    {
                        KEY_RELAY_CODE,
                        new DataObject(DataObject.VisibilityOptions.Member,
                        relayCode
                        )
                    }
                }
            };

            m_joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                m_joinedLobby.Id,
                options);
        }
        catch (LobbyServiceException e)
        {
            m_isGameStarting = false;
            Debug.LogError(e);
        }
    }

    private async Task<string> CreateRelay()
    {
        int maxConnections = m_joinedLobby.MaxPlayers - 1;

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

        //接続用join codeを取得
        string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        //UnityTransportにRelayの接続情報を設定
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(
            allocation,
            "dtls"
        );

        transport.SetRelayServerData(relayServerData);
        return relayCode;
    }

    private async Task JoinRelay(string relayCode)
    {
        //Relayに参加するための情報を取得
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

        //UnityTransportを取得
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        transport.SetRelayServerData(relayServerData);

        bool success = NetworkManager.Singleton.StartClient();
        Debug.Log($"StartClient開始結果 : {success}");

        if (!success)
        {
            throw new Exception("Clientの開始に失敗しました");
        }
    }

    //Relayで接続時に呼ばれる　よってGameStart後に呼ばれる
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"接続イベント発生 ClientId : {clientId}");
        //ホスト以外はシーン移動しない
        if (!NetworkManager.Singleton.IsHost) return;

        if(m_isSceneLoading) return;

        int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;

        int lobbyPlayerCount = m_joinedLobby.Players.Count;

        Debug.Log($"接続人数:{connectedCount} / {lobbyPlayerCount}");

        if(connectedCount >= lobbyPlayerCount)
        {
            LoadGameScene();
        }
    }
    private void LoadGameScene()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (m_isSceneLoading) return;

        m_isSceneLoading = true;

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            LoadSceneMode.Single
        );
    }
    //optionsについて
    //そのロビーがPrivateかPublicか　IsPrivate
    //そのロビーに入るPlayerDataの情報
    //VisibilityOptions　public, member, private
    //それぞれ参加せずに読み取れる　参加した人物のみ読み取れる　Dataを所持するもののみ読みとれる
    //
    private void ViewCreateRoomCanvas(bool value) => m_createRoomCanvas.SetActive(value);
    private void ViewSearchPublicRoomCanvas(bool value) => m_searchPublicRoomCanvas.SetActive(value);
    private void ViewLobbyCanvas(bool value) => m_lobbyCanvas.SetActive(value);
    private void MaxPlayer(int num) => m_maxPlayers = num;
    private void SetHasPlayer(bool value) => m_hasPrivate = value;
    
}
