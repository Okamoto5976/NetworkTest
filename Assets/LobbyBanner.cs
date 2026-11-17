using System;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyBanner : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_lobbyNameText;
    [SerializeField] private TextMeshProUGUI m_playerCountText;
    [SerializeField] private Button m_joinButton;

    public Lobby MyLobby {  get; private set; }
    public static EventHandler<Lobby> OnJoinLobby;

    public void Init(Lobby lobby)
    {
        MyLobby = lobby;
        m_lobbyNameText.text = lobby.Name;
        m_playerCountText.text = lobby.Players.Count.ToString() + "/" + lobby.MaxPlayers.ToString();
        m_joinButton.onClick.AddListener(() =>
        {
            OnJoinLobby?.Invoke(this, MyLobby);
            Debug.Log("JoinLobby LobbyID=" + MyLobby.Id.ToString());
        });
    }
}
