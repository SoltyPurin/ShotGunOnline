using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ZombiModeSummonEnemy : NetworkBehaviour
{
    [System.Serializable]
    public struct EnemyAndSpawnOdds
    {
        [Tooltip("出現させる敵")]
        public GameObject enemy;
        [Tooltip("出現確率")]
        public int odds;
    }

    [SerializeField, Header("出現させる敵の格納場所")]
    private List<EnemyAndSpawnOdds> _enemyList = new List<EnemyAndSpawnOdds>();
    [SerializeField, Header("敵が湧く半径")]
    private int _enemySpawnRadius = 5;
    [SerializeField, Header("1ウェーブ終わるたびに何m出現範囲を広げるか")]
    private int _spawnRadiusPlusValue = 1;
    [SerializeField, Header("何mに一体敵が湧くか")]
    private int _enemyApartSpawnMeters = 2;
    [SerializeField, Header("敵が湧く間隔")]
    private float _enemySpawnInterval = 5;
    public float EnemySpawnInterval
    {
        get { return _enemySpawnInterval; }
    }
    [SerializeField, Header("インターバルを何秒ずつ短縮するか")]
    private float _intervalReductionTime = 0.1f;


    private float _enemyPopTime = 10f;
    private float _popTimer = 0;

    private List<Vector2> _spawnPositions =  new List<Vector2>();
    private Vector2 _center = Vector2.zero;

    private int _divisionValueIncludeValue = 0;

    private bool _isSpawnStop = false;

    private void Start()
    {
        if (JsonSaver.Instance != null && JsonSaver.Instance.EnemyJson != null)
        {
            _enemyPopTime = JsonSaver.Instance.EnemyJson.EnemyPopTime;
        }
        _popTimer = Mathf.Max(0, _enemyPopTime - 5f);

        _center = this.transform.position;
        //円形のスポーン地点、東西南北に1体ずつスポーンさせるため最小単位は4
        //ウェーブが増えるたびに範囲が広がり、_enemySpawnRadiusを_enemyApartSpawnMetersで割った数(x)*4
        //centerに_enemyApartSpawnMetersにxを掛けた値を足し算or引き算した座標をスポーン地点とする
        CalcAndSetSpawnPos();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        if (_isSpawnStop)
        {
            return;
        }
        _popTimer += Time.fixedDeltaTime;
        if (_popTimer >= _enemySpawnInterval)
        {
            CalcAndSetSpawnPos();
            PopEnemy();
        }
    }

    public IEnumerator SpawnStop(float stopTime)
    {
        _isSpawnStop = true;
        Debug.Log("スポーン停止");
        yield return new WaitForSeconds(stopTime);
        _isSpawnStop = false;
        Debug.Log("スポーン再開");
    }

    private void PopEnemy()
    {
        if (_enemyList != null && _enemyList.Count > 0)
        {
            GameObject spawnEnemy = OddsLotteryGameObjectReturn();
            if(spawnEnemy == null)
            {
                spawnEnemy = _enemyList[0].enemy;
            }
            for(int i = 0; i< _spawnPositions.Count; i++)
            {
                SpawnEnemy(spawnEnemy, _spawnPositions[i]);
            }
        }
        _enemySpawnRadius += _spawnRadiusPlusValue;
        Debug.Log("湧く半径は" + _enemySpawnRadius);
        _enemySpawnInterval -= _intervalReductionTime;
        _popTimer = 0;
    }
    /// <summary>
    /// ランダムな値を生成し、それに対応した配列番号を返す
    /// </summary>
    /// <returns></returns>
    private GameObject OddsLotteryGameObjectReturn()
    {

        int totalOdds = 0;
        //確率の合計値を求める
        foreach (EnemyAndSpawnOdds data in _enemyList)
        {
            totalOdds += data.odds;
        }

        // 合計が0以下の場合は抽選できないため終了
        if (totalOdds <= 0) return null;
        int randomValue = Random.Range(1, totalOdds);
        foreach(EnemyAndSpawnOdds data in _enemyList)
        {
            if(randomValue < data.odds)
            {
                return data.enemy;
            }
            randomValue -= data.odds;
        }

        return null;
    }
    private void SpawnEnemy(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
        NetworkObject nEnemy = enemy.GetComponent<NetworkObject>();

        if (nEnemy != null)
        {
            // ネットワーク上にスポーンさせる（全クライアントに同期）
            nEnemy.Spawn();

            WaveObj waveObj = enemy.GetComponent<WaveObj>();
            if (waveObj != null)
            {
                waveObj.PopAnim();
            }
        }
    }




    private void CalcAndSetSpawnPos()
    {
        _divisionValueIncludeValue = _enemySpawnRadius / _enemyApartSpawnMeters;
        Debug.Log("forを回す回数は" + _divisionValueIncludeValue);
        _spawnPositions.Clear();
        for (int i = 1; i <= _divisionValueIncludeValue; i++)
        {
            int calcValue = _enemyApartSpawnMeters * i;
            //北
            _spawnPositions.Add(new Vector2(_center.x, _center.y + calcValue));
            //南
            _spawnPositions.Add(new Vector2(_center.x, _center.y - calcValue));
            //東
            _spawnPositions.Add(new Vector2(_center.x + calcValue, _center.y));
            //西
            _spawnPositions.Add(new Vector2(_center.x - calcValue, _center.y));
        }

    }

    public void AdditionBossInList(GameObject bossObj, int odds)
    {
        EnemyAndSpawnOdds eAo = new EnemyAndSpawnOdds();
        eAo.enemy = bossObj;
        eAo.odds = odds;
        _enemyList.Add(eAo);
    }
}
