using System.Collections;
using UnityEngine;

/// <summary>
/// ギミックが出現するアニメーションを管理するクラス
/// （正直これ使わずEnemySpawnAnimと統合すれば良いのではと思い始めている）
/// </summary>
public class WaveGimmickSpawn : MonoBehaviour
{
    //ギミックの状態遷移用enum
    public enum GimmickState
    {
        Wait,
        Notice,
        GimmickSpawn,
    }

    [SerializeField, Header("アニメータ")]
    private Animator _spawnAnim = default;
    [SerializeField]
    private Animator _spawnPoint = default;

    [SerializeField, Header("コライダー")]
    private Collider2D _spawnCollider = default;

    [SerializeField, Header("スプライトの取得")]
    private SpriteRenderer[] _spriteRenderers = default;

    [SerializeField, Header("ギミック出現までの時間")]
    private float _spawnTime = 2.0f;
    [SerializeField, Header("コライダーがオンになるまでの時間")]
    private float _colliderOnTime = 0.55f;

    private float _timer = 0.0f;
    private GimmickState _state = GimmickState.Wait;

    private void Start()
    {
        //初期化とスプライトの非表示
        _timer = 0.0f;
        _spawnCollider.enabled = false;
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
        //switchで状態によって処理を変更
        switch (_state)
        {
            case GimmickState.Wait:
                break;
            case GimmickState.Notice:
                NoticeTimer();
                break;
            case GimmickState.GimmickSpawn:
                ColliderOn();
                break;
        }
    }

    /// <summary>
    /// 出現地点予告からギミック出現までを管理するメソッド
    /// </summary>
    private void NoticeTimer()
    {
        if (_timer >= _spawnTime)
        {
            _timer = 0.0f;
            foreach (SpriteRenderer sr in _spriteRenderers)
            {
                if (sr != null)
                {
                    sr.enabled = true;
                }
            }
            _spawnAnim.SetTrigger("Spawn");
            _spawnPoint.SetTrigger("Wait");
            _state = GimmickState.GimmickSpawn;
            return;
        }

        _timer += Time.deltaTime;
    }

    /// <summary>
    /// ギミック出現から判定復活までを管理するメソッド
    /// </summary>
    private void ColliderOn()
    {
        if(_timer >= _colliderOnTime)
        {
            _timer = 0.0f;
            _spawnCollider.enabled = true;
            _state = GimmickState.Wait;
            return;
        }

        _timer += Time.deltaTime;
    }

    /// <summary>
    /// ギミック出現開始用メソッド
    /// </summary>
    public void StartSpawn()
    {
        _spawnPoint.SetTrigger("Spawn");
        _state = GimmickState.Notice;
    }

    /// <summary>
    /// ボスが落とし穴に落下した後にランダムな場所に移動させるメソッド
    /// </summary>
    public IEnumerator RespawnFallHole()
    {
        yield return new WaitForSeconds(2);
        _spawnAnim.SetTrigger("Spawn");
        float randomRangeX = Random.Range(-20, 20);
        float randomRangeY = Random.Range(-20, 20);
        this.transform.position = new Vector2(randomRangeX, randomRangeY);
    }
}
