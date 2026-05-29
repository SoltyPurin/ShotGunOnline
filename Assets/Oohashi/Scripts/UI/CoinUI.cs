using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;

public class CoinUI : MonoBehaviour
{
    [SerializeField, Header("スクリプト取得")]
    private SoulKeep _coinScript = default;

    [SerializeField, Header("ウルト発動可能UI")]
    private GameObject _canUltImage = default;

    private const int NEEDUSEULTIMATECOIN = 10; //必殺技を使うのに必要なコインの枚数

    private void FixedUpdate()
    {
    //    if (_coinScript.UseFullSoul >= NEEDUSEULTIMATECOIN)
    //    {
    //        _canUltImage.SetActive(true);
    //    }
    //    else
    //    {
    //        _canUltImage.SetActive(false);
    //    }
    }

}
