using Unity.Netcode;
using UnityEngine;

public class MiddleBossCharacterSpawner : NetworkBehaviour
{
    #region protected]
    protected GameObject _spawnBoss = default;
    #endregion

    [SerializeField, Header("スポーンさせるボス")]
    private GameObject _spawnBossPrefab = default;

    [SerializeField, Header("初登場後、雑魚の中に混ぜる？")]
    private bool _isSpawnWithZako = false;
    [SerializeField, Header("(雑魚の中に混ぜる場合)出現確率")]
    private int _spawnOdds = 10;

    public virtual void SpawnBoss()
    {
        if (NetworkManager.Singleton == null ||!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        _spawnBoss = Instantiate(_spawnBossPrefab, this.transform.position, Quaternion.identity);
        NetworkObject nBoss = _spawnBoss.GetComponent<NetworkObject>();
        if (nBoss != null)
        {
            nBoss.Spawn();
            WaveObj waveObj = _spawnBoss.GetComponent<WaveObj>();
            if(waveObj != null)
            {
                waveObj.PopAnim();
            }
        }

        if (!_isSpawnWithZako)
        {
            return;
        }
        ZombiModeSummonEnemy[] summoner = FindObjectsByType<ZombiModeSummonEnemy>();
        foreach(ZombiModeSummonEnemy useSummon in  summoner)
        {
            useSummon.AdditionBossInList(_spawnBossPrefab, _spawnOdds);
        }
    }
}
