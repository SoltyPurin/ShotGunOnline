using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
/// <summary>
/// ゾンビモードのクラス
/// 毎回同じ展開に同じ展開にしないためにObjListからランダムに敵を抜き出す
/// </summary>
public class ZombiWave : Wave
{
    [System.Serializable]
    public struct BossSpawnStruct
    {
        [Tooltip("出現させるボスのスポナー")]
        public GameObject bossSpawner;
        [Tooltip("何ウェーブに一回出現させる?")]
        public int spawnWaveCount;
    }
    [SerializeField, Header("ボス出現後何秒間雑魚のスポーンを止めるか")]
    private float _mobSpawnStopTime = 5.0f;
    [SerializeField,Header("ボスのスポナーと何ウェーブに一回出現させるか")]
    private List<BossSpawnStruct> _bossSpawnWaves = new List<BossSpawnStruct>();
    protected List<ZombiModeSummonEnemy> _summonEnemys = new List<ZombiModeSummonEnemy>();
    private int _currentWaveCount = 0;

    private bool _isBossSpawning = false;
    public override void Start()
    {
        foreach (GimmickData data in _gimmickList)
        {
            ZombiModeSummonEnemy zombi = data._waveGimmick.GetComponent<ZombiModeSummonEnemy>();
            if (zombi != null)
            {
                _enemySpawnInterval += zombi.EnemySpawnInterval;
                _summonEnemys.Add(zombi);
            }
        }

        //for (int i = 0; i < _gimmickList.Count; i++)
        //{
        //    GimmickData data = _gimmickList[i];

        //    if (data._waveGimmick == null)
        //    {
        //        Debug.LogError($"ギミックリストの {i} 番目が null です");
        //        continue;
        //    }

        //    ZombiModeSummonEnemy zombi =
        //        data._waveGimmick.GetComponent<ZombiModeSummonEnemy>();

        //    if (zombi != null)
        //    {
        //        _enemySpawnInterval += zombi.EnemySpawnInterval;
        //        _summonEnemys.Add(zombi);
        //    }
        //}


        //_enemySpawnInterval /= _gimmickList.Count;
        if (_summonEnemys.Count > 0)
        {
            _enemySpawnInterval /= _summonEnemys.Count;
        }
        base.Start();
        StartCoroutine(CallCreateTime());
    }

    private IEnumerator CallCreateTime()
    {
        yield return new WaitForSeconds(_enemySpawnInterval);
        _currentWaveCount++;
        _enemySpawnInterval = 0;
        foreach(ZombiModeSummonEnemy summon in  _summonEnemys)
        {
            _enemySpawnInterval += summon.EnemySpawnInterval;
        }
        if (_summonEnemys.Count > 0)
        {
            _enemySpawnInterval /= _summonEnemys.Count;
        }
        _counter.EnemyCount(0, (int)_difficulty, _waveType, _enemySpawnInterval);
        StartCoroutine(CallCreateTime());
        foreach(BossSpawnStruct structs in _bossSpawnWaves)
        {
            if (structs.spawnWaveCount % _currentWaveCount == 0 && _currentWaveCount != 1)
            {
                Debug.Log("割り算した結果は" + structs.spawnWaveCount+"%"+ _currentWaveCount + "=" +structs.spawnWaveCount%_currentWaveCount);
                BossCharacterSpawner spawner = structs.bossSpawner.GetComponent<BossCharacterSpawner>();
                spawner.SpawnBoss();
                _isBossSpawning = true;
            }
        }
        if( _isBossSpawning)
        {
            foreach(ZombiModeSummonEnemy summonEnemy in _summonEnemys)
            {
                summonEnemy.StartCoroutine(summonEnemy.SpawnStop(_mobSpawnStopTime));
            }
        }
    }

}
