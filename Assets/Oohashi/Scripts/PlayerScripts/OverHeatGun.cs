using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverHeatGun : MonoBehaviour
{
    [SerializeField, Header("オーバーヒートの値取得")]
    private OverHeat _overHeat = default;

    [SerializeField, Header("ショットガンのスプライト登録")]
    private SpriteRenderer _sprite = default;

    //赤のカラーコード
    private float _red = 1;

    private void FixedUpdate()
    {
        //補正値をたたき出す
        float t = _overHeat.InitHeatGageValue / _overHeat.MaxHeatValue;
        //色を変更
        _sprite.color = new Color(1, _red, _red);
        //線形補正しながら色を変更
        _red = Mathf.Lerp(1, 0, t);

    }
}
