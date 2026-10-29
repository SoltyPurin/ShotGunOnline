using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class SoulMeter : MonoBehaviour
{
    [SerializeField, Header("ソウルの液体のオブジェクト")]
    private GameObject _soulObject = default;
    private RectTransform _soulRect = default;
    [SerializeField, Header("液体の上がり幅")]
    private int _soulUpValue = 1;
    [SerializeField, Header("液体の発光イメージのアニメータ")]
    private Animator _soulFlashAnimator = default;

    [SerializeField, Header("ソウル取得時のエフェクト")]
    private Animator _getSoulEffect = default;

    [Header("ソウルMAX時のUIとエフェクト")]
    [SerializeField] private GameObject _soulMaxUI = default;
    [SerializeField] private GameObject _soulMaxEffect = default;


    private int _maxCeil = 0;
    private const int SOUL_MAX_VALUE = 9;

    private int _soulFirstPos = -780;
    private int _upValue = 0;

    private void Start()
    {
        _maxCeil = _soulFirstPos;
        _upValue = (_soulFirstPos * -1) / SOUL_MAX_VALUE;
        _soulRect = _soulObject.GetComponent<RectTransform>();
        _bottleMax.SetActive(false);
    }

    [SerializeField] private List<Image> _vstockList;
    [SerializeField] private List<GameObject> _vstockFlash;
    [SerializeField] private GameObject _bottleMax;
    [SerializeField] private Text _soulCountText;
    [SerializeField] private Sprite _assignSoulImage;
    [SerializeField] private Sprite _unAssignSoulImage;
    [SerializeField] private Color _activeColor;
    [SerializeField] private Color _hideColor;


    public void SetCount(int count)
    {
        _soulCountText.text = count.ToString();
    }


    public void UpdateVStockDisplay(int vstock)
    {
        for (int i = 0; i < _vstockList.Count; i++)
        {
            bool isActive = i < vstock;
            _vstockList[i].color = isActive ? _activeColor : _hideColor;
            _vstockList[i].sprite = isActive ? _assignSoulImage : _unAssignSoulImage;
            _vstockFlash[i].SetActive(isActive);
        }

        ShowSoulMax(vstock);

        _bottleMax.SetActive(vstock >= _vstockList.Count);
    }

    private void Update()
    {
        Vector2 soulPos = _soulObject.transform.localPosition;

        if ((soulPos.y <= _maxCeil))
        {
            soulPos.y += _soulUpValue;
            _soulRect.localPosition = soulPos;
        }

    }
    /// <summary>
    /// メーターを上げるメソッド
    /// </summary>
    /// <param name="count">現在の所有ソウルを渡す</param>
    public void AddMeter(int count)
    {
        //_maxCeil = _soulFirstPos + count * _upValue;
        //_soulFlashAnimator.SetTrigger("Flash");
        //_getSoulEffect.SetTrigger("Get");
        _maxCeil = _soulFirstPos + count * _upValue;

        Vector2 soulPos = _soulRect.localPosition;

        // ソウルが減って現在の高さを超えてしまった場合は、即座に目標位置へ下げる
        if (soulPos.y > _maxCeil)
        {
            soulPos.y = _maxCeil;
            _soulRect.localPosition = soulPos;
        }
        else
        {
            // 増えた時のみ演出を再生
            if (_soulFlashAnimator != null) _soulFlashAnimator.SetTrigger("Flash");
            if (_getSoulEffect != null) _getSoulEffect.SetTrigger("Get");
        }
    }


    /// <summary>
    /// メーターをリセットする
    /// </summary>
    public void ResetMeter()
    {
        Vector2 soulPos = _soulObject.transform.localPosition;
        soulPos.y = _soulFirstPos;
        _soulRect.localPosition = soulPos;
        _maxCeil = _soulFirstPos;
    }

    private void ShowSoulMax(int vstock)
    {
        if (vstock > 0)
        {
            _soulMaxUI.SetActive(true);
            _soulMaxEffect.SetActive(true);
        }
        else
        {
            _soulMaxUI.SetActive(false);
            _soulMaxEffect.SetActive(false);
        }
    }



}
