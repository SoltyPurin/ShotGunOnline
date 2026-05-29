using UnityEngine;
using UnityEngine.Rendering;

//using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering.Universal;

public class BloomScript : MonoBehaviour
{
    //SoftKnee
    [SerializeField, Header("urp")]
    private Volume _urp = default;

    private Bloom _bloom = default;

    [SerializeField, Header("ブルームの強さ")]
    private float _strangthBloomValue = 1;

    private float _originStrangthBloomValue = default;
    [SerializeField, Header("ブルームを小さくする値")]
    private float _decBloomEffectValue = 0.05f;

    private bool _canSizeDownUltEffect = false;

    private Color32 _ultColor = new Color (0, 233, 255);

    private Color32 _criticalColor = new Color(255, 226, 0);

    private void Start()
    {
        _urp.profile.TryGet(out _bloom);
        _originStrangthBloomValue = _strangthBloomValue;
    }

    /// <summary>
    /// ウルト撃った時のチェレンコフ光を表示
    /// </summary>
    public void UseUltimate()
    {
        _bloom.clamp.value = 2;
        _bloom.tint.value = _ultColor;
        //_bloom.softKnee.value = _strangthBloomValue;
        _canSizeDownUltEffect = true;
    }

    public void UseCritical()
    {
        _bloom.clamp.value = 2;
        _bloom.tint.value = _criticalColor;
        _bloom.scatter.value = _strangthBloomValue;
        _canSizeDownUltEffect = true;
    }

    private void FixedUpdate()
    {
        if (_canSizeDownUltEffect)
        {
            _bloom.scatter.value = _strangthBloomValue;
            _strangthBloomValue -= _decBloomEffectValue;
            if(_strangthBloomValue <= 0)
            {
                _bloom.clamp.value = 0;
                _bloom.scatter.value = 0;
                _canSizeDownUltEffect = false;
            }
        }
        else
        {
            _strangthBloomValue = _originStrangthBloomValue;
        }
    }
}
