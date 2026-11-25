using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject m_playerPrefab;

    [SerializeField] private Transform m_spawnPos;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        foreach(ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject player = Instantiate(m_playerPrefab, m_spawnPos);

            NetworkObject networkObject = player.GetComponent<NetworkObject>();

            networkObject.SpawnAsPlayerObject(clientId);


        }
    }
}
