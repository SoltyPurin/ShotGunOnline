using UnityEngine;

public class EnemyContactStone : MonoBehaviour
{
    [SerializeField,Header("ノックバックのスクリプト")]
    protected EnemyKnockBack _enemyKnockBack = default;
    [SerializeField,Header("ダメージ計算のスクリプト")]
    protected EnemyTakeDamage _takeDamage = default;
    //コンボカウンター
    protected ComboCounter _counter;

    protected EnemyMove _enemyMove = default;

    private readonly string COMBOCOUNTERTAGNAME = "ComboCounter";
    private readonly string STONETAGNAME = "Stone";
    private void Start()
    {
        //インスペクターから登録はできないので最初にコンボカウンターを探索する
        GameObject countObj = GameObject.FindWithTag(COMBOCOUNTERTAGNAME);
        if(countObj != null)
        {
            _counter = countObj.GetComponent<ComboCounter>();
        }
        _enemyMove = GetComponent<EnemyMove>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(STONETAGNAME))
        {
            //岩に当たった時のメソッド呼び出し
            ContactStoneMethod(collision.gameObject);
        }
    }

    /// <summary>
    /// 岩に接触したときのメソッド
    /// </summary>
    /// <param name="collision">岩のゲームオブジェクト</param>
    public virtual void ContactStoneMethod(GameObject collision)
    {
        SetKnockBackStone stoneKnockBack = collision.GetComponent<SetKnockBackStone>();
        if(stoneKnockBack == null)
        {
            return;
        }
        //岩が吹き飛び状態であれば実行
        if (stoneKnockBack.State == StoneState.KnockBack)
        {
            if(_enemyMove == null)
            {
                return;
            }
            _enemyMove.RoadKill();
            //吹き飛ぶ方向をセット
            _enemyKnockBack.SetDirectionAndForce(collision.transform.position, 2, false,false);
            //岩のダメージ判定を呼び出す
            _takeDamage.ContactStoneMethod();
            //コンボカウンターの回数をプラスするメソッド呼び出し
            //_counter.ComboPlus();
        }

    }
}
