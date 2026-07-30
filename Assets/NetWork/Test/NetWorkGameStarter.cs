using UnityEngine;
using Unity.Netcode;

public class NetWorkGameStarter : Unity.Netcode.NetworkBehaviour
{
    [SerializeField, Header("æ“¾Œn")]
    private WaveManager _waveManager = default;

    private bool _isGameStarted = false;
    private void Start()
    {
    }

    private void FixedUpdate()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (_isGameStarted)
        {
            return;
        }

        int conectingCount = NetworkManager.Singleton.ConnectedClientsList.Count;


        if(conectingCount >= 2)
        {
            _waveManager.StartWave(0);
            _isGameStarted = true;
        }
    }
    }
