using System.Collections.Generic;
using UnityEngine;
using System.Collections;
/// <summary>
/// ゾンビモードのクラス
/// 毎回同じ展開に同じ展開にしないためにObjListからランダムに敵を抜き出す
/// </summary>
public class ZombiWave : Wave
{
    protected List<ZombiModeSummonEnemy> _summonEnemys = new List<ZombiModeSummonEnemy>();
    public override void Start()
    {
        foreach(GimmickData data in _gimmickList)
        {
            ZombiModeSummonEnemy zombi = data._waveGimmick.GetComponent<ZombiModeSummonEnemy>();
            _enemySpawnInterval += zombi.EnemySpawnInterval;
            _summonEnemys.Add(zombi);
        }

        _enemySpawnInterval /= _gimmickList.Count;
        base.Start();
        Debug.Log("インターバルは" +  _enemySpawnInterval);
        StartCoroutine(CallCreateTime());
    }

    private IEnumerator CallCreateTime()
    {
        yield return new WaitForSeconds(_enemySpawnInterval);
        _enemySpawnInterval = 0;
        foreach(ZombiModeSummonEnemy summon in  _summonEnemys)
        {
            _enemySpawnInterval += summon.EnemySpawnInterval;
        }
        _enemySpawnInterval /= _summonEnemys.Count;
        _counter.EnemyCount(0, (int)_difficulty, _waveType, _enemySpawnInterval);
        Debug.Log("インターバルは" + _enemySpawnInterval);
        StartCoroutine(CallCreateTime());
    }
}
