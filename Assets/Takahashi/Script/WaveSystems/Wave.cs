using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1つのウェーブに出現するオブジェクトを管理するクラスで、オブジェクトと出現タイミングを設定できる。
/// 使う際には空オブジェクトにこれをアタッチし、子オブジェクトに敵を配置、その敵をリストに追加しよう。
/// </summary>
public class Wave : MonoBehaviour
{
    #region 【Structs】

    [System.Serializable]
    private struct GimmickData
    {
        [Tooltip("ギミック出現までの時間（フレーム）")]
        public int _generateFrame;
        [Tooltip("WaveGimmickを継承したギミックを入れる場所")]
        public WaveGimmick _waveGimmick;
    }

    [System.Serializable]
    public struct ObjData
    {
        [Tooltip("敵出現までの時間（フレーム）")]
        public int _generateFrame;
        [Tooltip("WaveObjを継承した敵を入れる場所")]
        public WaveObj _waveObj;
    }
    #endregion

    #region 【Enums】

    /// <summary>
    /// ウェーブの種類
    /// </summary>
    public enum WaveEnemyType
    {
        Normal,
        Boss,
        MediumBoss,
    }

    /// <summary>
    /// 難易度
    /// </summary>
    public enum Difficulty
    {
        Easy = 1,
        Normal = 2,
        Hard = 3
    }

    /// <summary>
    /// ウェーブの状態遷移
    /// </summary>
    private enum Transition
    {
        Wait,
        Gimmick,
        Enemy,
        End,
    }
    #endregion

    #region 【各種変数】

    [Header("ウェーブの属性")]
    public WaveEnemyType _waveType = WaveEnemyType.Normal;

    [SerializeField, Header("ギミックの格納場所")]
    private List<GimmickData> _gimmickList;

    [SerializeField, Header("敵の格納場所")]
    private List<ObjData> _objList;
    public List<ObjData> ObjList
    {
        get { return _objList; }
    }

    [SerializeField, Header("ウェーブの難易度")]
    private Difficulty _difficulty = Difficulty.Easy;

    [SerializeField, Header("自動遷移させるかどうか")]
    private bool _autoTransition = true;
    public bool AutoTransition
    {
        get { return _autoTransition; }
    }

    
    [SerializeField, Header("ウェーブ開始時に表示したいテキスト"),
        Tooltip("何も書かない場合は「Wave（その時のウェーブ数）」と表示されます。")]
    private string _waveStartText = default;
    public string WaveStartText
    {
        get { return _waveStartText; }
    }

    //各ウェーブのスコアを計算するスクリプトを取得
    private EnemyCountAndCreateTime _counter = default;

    //現在のウェーブの状態
    private Transition _nowState = Transition.Wait;

    //その他変数
    private int _nowGimmick = 0;
    private int _nowObj = 0;
    private int _gimmickFrame = 0;
    private int _objFrame = 0;
    private bool _waveEnd = false;
    private int _prevEnemyCount = 0;
    private int _initEnemyCount = 0;
    private WaveManager _waveManager = default;

    #endregion

    #region 定数
    private readonly string WAVE_MANAGER = "WaveManager";

    #endregion

    private void Start()
    {
        //各種変数の初期化
        _gimmickFrame = 0;
        _objFrame = 0;

        ObjSetFalse();

        //ウェーブスコアを取得
        GameObject count = GameObject.FindWithTag("WaveScore");

        //スコアカウントオブジェが存在するならGetConponent
        if ((count != null))
        {
            _counter = count.GetComponent<EnemyCountAndCreateTime>();
        }

        //スコアカウンター内に敵数と難易度を送信
        if (_counter != null)
        {
            _counter.EnemyCount(_objList.Count, (int)_difficulty,_waveType);
        }

        //自動スタートが有効ならウェーブの状態を変更しスタート可能に
        if (_autoTransition)
        {
            _nowState = Transition.Gimmick;
        }

        _waveManager = FindFirstObjectByType<WaveManager>();
        _prevEnemyCount = EnemyCount();
        _initEnemyCount = EnemyCount();
    }

    private void FixedUpdate()
    {
        //現在のウェーブの状態に応じ処理を変更する。
        switch (_nowState)
        {
            case Transition.Gimmick:
                GimmickSetTrue();
                break;

            case Transition.Enemy:
                ObjSetTrue();
                break;
        }

        _prevEnemyCount = EnemyCount();
        if(_prevEnemyCount != _initEnemyCount)
        {
            _initEnemyCount = _prevEnemyCount;
            _waveManager.SetChangeCount(_prevEnemyCount);
        }
        
    }

    /// <summary>
    /// ウェーブ生成を開始させるメソッド
    /// </summary>
    public void StartWave()
    {
        _nowState = Transition.Gimmick;
    }

    /// <summary>
    /// 全オブジェクトの無効化メソッド
    /// </summary>
    private void ObjSetFalse()
    {
        //ギミックを一旦全て無効化
        for(int i = 0; i < _gimmickList.Count; i++)
        {
            if (_gimmickList[i]._waveGimmick)
            {
                _gimmickList[i]._waveGimmick.gameObject.SetActive(false);
            }
        }

        //敵を一旦全て無効化
        for(int i = 0; i < _objList.Count; i++)
        {
            if (_objList[i]._waveObj != null )
            {
                _objList[i]._waveObj.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ギミックの有効化用メソッド
    /// </summary>
    private void GimmickSetTrue()
    {
        //ギミックが存在しないならステートを変更し早期リターン
        if(_gimmickList.Count <= 0)
        {
            _nowState = Transition.Enemy;
            return;
        }

        //タイマーを加算
        _gimmickFrame++;

        //generateFrame分時間が経ったら有効化して次へ
        while(_gimmickFrame >= _gimmickList[_nowGimmick]._generateFrame)
        {
            //リスト内にギミックが存在するなら、ギミックを出現させる。
            if (_gimmickList[_nowGimmick]._waveGimmick != null)
            {
                _gimmickList[_nowGimmick]._waveGimmick.gameObject.SetActive(true);
                _gimmickList[_nowGimmick]._waveGimmick.GimmickPopAnm();
            }

            //次のギミックへ移動
            NextGimmick();

            if (_nowState != Transition.Gimmick)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 敵の有効化用メソッド
    /// </summary>
    private void ObjSetTrue()
    {
        //敵が存在しないならウェーブ終了可能、ステートを変更し早期リターン
        if( _objList.Count <= 0)
        {
            _waveEnd = true;
            _nowState = Transition.End;
            return;
        }

        //タイマーを加算
        _objFrame++;

        //generateFrame以上の時間が経ったらオブジェクトを有効化
        while(_objFrame >= _objList[_nowObj]._generateFrame)
        {
            //リスト内に敵が存在するなら出現させる。
            if (_objList[_nowObj]._waveObj != null)
            {
                _objList[_nowObj]._waveObj.gameObject.SetActive(true);
                _objList[_nowObj]._waveObj.PopAnim();
            }

            //次の敵へ移動
            NextObj();

            if (_nowState != Transition.Enemy)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 次有効化するギミックの位置に遷移するメソッド
    /// </summary>
    private void NextGimmick()
    {
        if(_nowGimmick < (_gimmickList.Count - 1))
        {
            //指標を変更
            _nowGimmick++;
            _gimmickFrame = 0;
        }
        else
        {
            //ギミックセット完了を通達
            _nowState = Transition.Enemy;
        }
    }

    /// <summary>
    /// 次に有効化する敵の位置に遷移するメソッド
    /// </summary>
    private void NextObj()
    {
        //次のリストの位置に移動しframeの値を初期化
        if(_nowObj < (_objList.Count - 1))
        {
            //指標を変更
            ++_nowObj;
            _objFrame = 0;
        }
        else
        {
            //敵配置完了を通達
            _waveEnd = true;
            _nowState = Transition.End;
        }
    }

    /// <summary>
    /// ウェーブが終了できるかどうかの判定をするメソッド
    /// </summary>
    /// <returns>全ての敵が有効なら終了</returns>
    public bool IsEnd()
    {
        //for文でリスト内でまだ出現していない敵を判別
        for(int i = 0; i < _objList.Count; ++i)
        {
            if (_objList[i]._waveObj != null)
            {
                //出現完了していない敵がいたらfalseを返す。
                if (!_objList[i]._waveObj.IsEnd())
                {
                    return false;
                }
            }
        }

        //カウンターのタイマーをストップ
        if(_counter != null)
        {
            _counter.TimerStop();
            _counter.Calculator();
        }

        return _waveEnd;
    }

    /// <summary>
    /// 全ての敵が消えたかどうかの判定用メソッド
    /// </summary>
    /// <returns>敵がまだ存在するならfalse,しなかったらtrue</returns>
    public bool IsDelete()
    {
        for(int i = 0; i < _objList.Count; i++)
        {
            if (_objList[i]._waveObj != null)
            {
                return false;
            }
        }

        return true;
    }

    private int EnemyCount()
    {
        int count = 0;
        for(int i =0; i< _objList.Count; i++)
        {
            if(_objList[i]._waveObj != null)
            {
                count++;
            }
        }
        return count;
    }
}