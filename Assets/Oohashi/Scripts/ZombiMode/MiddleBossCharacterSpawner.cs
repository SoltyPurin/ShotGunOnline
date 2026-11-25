using Unity.Netcode;
using UnityEngine;

public class MiddleBossCharacterSpawner : NetworkBehaviour
{
    #region protected
    [SerializeField, Header("スポーンさせるボス")]
    protected GameObject _spawnBossObj = default;
    #endregion

    [SerializeField, Header("初登場後、雑魚の中に混ぜる？")]
    private bool _isSpawnWithZako = false;
    [SerializeField, Header("(雑魚の中に混ぜる場合)出現確率")]
    private int _spawnOdds = 10;

    public virtual void SpawnBoss()
    {
        GameObject boss = Instantiate(_spawnBossObj, this.transform.position, Quaternion.identity);
        NetworkObject nBoss = boss.GetComponent<NetworkObject>();
        if (nBoss != null)
        {
            nBoss.Spawn();
            WaveObj waveObj = boss.GetComponent<WaveObj>();
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
            useSummon.AdditionBossInList(_spawnBossObj, _spawnOdds);
        }
    }
}
