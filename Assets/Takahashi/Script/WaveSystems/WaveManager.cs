using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ウェーブおよびステージプレハブの切り替えを管理するクラス
/// </summary>
public class WaveManager : Unity.Netcode.NetworkBehaviour
{
    #region 【Enum】
    public enum WaveType
    {
        Normal,
        Tutorial,
    }

    public enum TransitionState
    {
        Transition,
        Wave,
    }
    private TransitionState _nowTransitionState = TransitionState.Wave;
    public TransitionState NowTransitionState
    {
        get { return _nowTransitionState; }
    }
    #endregion

    #region 【変数】

    [SerializeField, Header("ステージシーケンスデータ（設定時はPrefab+SO方式でステージを動的生成）")]
    private StageSequenceSO _stageSequence = default;
    public StageSequenceSO StageSequence => _stageSequence;

    public bool UsesStageSequence =>
        _stageSequence != null &&
        _stageSequence.Stages != null &&
        _stageSequence.Stages.Count > 0;

    [SerializeField, Header("ウェーブのタイプ")]
    private WaveType _waveType = WaveType.Normal;
    public WaveType GameWaveType
    {
        get { return _waveType; }
    }

    [SerializeField, Header("ウェーブリスト（レガシー互換 / StageSequence設定時は自動同期）")]
    private List<Wave> _waveList = new List<Wave>();
    public List<Wave> WaveList
    {
        get { return _waveList; }
    }

    public int TotalWaveCount =>
        UsesStageSequence ? _stageSequence.Stages.Count : (_waveList != null ? _waveList.Count : 0);

    [SerializeField, Min(0), Header("ウェーブの番号")]
    private NetworkVariable<int> _waveIndex = new NetworkVariable<int>(0);
    public int WaveIndex
    {
        get { return _waveIndex.Value; }
    }

    [SerializeField, Header("取得系")]
    private BGMControl_Ver2 _bgmControll = default;
    [SerializeField]
    private WaveUIManager _uiManager = default;
    [SerializeField]
    private ScoreManager _scoreManager = default;
    [SerializeField]
    private SceneTransitionManager _sceneTransitionManager = default;
    [SerializeField]
    private WaveTransition _waveTransition = default;
    [SerializeField]
    GameObject _deathEffects = default;

    [SerializeField, Header("次に遷移するシーン")]
    private string _nextSceneName = default;
    public string NextSceneName
    {
        get
        {
            if (UsesStageSequence && !string.IsNullOrEmpty(_stageSequence.NextSceneNameOverride))
            {
                return _stageSequence.NextSceneNameOverride;
            }
            return _nextSceneName;
        }
    }

    //ウェーブプレハブを格納する用のリスト
    private List<Wave> _cloneList = new List<Wave>();

    //指標
    private int _cloneIndex = 0;

    //ウェーブが終了したか
    private bool _listEnd = false;
    public bool ListEnd
    {
        get { return _listEnd; }
    }
    private bool _startWave = false;

    //ボスウェーブかどうか
    private bool _isBossWave = false;
    public bool IsBossWave
    {
        get { return _isBossWave; }
    }

    // --- 動的ステージ生成管理 ---
    private Transform _stagingContainer;
    private Transform _activeStageContainer;
    private StageController _currentStageInstance;
    private int _loadedStageIndex = -1;
    private NavMeshBuilder2D _navMeshBuilder;

    public StageController CurrentStageInstance => _currentStageInstance;
    #endregion

    #region シングルトン化
    [HideInInspector]
    public static WaveManager _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (UsesStageSequence)
        {
            SyncWaveListFromStageSequence();
            EnsureStageContainers();
            // シーンロード直後（MP_GameManagerのプレイヤースポーン・開始ムービー前）にStage 0の地形とStageSpawnPointsを展開する
            LoadStageLocal(0);
        }
    }
    #endregion

    private void Start()
    {
        //数値の初期化
        _cloneList.Clear();

        //チュートリアルステージだった場合、ウェーブ遷移の演出をカットするように
        if (_waveType == WaveType.Tutorial)
        {
            _waveTransition.Tutorial = true;
        }

        switch (_waveType)
        {
            case WaveType.Tutorial:
                _bgmControll.BGM_Start(2);
                _bgmControll.BGM_Pause();
                break;

            case WaveType.Normal:
                _bgmControll.BGM_Start(0);
                _bgmControll.BGM_Pause();
                break;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 途中参加クライアントや再同期時にホストと同じステージが読み込まれていることを保証する
        if (UsesStageSequence && !IsServer)
        {
            EnsureStageLoaded(_waveIndex.Value);
        }
    }

    private void FixedUpdate()
    {
        if (!_startWave)
        {
            return;
        }

        switch (_nowTransitionState)
        {
            case TransitionState.Wave:
                WaveDelete();
                break;

            case TransitionState.Transition:
                EndTransition();
                break;
        }
    }

    #region 【ステージ動的ロード制御】

    /// <summary>
    /// StageSequenceSO の設定から _waveList をメモリ上で同期し、既存スクリプト（ScoreData等）の互換性を維持
    /// </summary>
    private void SyncWaveListFromStageSequence()
    {
        if (!UsesStageSequence) return;

        if (_waveList == null)
        {
            _waveList = new List<Wave>();
        }
        else
        {
            _waveList.Clear();
        }

        for (int i = 0; i < _stageSequence.Stages.Count; i++)
        {
            Wave template = _stageSequence.Stages[i] != null
                ? _stageSequence.Stages[i].ResolveWaveTemplate()
                : null;
            _waveList.Add(template);
        }
    }

    private void EnsureStageContainers()
    {
        if (_stagingContainer == null)
        {
            GameObject stagingObj = new GameObject("[StageSystem_Staging]");
            stagingObj.SetActive(false);
            _stagingContainer = stagingObj.transform;
        }

        if (_activeStageContainer == null)
        {
            GameObject activeObj = new GameObject("[ActiveStage]");
            activeObj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _activeStageContainer = activeObj.transform;
        }
    }

    /// <summary>
    /// 暗転中（テレポート直前）にサーバーから全クライアントへ次ステージの切り替えを指示
    /// </summary>
    public void TransitionToStage(int stageIndex)
    {
        if (!UsesStageSequence) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsServer)
        {
            LoadStageClientRpc(stageIndex);
        }
        else
        {
            LoadStageLocal(stageIndex);
        }
    }

    [ClientRpc]
    private void LoadStageClientRpc(int stageIndex)
    {
        LoadStageLocal(stageIndex);
    }

    /// <summary>
    /// 指定インデックスのステージが未ロードであれば即座にロード
    /// </summary>
    public void EnsureStageLoaded(int stageIndex)
    {
        if (!UsesStageSequence) return;
        if (_loadedStageIndex == stageIndex && _currentStageInstance != null) return;

        LoadStageLocal(stageIndex);
    }

    /// <summary>
    /// 前のステージを破棄し、指定インデックスのステージプレハブを原点(0,0,0)に生成してNavMesh2Dとリスポーン地点を更新
    /// </summary>
    public void LoadStageLocal(int stageIndex)
    {
        if (!UsesStageSequence) return;
        if (stageIndex < 0 || stageIndex >= _stageSequence.Stages.Count)
        {
            Debug.LogError($"[WaveManager] 不正なステージインデックスです: {stageIndex}");
            return;
        }

        if (_loadedStageIndex == stageIndex && _currentStageInstance != null)
        {
            return;
        }

        EnsureStageContainers();

        // 前のステージがあれば、前ウェーブの岩（WaveGimmick）等をデスポーンしてから即座に非アクティブ化・破棄
        if (_currentStageInstance != null)
        {
            if (_currentStageInstance.StageWave != null)
            {
                _currentStageInstance.StageWave.CleanupSpawnedObjects();
            }
            _currentStageInstance.DespawnNetworkGimmicks();
            _currentStageInstance.gameObject.SetActive(false);
            Destroy(_currentStageInstance.gameObject);
            _currentStageInstance = null;
        }

        // 念のため親から切り離されてシーン上に残っている前ウェーブのネットワークギミック（Stone等）もサーバー側で一掃
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsServer)
        {
            foreach (WaveGimmick leftoverGimmick in FindObjectsByType<WaveGimmick>(FindObjectsInactive.Exclude))
            {
                if (leftoverGimmick == null || leftoverGimmick.transform.parent != null) continue;
                NetworkObject netObj = leftoverGimmick.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
                else
                {
                    Destroy(leftoverGimmick.gameObject);
                }
            }
        }

        StageSequenceSO.StageEntry entry = _stageSequence.Stages[stageIndex];
        if (entry == null || entry.StagePrefab == null)
        {
            Debug.LogError($"[WaveManager] StageSequence の要素 {stageIndex} に StagePrefab が設定されていません。");
            return;
        }

        // 非アクティブなStaging親の下に生成することで、Wave内のダミー敵や演出がStartWave前にAwake/OnEnableするのを防止
        StageController instance = Instantiate(
            entry.StagePrefab,
            Vector3.zero,
            Quaternion.identity,
            _stagingContainer
        );
        instance.name = entry.StagePrefab.name;
        instance.AutoResolveReferencesIfNeeded();

        // WavePrefabOverride が指定されている場合は差し替え
        if (entry.WavePrefabOverride != null)
        {
            if (instance.StageWave != null)
            {
                DestroyImmediate(instance.StageWave.gameObject);
            }

            Wave overrideWave = Instantiate(entry.WavePrefabOverride, Vector3.zero, Quaternion.identity, instance.transform);
            overrideWave.name = "Wave";
            overrideWave.transform.localPosition = Vector3.zero;
            NetworkObject legacyNetObj = overrideWave.GetComponent<NetworkObject>();
            if (legacyNetObj != null)
            {
                DestroyImmediate(legacyNetObj);
            }
            instance.SetStageWave(overrideWave);
        }

        // StartWaveが呼ばれるまでWaveオブジェクト自体とダミー敵を非表示に保つ
        if (instance.StageWave != null)
        {
            instance.StageWave.PrepareDummyObjectsInactive();
            instance.StageWave.gameObject.SetActive(false);
        }

        // アクティブなステージルートへ移動（地形・Collider・StageSpawnPoints が有効化される）
        instance.transform.SetParent(_activeStageContainer, false);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        _currentStageInstance = instance;
        _loadedStageIndex = stageIndex;

        // PlayerRespawn に新ステージを登録
        PlayerRespawn respawn = FindAnyObjectByType<PlayerRespawn>();
        if (respawn != null)
        {
            respawn.SetActiveStage(_currentStageInstance);
        }

        // Physics2DのCollider形状を即座に反映して2D NavMeshを再ベイク
        RebuildStageNavMesh();

        Debug.Log($"[WaveManager] ステージ {stageIndex + 1}/{_stageSequence.Stages.Count} ('{entry.StageName}') をロードしました。");
    }

    /// <summary>
    /// 現在アクティブなステージの地形に合わせてNavMeshを再構築
    /// </summary>
    public void RebuildStageNavMesh()
    {
        Physics2D.SyncTransforms();

        if (_navMeshBuilder == null)
        {
            _navMeshBuilder = FindAnyObjectByType<NavMeshBuilder2D>();
        }

        if (_navMeshBuilder == null)
        {
            GameObject navObj = new GameObject("NavBuildMan");
            _navMeshBuilder = navObj.AddComponent<NavMeshBuilder2D>();
            _navMeshBuilder.bakeOnEnable = true;
            _navMeshBuilder.updateMethod = NavMeshBuilder2D.UpdateMethod.Manual;
        }

        _navMeshBuilder.RebuildNavmesh(false);
    }

    private Wave GetWaveMetadataAt(int index)
    {
        if (UsesStageSequence && index >= 0 && index < _stageSequence.Stages.Count)
        {
            return _stageSequence.Stages[index].ResolveWaveTemplate();
        }

        if (_waveList != null && index >= 0 && index < _waveList.Count)
        {
            return _waveList[index];
        }

        return null;
    }

    private string GetWaveStartTextAt(int index)
    {
        if (UsesStageSequence && index >= 0 && index < _stageSequence.Stages.Count)
        {
            string overrideText = _stageSequence.Stages[index].WaveStartTextOverride;
            if (!string.IsNullOrEmpty(overrideText))
            {
                return overrideText;
            }
        }

        Wave meta = GetWaveMetadataAt(index);
        return meta != null ? meta.WaveStartText : string.Empty;
    }

    #endregion

    /// <summary>
    /// ウェーブを開始する用のメソッド
    /// </summary>
    /// <param name="index">index番目のウェーブを開始しcloneListに格納する。</param>
    public void StartWave(int index)
    {
        if (!IsServer)
        {
            return;
        }

        if (UsesStageSequence)
        {
            EnsureStageLoaded(index);

            if (_currentStageInstance != null && _currentStageInstance.StageWave != null)
            {
                Wave stageWave = _currentStageInstance.StageWave;
                stageWave.PrepareDummyObjectsInactive();
                stageWave.gameObject.SetActive(true);
                _cloneList.Add(stageWave);
            }
            else
            {
                Debug.LogError($"[WaveManager] ステージ {index} に Wave コンポーネントが見つかりません。");
            }

            StartWaveClientRpc(index);
            return;
        }

        // --- レガシー互換モード（_waveList直接指定時） ---
        Wave cloneWave = Instantiate(_waveList[_waveIndex.Value]);
        _cloneList.Add(cloneWave);

        foreach (Wave.ObjData enemyData in cloneWave.ObjList)
        {
            if (enemyData._waveObj != null)
            {
                enemyData._waveObj.gameObject.SetActive(true);
            }
        }

        if (cloneWave.GetComponent<NetworkObject>() != null)
        {
            cloneWave.GetComponent<NetworkObject>().Spawn();
        }

        foreach (Wave.ObjData enemyData in cloneWave.ObjList)
        {
            if (enemyData._waveObj != null)
            {
                if (enemyData._waveObj.CompareTag("Boss"))
                {
                    continue;
                }
                enemyData._waveObj.gameObject.SetActive(false);
            }
        }

        StartWaveClientRpc(index);
    }

    /// <summary>
    /// ネットワーク対応のウェーブスタート処理
    /// </summary>
    /// <param name="index"></param>
    [ClientRpc]
    private void StartWaveClientRpc(int index)
    {
        if (!_startWave)
        {
            _startWave = true;
        }

        // クライアント端末でもステージ内のWave（中ボスムービーCanvas/Timeline等の演出オブジェクト含む）を有効化する
        if (UsesStageSequence && !IsServer)
        {
            EnsureStageLoaded(index);
            if (_currentStageInstance != null && _currentStageInstance.StageWave != null)
            {
                _currentStageInstance.StageWave.PrepareDummyObjectsInactive();
                _currentStageInstance.StageWave.gameObject.SetActive(true);
            }
        }

        Wave waveMeta = GetWaveMetadataAt(index);
        if (waveMeta == null) return;

        if (_waveType != WaveType.Tutorial)
        {
            _uiManager.SetFirstEnemyCounter(waveMeta.ObjList.Count);
            _uiManager.ReMoveRank();
        }

        //ウェーブのオート遷移が有効なら演出を出す。
        if (waveMeta.AutoTransition)
        {
            StartWaveProduction(index);
        }
    }

    /// <summary>
    /// ウェーブ開始時の演出を出す用メソッド
    /// </summary>
    /// <param name="index"></param>
    public void StartWaveProduction(int index)
    {
        int waveCount = index + 1;
        Wave waveMeta = GetWaveMetadataAt(index);
        if (waveMeta == null) return;

        //ボスウェーブだった場合、BOSSと表示しBGMを変える。
        if (waveMeta._waveType == Wave.WaveEnemyType.Boss)
        {
            _isBossWave = true;
            _uiManager.TextChangeClientRpc("BOSS", 0);
            _uiManager.HiddenText(0);
            _bgmControll.BGM_Start(1);
        }
        else
        //普通のウェーブだった場合、現在ウェーブ数を表示する。
        {
            //チュートリアルかそうでないかで演出を変更
            switch (_waveType)
            {
                case WaveType.Normal:
                    //初回だった場合BGMを流すよ
                    if (index == 0)
                    {
                        _bgmControll.BGM_UnPause();
                    }
                    //ウェーブ開始時テキストがあるなら、それを表示。無いなら現在のウェーブ数を表示。
                    string startText = GetWaveStartTextAt(index);
                    if (!string.IsNullOrEmpty(startText))
                    {
                        _uiManager.TextChangeClientRpc(startText, 0);
                    }
                    else
                    {
                        _uiManager.TextChangeClientRpc("Wave " + waveCount, 0);
                    }

                    _uiManager.TextChangeClientRpc(waveCount + " / " + TotalWaveCount, 1);
                    _uiManager.HiddenText(0);
                    _uiManager.HiddenText(1);
                    break;

                case WaveType.Tutorial:
                    if (index == 0)
                    {
                        _bgmControll.BGM_UnPause();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// ウェーブの削除判定、及び次ウェーブ移行判定用のメソッド
    /// </summary>
    private void WaveDelete()
    {
        // 新ステージシステム使用時は、暗転切り替え時（LoadStageLocal）にステージごと破棄するため早期削除しない
        if (!UsesStageSequence)
        {
            for (int i = 0; i < _cloneList.Count; i++)
            {
                //ウェーブが終了可能だった場合、ウェーブを削除する。
                if (_cloneList[i] && _cloneList[i].IsDelete())
                {
                    Destroy(_cloneList[i].gameObject);
                }
            }
        }

        //ウェーブがリストに存在しない、あるいは全て終わった場合リターン
        if (TotalWaveCount <= 0 || IsEnd() || _cloneList.Count <= _cloneIndex)
        {
            return;
        }

        //ウェーブ終了可能なら次のウェーブへ
        if (_cloneList[_cloneIndex] != null && _cloneList[_cloneIndex].IsEnd())
        {
            GameObject[] eBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
            foreach (GameObject eBullet in eBullets)
            {
                Destroy(eBullet);
            }
            _uiManager.SetEnemyCounter(0);
            NextWave();
        }
    }

    /// <summary>
    /// ウェーブ移行用メソッド
    /// </summary>
    private void NextWave()
    {
        Debug.Log("ウェーブの数更新");
        //ウェーブがまだ残っているなら次のウェーブへ
        if (_waveIndex.Value < (TotalWaveCount - 1))
        {
            //それぞれの指標をプラスする
            _waveIndex.Value++;
            _cloneIndex++;

            Wave nextMeta = GetWaveMetadataAt(_waveIndex.Value);
            Wave.WaveEnemyType nextType = nextMeta != null ? nextMeta._waveType : Wave.WaveEnemyType.Normal;
            _waveTransition.StartTransition(nextType);
            _nowTransitionState = TransitionState.Transition;
        }
        //残っていないなら終了
        else
        {
            if (_waveType == WaveType.Tutorial)
            {
                TutorialWaveEnd();
            }
            else
            {
                BossWaveEnd();
            }
        }
    }

    /// <summary>
    /// 終了判定用メソッド
    /// </summary>
    /// <returns>_listEndの値をそのまま返す</returns>
    public bool IsEnd()
    {
        return _listEnd;
    }

    /// <summary>
    /// ウェーブ遷移が終了したかどうかを判別するメソッド
    /// </summary>
    private void EndTransition()
    {
        if (_waveTransition.EndTransition())
        {
            StartWave(_waveIndex.Value);
            _nowTransitionState = TransitionState.Wave;
        }
    }

    public void BossWaveEnd()
    {
        _listEnd = true;
        _scoreManager.TextAlpha(true);

        if (IsServer)
        {
            int min = _scoreManager.Minute;
            float sec = _scoreManager.Second;
            int totalDamage = ScoreData._scoreData != null ? ScoreData._scoreData.TotalTakenDamage : 0;
            SyncFinalScoreClientRpc(min, sec, totalDamage);
        }

        _sceneTransitionManager.StartSceneTransition();
    }

    /// <summary>
    /// 全クライアント端末の GetScoreManager にクリアタイムを同期書き込みする RPC
    /// </summary>
    [ClientRpc]
    private void SyncFinalScoreClientRpc(int minute, float seconds, int totalDamageCount)
    {
        if (GetScoreManager._this != null)
        {
            GetScoreManager._this.GetScore(minute, seconds);
        }
        else if (_scoreManager != null)
        {
            _scoreManager.ScoreCalculation();
        }

        if (ScoreData._scoreData != null)
        {
            ScoreData._scoreData.SyncTotalDamageFromHost(totalDamageCount);
        }
    }

    private void TutorialWaveEnd()
    {
        _listEnd = true;
        _scoreManager.TextAlpha(true);

        _sceneTransitionManager.StartSceneTransition();
    }

    /// <summary>
    /// 敵の数が変動した時にカウントを変えるスクリプト
    /// </summary>
    /// <param name="count">敵の数</param>
    [ClientRpc]
    public void SetChangeCountClientRpc(int count)
    {
        _uiManager.SetEnemyCounter(count);
    }

    /// <summary>
    /// サーバーで計算したスコア結果を全プレイヤーのScoreDataに反映させるRPC
    /// </summary>
    [ClientRpc]
    public void SyncScoreClientRpc(int timeRank, int damageCount, float clearTime)
    {
        EnemyCountAndCreateTime timer = FindAnyObjectByType<EnemyCountAndCreateTime>();
        if (timer != null)
        {
            timer.TimerStop();
        }

        if (ScoreData._scoreData != null)
        {
            ScoreData._scoreData.SetScore(timeRank, damageCount, clearTime);
        }
    }

    /// <summary>
    /// クライアントが被弾した際、全プレイヤーのタイマーとScoreDataにペナルティを同期する
    /// </summary>
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void NotifyTakeDamageServerRpc()
    {
        if (ScoreData._scoreData != null)
        {
            ScoreData._scoreData.AddDamageCount();
        }
        ApplyDamagePenaltyClientRpc();
    }

    /// <summary>
    /// 全プレイヤー端末のタイマーを減算させる ClientRpc
    /// </summary>
    [ClientRpc]
    private void ApplyDamagePenaltyClientRpc()
    {
        EnemyCountAndCreateTime timer = FindAnyObjectByType<EnemyCountAndCreateTime>();
        if (timer != null)
        {
            timer.TakeDamage();
        }
        else
        {
            Debug.Log("タイマーないよ");
        }
    }
}
