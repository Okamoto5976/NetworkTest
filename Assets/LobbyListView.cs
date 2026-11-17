using Unity.Services.Lobbies.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListView : MonoBehaviour
{
    [SerializeField] private GameObject m_lobbyBannerPrefab;
    [SerializeField] private Transform m_lobbyListContent;
    [SerializeField] private Button m_refreshButton;

    public static EventHandler OnRefreshLobbyListRequest;

    private void Start()
    {
        m_refreshButton.onClick.AddListener(() =>
        {
            OnRefreshLobbyListRequest?.Invoke(this, EventArgs.Empty);
        });
    }

    public void Refresh(List<Lobby> lobbyList)
    {
        foreach(Transform child in m_lobbyListContent)
        {
            Destroy(child.gameObject);
        }

        foreach(var lobby in lobbyList)
        {
            GameObject lobbyBanner = Instantiate(m_lobbyBannerPrefab, m_lobbyListContent);
            lobbyBanner.GetComponent<LobbyBanner>().Init(lobby);
        }
    }
}
