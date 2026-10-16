using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ランダムに雑魚敵を生成するメソッド
/// </summary>
public class SummonEnemy : NetworkBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("ボム兵")]
    private GameObject _bombEnemy = default;
    private GameObject _spawnPoint = default;

    [SerializeField, Header("敵のポップリスト")]
    private List<GameObject> _enemyList;

    //---int,floatなどの数値---------------------------------
    [SerializeField, Header("敵のポップ位置間隔")]
    private float _enemyPopPos = 7;


    private float _enemyPopTime = 10f;

    //タイマー
    private float _popTimer = 0;

    Vector3[] _spawnPositions = new Vector3[3];

    #endregion

    private void Start()
    {
        if (JsonSaver.Instance != null && JsonSaver.Instance.EnemyJson != null)
        {
            _enemyPopTime = JsonSaver.Instance.EnemyJson.EnemyPopTime;
        }
        //_popTimer = _enemyPopTime - 5;
        _popTimer = Mathf.Max(0, _enemyPopTime - 5f);

        _spawnPoint = GameObject.Find("SpawonPoint2");
        if (_spawnPoint != null)
        {
            _spawnPositions = new Vector3[]
    {
    new Vector3(_spawnPoint.transform.position.x,
    _spawnPoint.transform.position.y + _enemyPopPos,
    _spawnPoint.transform.position.z),

    new Vector3(_spawnPoint.transform.position.x + _enemyPopPos,
    _spawnPoint.transform.position.y - _enemyPopPos,
    _spawnPoint.transform.position.z),

    new Vector3(_spawnPoint.transform.position.x - _enemyPopPos,
    _spawnPoint.transform.position.y - _enemyPopPos,
    _spawnPoint.transform.position.z)
    };
            //_enemyPopTime = JsonSaver.Instance.EnemyJson.EnemyPopTime;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        _popTimer += Time.fixedDeltaTime;
        if (_popTimer >= _enemyPopTime)
        {
            PopEnemy();
        }
    }
    //敵生成
    private void PopEnemy()
    {

        SpawnEnemy(_bombEnemy, _spawnPositions[0]);

        int index = Random.Range(0, _enemyList.Count);
        SpawnEnemy(_enemyList[index], _spawnPositions[1]);

        int index2 = Random.Range(0, _enemyList.Count);
        SpawnEnemy(_enemyList[index2], _spawnPositions[2]);

        _popTimer = 0;
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
        //GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
        //NetworkObject nEnemy = enemy.GetComponent<NetworkObject>();
        //WaveObj waveObj = nEnemy.GetComponent<WaveObj>();
        //nEnemy.Spawn();
        //waveObj.PopAnim();
        //Destroy(enemy);
    }

}
