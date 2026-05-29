using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [SerializeField, Header("マズルフラッシュのオブジェクト登録")]
    private GameObject _flashObject = default;
    [SerializeField, Header("マズルフラッシュのアニメーター登録")]
    private Animator _flashAnimator = default;
    [SerializeField, Header("ジェネシスのアニメーター登録")]
    private Animator _ultAnitamtor = default;
    [SerializeField, Header("マズルのオブジェクト登録")]
    private GameObject _muzzleObject = default;
    [SerializeField,Header("プレイヤーのオブジェクト登録")]
    private GameObject _playerObject = default; 

    /// <summary>
    /// マズルフラッシュの座標をバレルに移動、回転をプレイヤーに合わせてエフェクトを再生
    /// </summary>
    public void PlayTheMuzzleFlash()
    {
        _flashObject.transform.position = _muzzleObject.transform.position;
        _flashObject.transform.rotation = _playerObject.transform.rotation;
        _flashAnimator.SetTrigger("Flash");
    }

    /// <summary>
    /// マズルフラッシュと同じ処理で違うエフェクトを再生
    /// </summary>
    public void PlayTheUltFlash()
    {
        _flashObject.transform.position = _muzzleObject.transform.position;
        _flashObject.transform.rotation = _playerObject.transform.rotation;
        _ultAnitamtor.SetTrigger("Flash");
    }
}
