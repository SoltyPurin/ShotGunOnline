using UnityEngine;

/// <summary>
/// 敵の出現アニメーションを管理するクラス
/// 【作成者：髙橋英士】
/// </summary>
public class EnemySpawnAnim : MonoBehaviour
{
    //アニメーションのステータス
    public enum AnimState
    {
        Wait,
        SpawnNotice,
        EnemySpawn,
    }

    [SerializeField, Header("アニメーターの取得")]
    private Animator _enemySprite = default;
    [SerializeField]
    private Animator _spawnPoint = default;

    [SerializeField, Header("コライダーの取得")]
    private Collider2D _enemyCollider = default;

    [SerializeField, Header("敵スプライトの取得")]
    private SpriteRenderer[] _spriteRenderers = default;

    [SerializeField, Header("キャンバス取得")]
    private Canvas _spriteCanvas = default;

    [SerializeField, Header("スクリプト取得")]
    private EnemyMove _enemyMove = default;

    [SerializeField, Header("敵出現までの時間")]
    private float _spawnTime = 2.0f;
    [SerializeField, Header("敵の判定出現までの時間")]
    private float _enableColliderTime = 1.0f;

    [HideInInspector]
    public AnimState _state = AnimState.Wait;

    private float _timer = 0.0f;

    private void Start()
    {
        if(_enemyCollider == null)
        {
            return;
        }
        //初期化
        _timer = 0.0f;
        
        //出てくる前の敵の当たり判定を消し、移動しないようにする。
        _enemyCollider.enabled = false;
        _enemyMove.EnemyState = EnemyState.Wait;

        //敵のHPインジゲーターを見えなくする。
        if (_spriteCanvas != null)
        {
            _spriteCanvas.enabled = false;
        }

        //敵のスプライトを全て見えなくする。
        foreach (SpriteRenderer sr in _spriteRenderers)
        {
            if (sr != null)
            {
                sr.enabled = false;
            }
        }
    }

    private void FixedUpdate()
    {
        //switchを使い、stateによってアニメーションの遷移を管理
        switch(_state)
        {
            case AnimState.Wait:
                break;
            case AnimState.SpawnNotice:
                NoticeTimer();
                break;
            case AnimState.EnemySpawn:
                SpawnTimer();
                break;
        }
    }

    /// <summary>
    /// 出現地点を予告するアニメーションを管理するメソッド。
    /// _spawnTimeによって出てくるまでの時間を変えられる。
    /// </summary>
    private void NoticeTimer()
    {
        //タイマーの値が_spawnTime以上になったら敵出現アニメーションの方に遷移
        if (_timer >= _spawnTime)
        {
            //_spriteRenderers内にあるスプライト全てを見えるようにする。
            foreach (SpriteRenderer sr in _spriteRenderers)
            {
                if (sr != null)
                {
                    sr.enabled = true;
                }
            }
            PopAnim();
            _timer = 0.0f;
            _state = AnimState.EnemySpawn;
            return;
        }

        _timer += Time.deltaTime;
    }

    /// <summary>
    /// 敵がポップしてから判定やHPバーが表示されるまでを管理するメソッド。
    /// _enableColliderTimeによって出てくるまでの時間を変えられます。
    /// </summary>
    private void SpawnTimer()
    {
        //タイマーの値が_enableColliderTime以上になったら敵出現アニメーションの方に遷移
        if (_timer >= _enableColliderTime)
        {
            _enemyCollider.enabled = true;
            _enemyMove.EnemyState = EnemyState.move;
            if(_spriteCanvas != null)
            {
                _spriteCanvas.enabled = true;
            }
            _timer = 0.0f;
            _state = AnimState.Wait;
            return;
        }

        _timer += Time.deltaTime;
    }

    /// <summary>
    /// スポーンを開始させるメソッド
    /// </summary>
    public void StartAnim()
    {
        _spawnPoint.SetTrigger("Spawn");
        _state = AnimState.SpawnNotice;
    }

    /// <summary>
    /// 予告ゾーンを消し、敵がポップするアニメーションを再生させるためのメソッド
    /// </summary>
    private void PopAnim()
    {
        _spawnPoint.SetTrigger("Wait");
        _enemySprite.SetTrigger("Spawn");
    }
}
