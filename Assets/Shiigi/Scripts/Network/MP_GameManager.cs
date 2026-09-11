using Unity.Netcode;
using UnityEngine;

public class MP_GameManager : MonoBehaviour
{
    public static MP_GameManager Instance { get; private set; }

    NetworkObject[] _players = null;

    private void Awake()
    {
        // シングルトンセットアップ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // _players = MultiplayerSessionManager.Instance.
    }
}
