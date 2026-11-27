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

    [SerializeField, Header("何秒でゾンビモードを終了するか")]
    private float _zombiModeEndTime = 120.0f;

    public override void Start()
    {
        base.Start();
        foreach (GimmickData data in _gimmickList)
        {
            ZombiModeSummonEnemy zombi = data._waveGimmick.GetComponent<ZombiModeSummonEnemy>();
            if (zombi != null )
            {
                _enemySpawnInterval += zombi.EnemySpawnInterval;
                _summonEnemys.Add(zombi);
            }
        }

        if (_summonEnemys.Count > 0)
        {
            _enemySpawnInterval /= _summonEnemys.Count;
        }
        StartCoroutine(CallCreateTime());
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        _zombiModeEndTime -= Time.fixedDeltaTime;
        if(_zombiModeEndTime <= 0)
        {
            SceneTransitionManager sceneTransition = FindAnyObjectByType<SceneTransitionManager>(); 
            if(sceneTransition != null)
            {
                int totalEnemyCount = 0;
                foreach(ZombiModeSummonEnemy summons in _summonEnemys)
                {
                    summons.SpawnStop();
                    totalEnemyCount += summons.SpawnCount;
                }
                _zombiModeEndTime = 120f;
                sceneTransition.StartSceneTransition();
                ScoreData scoreData = FindAnyObjectByType<ScoreData>();
                scoreData.GetTotalEnemyCount(totalEnemyCount);
            }
        }
    }
    public override void ObjSetFalse()
    {
        if (_objList != null)
        {
            //敵を一旦全て無効化
            for (int i = 0; i < _objList.Count; i++)
            {
                if (_objList[i]._waveObj != null)
                {
                    _objList[i]._waveObj.gameObject.SetActive(false);
                }
            }
        }

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
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            yield break;
        }
        foreach (BossSpawnStruct structs in _bossSpawnWaves)
        {
            if (_currentWaveCount %structs.spawnWaveCount  == 0)
            {
                MiddleBossCharacterSpawner spawner = structs.bossSpawner.GetComponent<MiddleBossCharacterSpawner>();
                spawner.SpawnBoss();
            }
        }
    }

    public override bool IsEnd()
    {
        return false;
    }
}
