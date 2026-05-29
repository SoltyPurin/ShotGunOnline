using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmorColorChange : MonoBehaviour
{
    [SerializeField, Header("突撃の待機時間取得用スクリプト")]
    private ArmorMove _armorMove = default;

    [SerializeField, Header("スプライト登録")]
    private SpriteRenderer _sprite = default;

    //赤に変化させるため緑と青を変化させる
    private float _green = 255;
    private float _blue = 255;

    private void FixedUpdate()
    {
        //盾持ちが回転(溜め)してるときに色を徐々に赤に変えていく
        if(_armorMove.InitState == ArmorState.Rotate)
        {
            float t = _armorMove.InitWaitTime / _armorMove.WaitLimitTime;
            _sprite.color = new Color(1, _green, _blue);
            _green = Mathf.Lerp(1, 0, t);
            _blue = Mathf.Lerp(1, 0, t);
        }//後隙で色を徐々に白に戻す
        else if(_armorMove.InitState == ArmorState.Stop)
        {
            float t = _armorMove.StopTime / _armorMove.RecoveryTime;
            _sprite.color = new Color(1, _green, _blue);
            _green = Mathf.Lerp(0, 1, t);
            _blue = Mathf.Lerp(0, 1, t);
        }
    }
}
