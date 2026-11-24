using Unity.Netcode;
using UnityEngine;

public class BossCharacterSpawner : NetworkBehaviour
{
    [SerializeField, Header("初登場後、雑魚の中に混ぜる？")]
    private bool _isSpawnWithZako = false;
    [SerializeField, Header("スポーンさせるボス")]
    private GameObject _spawnBoss = default;
    [SerializeField, Header("(雑魚の中に混ぜる場合)出現確率")]
    private int _spawnOdds = 10;

    public void SpawnBoss()
    {
        GameObject boss = Instantiate(_spawnBoss, this.transform.position, Quaternion.identity);
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

        ZombiModeSummonEnemy[] summoner = FindObjectsByType<ZombiModeSummonEnemy>();
        foreach(ZombiModeSummonEnemy useSummon in  summoner)
        {
            useSummon.AdditionBossInList(_spawnBoss, _spawnOdds);
        }
    }
}
