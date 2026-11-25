using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class TestPlayer : NetworkBehaviour
{
    [SerializeField]
    private float _moveSpeed = 5f;

    public override void OnNetworkSpawn()
    {
        enabled = IsOwner;
    }

    public override void OnGainedOwnership()
    {
        enabled = true;
    }

    public override void OnLostOwnership()
    {
        enabled = false;
    }

    private void Update()
    {
        Vector2 input = NetTest_PlayerInput.ReadMove();
        if (input == Vector2.zero) return;

        Vector3 move = new Vector3(input.x, input.y, 0f);
        transform.position += move * (_moveSpeed * Time.deltaTime);
    }
}
