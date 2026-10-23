using System.Collections;
using UnityEngine;

/// <summary>
/// ボスのパンチエフェクト・残像拳を管理するメソッド
/// </summary>
public class BossZanzouken : MonoBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("出現させる残像")]
    private GameObject _bossGrow = default;
    [SerializeField, Header("生成された残像のリスト")]
    private GameObject[] _grows = new GameObject[4];
    [SerializeField, Header("パンチのエフェクト")]
    private GameObject _effect = default;
    [SerializeField, Header("出現する残像のカラー")]
    private Color[] _growsColors = new Color[4];

    //---int,float等の数値---------------------------------
    [SerializeField, Header("残像がボスに向かって動く速度")]
    private float _returnSpeed = 0;
    [SerializeField, Header("残像がフェードアウトする速度")]
    private float _fadeSpeed = 0;
    [SerializeField, Header("パンチエフェクトの最大生存時間(秒)")]
    private float _effectMaxDuration = 0.9f;

    private BossStateManagement _bossStateManagement = default;
    private Coroutine _autoOffCoroutine = null;

    #endregion

    private void Start()
    {
        _bossStateManagement = GetComponent<BossStateManagement>();
        if (_bossStateManagement == null)
        {
            _bossStateManagement = GetComponentInParent<BossStateManagement>();
        }
    }

    private void Update()
    {
        // ボスがパンチ状態以外に遷移した場合（怯み、ダメージ、死亡、クールダウン等）、
        // 殴りエフェクトが画面に残り続けるのを防ぐため強制的に消去する
        if (_effect != null && _effect.activeSelf)
        {
            if (_bossStateManagement != null && _bossStateManagement._currentState != BossStateManagement.BossState.Punch)
            {
                DelGrow();
            }
        }
    }

    private void OnDisable()
    {
        // ボスが非アクティブになった際も確実に残像・エフェクトを片付ける
        DelGrow();
    }

    public void InstantiateGrowByIndex(int index)
    {
        if (index < 0 || index >= _grows.Length)
        {
            return;
        }
        if (_bossGrow != null)
        {
            _grows[index] = Instantiate(_bossGrow, transform.position, transform.rotation);
            SpriteRenderer sr = _grows[index].GetComponent<SpriteRenderer>();
            if (sr != null && index < _growsColors.Length)
            {
                sr.color = _growsColors[index];
            }
        }
    }

    public void MoveGrow()
    {
        for (int i = 0; i < _grows.Length; i++)
        {
            if (_grows[i] != null)
            {
                _grows[i].transform.position = Vector3.Lerp(
                    _grows[i].transform.position,
                    transform.position,
                    _returnSpeed * Time.fixedDeltaTime);
            }
        }
    }

    /// <summary>
    /// 残像とパンチエフェクトを非表示・破棄する
    /// </summary>
    public void DelGrow()
    {
        if (_autoOffCoroutine != null)
        {
            StopCoroutine(_autoOffCoroutine);
            _autoOffCoroutine = null;
        }

        for (int i = 0; i < _grows.Length; i++)
        {
            if (_grows[i] != null)
            {
                Destroy(_grows[i]);
                _grows[i] = null; // 参照クリア
            }
        }

        if (_effect != null)
        {
            _effect.SetActive(false);
        }
    }

    /// <summary>
    /// パンチエフェクトを表示し、時間経過で自動非表示にするセーフティを起動
    /// </summary>
    public void PunchEffect()
    {
        if (_effect != null)
        {
            _effect.SetActive(true);

            // アニメーションイベントが中断されて DelGrow が呼ばれなかった場合でも、
            // 一定時間後に確実にエフェクトを消去するセーフティコルーチン
            if (_autoOffCoroutine != null)
            {
                StopCoroutine(_autoOffCoroutine);
            }
            if (gameObject.activeInHierarchy)
            {
                _autoOffCoroutine = StartCoroutine(AutoOffEffectRoutine(_effectMaxDuration));
            }
        }
    }

    private IEnumerator AutoOffEffectRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        DelGrow();
        _autoOffCoroutine = null;
    }
}