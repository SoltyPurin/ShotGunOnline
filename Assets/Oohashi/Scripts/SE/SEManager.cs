using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SEManager : MonoBehaviour
{
    [SerializeField, Header("効果音のクールタイム")]
    private float _seCoolTime = 0.1f;
    private bool _canPlaySE = true;
    [SerializeField, Header("オーディオソース")]
    private AudioSource _audioSource = default;//オーディオソース
    [SerializeField, Header("シールド用オーディオソース")]
    private AudioSource _shield_audioSource = default;//オーディオソース
    [SerializeField,Header("落下音用")]
    private AudioSource _fall_audioSource = default;//オーディオソース
    [SerializeField,Header("轢殺音用")]
    private AudioSource _roadKillAudioSource = default; //オーディオソース
    [SerializeField, Header("ダメージ系")]
    private AudioClip _enemyDamageSE = default;//ダメージ音
    [SerializeField,Header("死亡音")]
    private AudioClip _enemyDeathSE = default;//死亡音
    [SerializeField,Header("ボスが死ぬ音")]
    private AudioClip _bossDeathSE = default;//死亡音
    [SerializeField,Header("ガード音")]
    private AudioClip _guardSE = default;//ガード音
    [SerializeField,Header("爆発音")]
    private AudioClip _bombSE = default;//爆発音
    [SerializeField, Header("射撃音")]
    private AudioClip _enemyShotSE = default;//敵の射撃音
    [SerializeField, Header("その他")]
    private AudioClip _dropSE = default;//敵の落下音
    [SerializeField,Header("アーマーの突進音")]
    private AudioClip _armorRushSE = default;
    [SerializeField, Header("アーマーの後退音")]
    private AudioClip _armorBackSE = default;
    [SerializeField,Header("轢殺音")]
    private AudioClip _roadkillSE = default; //岩に轢かれた音
    [SerializeField,Header("コインが落ちる音")]
    private AudioClip _moneyDropSE = default;//コインが落ちる音
    [SerializeField, Header("ソウル床を踏んだ時の音")]
    private AudioClip _stepOnSoulFloor = default;
    [SerializeField, Header("システム音")]
    private AudioClip _enterSE = default;//クリック・決定音
    [SerializeField,Header("カーソル移動の音")]
    private AudioClip _cursorMoveSE = default;//カーソル移動
    [SerializeField,Header("キャンセルの音")]
    private AudioClip _cancelSE = default;//キャンセル


    /// <summary>
    /// 効果音のクールタイムをスタート
    /// </summary>
    private void CallSECoolTime()
    {
        _canPlaySE = false;
        StartCoroutine(SECoolTime());
    }

    /// <summary>
    /// クールタイム解除
    /// </summary>
    /// <returns></returns>
    private IEnumerator SECoolTime()
    {
        yield return new WaitForSeconds(_seCoolTime);
        _canPlaySE = true;
    }



    //ダメージ系--------------------------------------------------------------------------------
    public void PlayEnemyDamageSound()
    {
        if (!_canPlaySE)
        {
            return;
        }
        CallSECoolTime();
        _audioSource.PlayOneShot(_enemyDamageSE);
    }
    public void PlayEnemyDeathSound()
    {
        if (!_canPlaySE)
        {
            return;
        }
        CallSECoolTime();

        _audioSource.PlayOneShot(_enemyDeathSE);
    }
    public void PlayBossDeathSound()
    {
        _audioSource.PlayOneShot(_bossDeathSE);
    }
    public void PlayGuardSound()
    {
        _shield_audioSource.PlayOneShot(_guardSE);
    }
    public void PlayBombSound()
    {
        _audioSource.PlayOneShot(_bombSE);
    }

    public void PlayRoadKill()
    {
        _roadKillAudioSource.PlayOneShot(_roadkillSE);
    }

    public void PlayStepOnSoulFloor()
    {
        _audioSource.PlayOneShot(_stepOnSoulFloor);
    }

    public void ShieldBackSE()
    {
        _audioSource.PlayOneShot(_armorBackSE);
    }

    public void ShieldRushSE()
    {
        if (!_canPlaySE)
        {
            return;
        }
        CallSECoolTime();
        _audioSource.PlayOneShot(_armorRushSE);
    }
    //射撃音--------------------------------------------------------------------------------
    public void PlayEnemyShotSound()
    {
        _audioSource.PlayOneShot(_enemyShotSE);
    }
    //その他--------------------------------------------------------------------------------
    public void PlayDropSound()
    {
        if (!_canPlaySE)
        {
            return;
        }
        if (_dropSE != null && _audioSource != null && gameObject.activeInHierarchy)
        {
            _audioSource.PlayOneShot(_dropSE);
            CallSECoolTime();
        }
    }
    public void PlayMoneyDropSound()
    {
        if (!_canPlaySE)
        {
            return;
        }
        CallSECoolTime();

        _audioSource.PlayOneShot(_moneyDropSE);
    }
    //システム音--------------------------------------------------------------------------------
    public void PlayEnterSound()
    {
        _audioSource.PlayOneShot(_enterSE);
    }
    public void PlayCursorMoveSound()
    {
        _audioSource.PlayOneShot(_cursorMoveSE);
    }
    public void PlayCancelSound()
    {
        _audioSource.PlayOneShot(_cancelSE);
    }
}