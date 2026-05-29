using UnityEngine.UI;
using UnityEngine;

public class OverHeat : MonoBehaviour
{
    [SerializeField, Header("プレイヤーの入力ステートを変更するためのスクリプト")]
    private InputPlayerShot _playerInput = default;
    [SerializeField, Header("オーバーヒートまでの最大値")]
    private float _maxHeatValue = 9;
    public float MaxHeatValue
    {
        get { return _maxHeatValue; }
    }
    [SerializeField, Header("オバヒ減算に乗算する値")]
    private float _substructionMultiplier = 1.0f;
    [SerializeField, Header("完全オーバーヒートした時の減算に乗算する値")]
    private float _overHeatSubstructionMultiplier = 2.0f;
    [SerializeField, Header("オバヒのアニメーション")]
    private Animator _overHeatAnimator = default;
    [SerializeField,Header("効果音再生")]
    private PlayerSEControlScript _seScript = default;
    //前のオバヒ減算値を保存
    private float _tempMultiplier = 0.0f;
    //現在のオバヒの値
    private float _initHeatGage = 0;
    public float InitHeatGageValue
    {
        get { return _initHeatGage; }
    }

    private float _complementValue = 0.0f;
    public float ComplementValue
    {
        get { return _complementValue; }
    }
    //オーバーヒート中か否か
    private bool _isOverHeating = false;
    //打つたびにプラス1してゲージに追加。
    //常にオーバーヒートゲージは減るがチャージショットはクールタイムもあるので基本オーバーヒートしない

    /// <summary>
    /// オーバーヒートゲージに+1する
    /// </summary>
    public void ShotCountPlus()
    {
        _initHeatGage++;
    }

    private void FixedUpdate()
    {
        _complementValue = _initHeatGage / _maxHeatValue;
        //オバヒゲージが0以上の時に減算していく
        if (_initHeatGage >= 0)
        {
            _initHeatGage -= Time.fixedDeltaTime * _substructionMultiplier;
        }

        //現在のオバヒゲージが最大値以上になってなおかつオバヒ中でないときに実行
        if(_initHeatGage >= _maxHeatValue && !_isOverHeating)
        {
            //オーバーヒートのフラグをON
            _isOverHeating = true;
            //ステートをオバーヒートにする
            _playerInput.ShootState = ShootState.OverHeat;
            //-----------ここから次のコメントアウトまででゲージ減算の値を入れ替える
            _tempMultiplier = _substructionMultiplier;
            _substructionMultiplier = _overHeatSubstructionMultiplier;
            _overHeatSubstructionMultiplier = _tempMultiplier;
            //-----------
            //オバヒのアニメーションを再生
            _overHeatAnimator.SetTrigger("Start");
            //オバヒの効果音を再生
            _seScript.PlayTheOverHeat();
        }
        //オバヒのゲージが0以下になってなおかつオバヒ中で合った場合実行
        else if(_initHeatGage <= 0 && _isOverHeating)
        {
            //ステートを射撃可能に変える
            _playerInput.ShootState = ShootState.CanShoot;
            //-----------ここから次のコメントアウトまででゲージ減算の値を入れ替える
            _tempMultiplier = _substructionMultiplier;
            _substructionMultiplier = _overHeatSubstructionMultiplier;
            _overHeatSubstructionMultiplier = _tempMultiplier;
            //-----------
            //オーバーヒートのフラグをOFF
            _isOverHeating = false;
            //オバヒのアニメーションのトリガーをリセット
            _overHeatAnimator.ResetTrigger("Start");
            //オバヒのアニメーションのWaitを再生
            _overHeatAnimator.SetTrigger("Wait");
        }
    }
}
