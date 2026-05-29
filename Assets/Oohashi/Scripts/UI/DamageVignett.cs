using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering.Universal;

public class DamageVignett : MonoBehaviour
{
    [SerializeField, Header("URP")]
    private Volume _urp = default;

    private Vignette _damageVignette = default;

    [SerializeField, Header("ダメージエフェクトの大きさ")]
    private float _sizeOfDamageVignette = 0.5f;

    [SerializeField, Header("エフェクトを小さくする値")]
    private float _decDamageVignettValue = 0.05f;

    [SerializeField, Header("プレイヤーのHP")]
    private PlayerHP _hp = default;

    //通常の暗いビネット
    private Color32 _darkVignette = new Color(118, 118, 118);

    //ダメージを食らった時のビネット
    private Color32 _damageVignetteColor = new Color32(255, 178, 167,255);

    //最初のビネットの大きさを保存する変数
    private float _firstVignette = 0;

    //ビネットを小さくしてもよいか
    private bool _canSizeDownVignette = false;

    private void Start()
    {
        _firstVignette = _sizeOfDamageVignette;
        _urp.profile.TryGet(out _damageVignette);
        _damageVignette.color.value = Color.gray;
        _damageVignette.intensity.value = 0.6f;

    }

    /// <summary>
    /// ダメージを喰らったときにビネットを表示する
    /// </summary>
    public void Damage()
    {
        //元の処理、しっくりこなかったら戻す
        _damageVignette.color.value = Color.red;
        _damageVignette.intensity.value = _sizeOfDamageVignette;
        _canSizeDownVignette = true;
        _hp.TakeDamage();
    }

    private void FixedUpdate()
    {
        //元の処理、しっくりこなかったら戻す
        if (_canSizeDownVignette)
        {
            //_damageVignette.intensity.value = _sizeOfDamageVignette;
            _sizeOfDamageVignette -= _decDamageVignettValue;
            if (_sizeOfDamageVignette <= 0)
            {
                _canSizeDownVignette = false;
                _sizeOfDamageVignette = _firstVignette;
            }
        }
        else
        {
            _damageVignette.color.value = Color.gray;
            _damageVignette.intensity.value = 0.6f;
        }

    }
}
