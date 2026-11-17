using Unity.Netcode;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using static Wave;

public class ZombiWave : Wave
{
    private float _saveInitInterval = 0;

    public override void Start()
    {
        base.Start();
        _saveInitInterval = _enemySpawnInterval;
        _enemySpawnInterval = 0;
    }
    public override void FixedUpdate() 
    {
        _enemySpawnInterval -= Time.fixedDeltaTime;
        if(_enemySpawnInterval <=0)
        {
            base.FixedUpdate();
        }
    }

    public override void ObjSetTrue()
    {
        // 敵が存在しないならウェーブ終了可能、ステートを変更し早期リターン
        if (_objList == null || _objList.Count <= 0)
        {
            _isWaveEnd = true;
            _nowState = Transition.End;
            return;
        }

        // タイマーを加算
        _objFrame++;

        // generateFrame以上の時間が経ったらオブジェクトを有効化
        while (_objFrame >= _objList[_nowObj]._generateFrame)
        {
            WaveObj waveChildEnemy = _objList[_nowObj]._waveObj;

            if (waveChildEnemy != null)
            {
                // 1. 配置されている子供の敵の名前を取得
                string cleanedName = System.Text.RegularExpressions.Regex.Replace(waveChildEnemy.name, @"\s*\(\d+\)", "");
                string targetPrefabName = cleanedName.Replace("(Clone)", "").Trim();
                GameObject prefabToSpawn = null;

                // 2. ネットワークマネージャーの登録リストから「本物のプレハブ」を探す
                if (NetworkManager.Singleton != null)
                {
                    foreach (NetworkPrefabsList prefabList in NetworkManager.Singleton.NetworkConfig.Prefabs.NetworkPrefabsLists)
                    {
                        foreach (NetworkPrefab networkPrefab in prefabList.PrefabList)
                        {
                            if (networkPrefab.Prefab != null && networkPrefab.Prefab.name == targetPrefabName)
                            {
                                prefabToSpawn = networkPrefab.Prefab;
                                break;
                            }
                        }
                        if (prefabToSpawn != null) break;
                    }
                }

                // 3. 本物のプレハブが見つかったら、ダミーと同じ位置に生成してSpawn
                if (prefabToSpawn != null)
                {
                    GameObject spawnedEnemy = Instantiate(
                        prefabToSpawn,
                        waveChildEnemy.transform.position,
                        waveChildEnemy.transform.rotation
                    );

                    // ネットワーク上に一斉スポーン（クライアント側にも自動出現）
                    NetworkObject netObj = spawnedEnemy.GetComponent<NetworkObject>();
                    if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    {
                        netObj.transform.SetParent(null); // 親子関係を切る
                        netObj.Spawn();
                    }

                    // 生成された本物の敵の出現アニメーションを再生
                    WaveObj newWaveObj = spawnedEnemy.GetComponent<WaveObj>();
                    if (newWaveObj != null)
                    {
                        newWaveObj.PopAnim();

                        //// 既存のリストの参照を、新しく生まれた本物の敵に差し替える
                        //_objList[_nowObj] = new ObjData
                        //{
                        //    _generateFrame = _objList[_nowObj]._generateFrame,
                        //    _waveObj = newWaveObj
                        //};
                    }
                }
                else
                {
                    Debug.LogError($"[Wave] NetworkManager にプレハブ '{targetPrefabName}' が登録されていません！");
                }

                //// サーバー側でのみ、用済みになったシーン上のダミーオブジェクトを即座に削除
                //Destroy(waveChildEnemy.gameObject);
            }

            bool isLastObj = _nowObj >= _objList.Count - 1;
            // 次の敵へ移動
            NextObj();

            //敵の配列を一周したら一旦終了
            if (isLastObj || _nowState != Transition.Enemy)
            {
                _enemySpawnInterval = _saveInitInterval;
                if (_counter != null)
                {
                    _counter.EnemyCount(_objList.Count, (int)_difficulty, _waveType, _enemySpawnInterval);
                }
                break;
            }
        }

    }

    public override void NextObj()
    {
        if (_nowObj < (_objList.Count - 1))
        {
            ++_nowObj;
        }
        else
        {
            // 次の周回に移る
            _nowObj = 0;
            _objFrame = 0;
        }
    }
}
