using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    //ない
    private GameObject _flashObject = default;
    //ない
    private Animator _flashAnimator = default;
    //ない
    private Animator _ultAnitamtor = default;
    [SerializeField, Header("マズルのオブジェクト登録")]
    private GameObject _muzzleObject = default;
    [SerializeField,Header("プレイヤーのオブジェクト登録")]
    private GameObject _playerObject = default;


    private void OnEnable()
    {
        if(_flashObject == null)
        {
            _flashObject = GameObject.Find("FlashObject");
            _flashAnimator = _flashObject.transform.Find("Flash").GetComponent<Animator>();
            _ultAnitamtor = _flashObject.transform.Find("Flash_2").GetComponent <Animator>();
        }
    }
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
