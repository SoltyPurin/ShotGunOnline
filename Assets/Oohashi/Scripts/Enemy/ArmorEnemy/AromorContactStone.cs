using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AromorContactStone : EnemyContactStone
{
    [SerializeField, Header("通常のアーマーのステートを取得")]
    private ArmorMove _armorMove = default;

    [SerializeField,Header("中ボスアーマーの被弾スクリプト(雑魚はここに登録しない)")]
    private MediumArmorTakeDamage _mediumTakeDamage = default;

    [SerializeField,Header("アーマーの被弾スクリプト(中ボスはここに登録しない)")]
    private ArmorTakeDamage _armorTakeDamage = default;

    //爆発エフェクト
    private PlayTheBombEffect _playTheBombEffect = default;
    //SEのマネージャー
    private SEManager _seManager = default;

    private readonly string EFFECTTAGNAME = "EffectManager";

    private readonly string SEMANAGERTAGNAME = "SEManager";

    private readonly string COMBOCOUNTERTAGNAME = "ComboCounter";


    private void Start()
    {
        _enemyMove = GetComponent<ArmorMove>();
        GameObject effectManager = GameObject.FindWithTag(EFFECTTAGNAME);
        _playTheBombEffect = effectManager.GetComponent<PlayTheBombEffect>();
        GameObject seManagerObject = GameObject.FindWithTag(SEMANAGERTAGNAME);
        _seManager = seManagerObject.GetComponent<SEManager>();
        _counter = GameObject.FindWithTag(COMBOCOUNTERTAGNAME).GetComponent<ComboCounter>();
    }

    /// <summary>
    /// 岩に接触したときのメソッド、突撃中にぶつかった時の処理を追加
    /// </summary>
    /// <param name="collision">岩のゲームオブジェクト</param>
    public override void ContactStoneMethod(GameObject collision)
    {
        base.ContactStoneMethod(collision);
        //岩が砕けた時のエフェクト
        _playTheBombEffect.BreakEffect(transform.position);

        //岩を砕く
        Destroy(collision.gameObject);
        //突撃中だった場合処理を行う
        if (_armorMove.InitState == ArmorState.Rush)
        {
            //通常アーマーの処理
            if (_armorTakeDamage != null)
            {
                _armorTakeDamage.ContactStoneMethod();
            }
            else //中ボスアーマーの処理
            {
                _mediumTakeDamage.ContactStoneMethod();
            }
            //岩にぶつかったときのメソッド
            _armorMove.ContactWithSomethingDuring();
            //轢殺音再生
            _seManager.PlayRoadKill();
            //コンボ数にプラスする
            _counter.ComboPlus();
        }
    }
}
