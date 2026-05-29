using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [Header("銃弾のオブジェクト登録")]
    [SerializeField] private GameObject _bullet = default;
    [Header("銃弾の飛ぶ方向を計算するスクリプトを登録")]
    [SerializeField] private CulcBulletDirection _culcbulletDirection = default;
    private const int POOLBULLETLIMIT = 100; //プールで用意する最大銃弾量、100では足りないので要調整
    //プール用配列を用意
    [SerializeField] private GameObject[] _bulletPool = new GameObject[POOLBULLETLIMIT];
    //銃弾をばらけさせるために必要なフラグ、消してはいけない
    private bool _isPlus = false;

    //一回の射撃でアクティブにする銃弾の数
    private int _spawnBulletValue = 5;
    private void Awake()
    {
        for (int i = 0; i < POOLBULLETLIMIT; i++)//銃弾を生成してプールに登録
        {
            //生成しつつGameObject型のobjに登録
            GameObject obj = Instantiate(_bullet, transform.position, Quaternion.identity);
            obj.SetActive(false);//最初は非表示
            _bulletPool[i] = obj;//配列に登録
        }
    }

    /// <summary>
    /// 銃弾をアクティブにし、目標方向に飛ばすメソッド
    /// </summary>
    /// <param name="direction">方向</param>
    /// <param name="position">現在地点</param>
    /// <param name="chargeValue">チャージの値</param>
    public void ActiveBullet(Vector3 direction, Vector3 position, float chargeValue)
    {
        int spawnBulletValue = 5;
        int initActivatedBulletCount = 0;
        for (int i = 0; i < _bulletPool.Length && initActivatedBulletCount < spawnBulletValue; i++)
        {
            GameObject bullet = _bulletPool[i];
            //その銃弾がアクティブじゃないときにアクティブ化の命令を実行
            if (!bullet.activeInHierarchy)
            {
                bullet.transform.position = position;
                bullet.SetActive(true);

                //アクティブにした銃弾についてるbulletScriptを取得できたら実行
                if (bullet.TryGetComponent(out Bullet bulletScript))
                {
                    //銃弾の色を変更して軌跡を表示
                    bullet.GetComponent<SpriteRenderer>().color = Color.white;
                    bullet.GetComponent<TrailRenderer>().startColor = Color.white;
                    //チャージが伸びるほど銃弾の生存時間が伸びるためこのようにしてる
                    bulletScript.ChargeTime = 2.1f-chargeValue;
                    //銃弾の飛ぶ方向を計算して代入
                    Vector3 modifiedDir = _culcbulletDirection.CulcDirection(direction, chargeValue, _isPlus);
                    //ばらけさせるフラグを切り替え
                    _isPlus = !_isPlus;
                    //銃弾に飛ぶべき方向を渡す
                    bulletScript.Init(modifiedDir, position);
                }
                //アクティブにした銃弾の数を+
                initActivatedBulletCount++;
            }
        }
    }

    /// <summary>
    /// ウルトの銃弾をアクティブにするメソッド
    /// </summary>
    /// <param name="direction">方向</param>
    /// <param name="position">現在地点</param>
    public void ActiveUltBullet(Vector3 direction, Vector3 position)
    {
        int spawnBulletValue = 100;
        int initActivatedBulletCount = 0;
        for (int i = 0; i < _bulletPool.Length && initActivatedBulletCount < spawnBulletValue; i++)
        {
            GameObject bullet = _bulletPool[i];
            if (!bullet.activeInHierarchy)
            {
                bullet.transform.position = position;
                bullet.SetActive(true);

                if (bullet.TryGetComponent(out Bullet bulletScript))
                {
                    //ウルトの銃弾の色は通常弾と違う
                    bullet.GetComponent<SpriteRenderer>().color = new Color32(0, 255, 187, 1);
                    bullet.GetComponent<TrailRenderer>().startColor = new Color32(0, 255, 187, 1);
                    Vector3 modifiedDir = _culcbulletDirection.CulcDirection(direction, 2, _isPlus);
                    _isPlus = !_isPlus;
                    bulletScript.Init(modifiedDir, position);
                }

                initActivatedBulletCount++;
            }
        }


    }
}
