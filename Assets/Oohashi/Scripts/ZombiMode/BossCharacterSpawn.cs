using UnityEngine;
using System.Collections.Generic;

public class BossCharacterSpawn : MiddleBossCharacterSpawner
{
    [System.Serializable]
    public struct BossEnhancedValues
    {
        //NavMesh2DAgentに渡す
        [Tooltip("増やす速度")]
        public float speed;
        //BossStateManagementに渡す
        [Tooltip("減らす後隙")]
        public float decInterval;
        //BossHPに渡す
        [Tooltip("増やす体力")]
        public int addHP;
    }

    [SerializeField,Header("ボスの強化値(出現するたびに上から足されていきます)")]
    private List<BossEnhancedValues> _bossEnhancedValue = new List<BossEnhancedValues>();

    private int _currentSpawnCount = 0;

    public override void SpawnBoss()
    {
        base.SpawnBoss();
        _currentSpawnCount++;
        _currentSpawnCount = Mathf.Clamp(_currentSpawnCount, 0, _bossEnhancedValue.Count-1);
        BossEnhance();
    }

    private void BossEnhance()
    {
        NavMesh2DAgent nav = _spawnBossObj.GetComponent<NavMesh2DAgent>();
        BossStateManagement bossState = _spawnBossObj.GetComponent<BossStateManagement>();
        BossHP bossHP = _spawnBossObj.GetComponent<BossHP>();
        if(nav != null)
        {
            nav.SpeedUp(_bossEnhancedValue[_currentSpawnCount].speed);
        }
        if(bossState != null)
        {
            bossState.EnhanceInterval(_bossEnhancedValue[_currentSpawnCount].decInterval);
        }
        if(bossHP != null)
        {
            bossHP.EnhanceBossHP(_bossEnhancedValue[_currentSpawnCount].addHP);
        }
    }
}
