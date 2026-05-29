using UnityEngine;

/// <summary>
/// ボスのWaveを生成するメソッド
/// </summary>
public class BossWaveCreate : MonoBehaviour
{
    [SerializeField]
    private GameObject _wave = default;
    private GameObject _respawnPointObj = default;
    private PlayerRespawn _playerRespawn = default;
    internal bool _waveSW = false;
    private bool _hasWaveSW = false;

    private void Start()
    {
        _respawnPointObj = GameObject.FindGameObjectWithTag("RespawnPoint");
        _playerRespawn = _respawnPointObj.GetComponent<PlayerRespawn>();
    }
    private void Update()
    {
        if (!_hasWaveSW && _waveSW)
        {
            _wave.SetActive(true);
            _playerRespawn.CountUp();
            _hasWaveSW = true;
        }
        else
        {
            return;
        }
    }
}
