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
        _maxCeil = _soulFirstPos + count * _upValue;
        _soulFlashAnimator.SetTrigger("Flash");
        _getSoulEffect.SetTrigger("Get");
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



    //[SerializeField, Header("ボトル前面の発光イメージ")]
    //private GameObject _bottleFlash = default;

    //[SerializeField, Header("ボトルマックス時の発光イメージ")]
    //private GameObject _bottleMax = default;

    //[SerializeField, Header("ウルトがない状態の見た目")]
    //private Sprite _unAssignSoulImage = default;

    //[SerializeField,Header("ウルトが貯まった時の見た目")]
    //private Sprite _assignSoulImage = default;

    //[SerializeField, Header("所持ソウル表示テキスト")]
    //private Text _soulCountText = default;

    //[SerializeField,Header("Vstockのリスト")]
    //private List<Image> _vstockList = new List<Image>();

    //[SerializeField,Header("Vstockの発光アニメータ")]
    //private List<GameObject> _vstockFlash = new List<GameObject>();

    ////現在アクティブになってるVストックのインデックス
    ////private int _initActiveVstockIndex = 0;


    //private int _soulMaxPos = 60;



    //private readonly string PLAYER_TAGNAME = "Player";

    ////ストックの透明度変更用変数
    //private readonly Color _activeColor = new Color(1f, 1f, 1f, 1f);
    //private readonly Color _hideColor = new Color(1f, 1f, 1f, 0.2f);



    ///// <summary>
    ///// vストックを光らせる、もし2つ光ってたら瓶の中身をマックスにする
    ///// </summary>
    ///// <param name="value">vストックの数</param>
    //public void ActiveVStock(int value)
    //{
    //    for (int i = 0; i < _vstockList.Count; i++)
    //    {
    //        bool isActive = i < value;
    //        _vstockList[i].color = isActive ? _activeColor : _hideColor;
    //        _vstockList[i].sprite = isActive ? _assignSoulImage : _unAssignSoulImage;
    //        _vstockFlash[i].SetActive(isActive);
    //    }
    //    //Debug.Log("ActiveVStock Called with index: " + _initActiveVstockIndex);
    //    //if (_initActiveVstockIndex < 2)
    //    //{
    //    //    _vstockList[_initActiveVstockIndex].color = _activeColor;
    //    //    _vstockList[_initActiveVstockIndex].sprite = _assignSoulImage;
    //    //    _vstockFlash[_initActiveVstockIndex].SetActive(true);
    //    //}
    //    //else
    //    //{
    //    //    _maxCeil = _soulMaxPos;
    //    //    _bottleMax.SetActive(true);
    //    //}
    //    //_initActiveVstockIndex++;
    //}


    ///// <summary>
    ///// vストックを消費する、もし瓶の中身がマックスだったら先にそっちから消費する
    ///// </summary>
    //public void DeActiveVStock()
    //{
    //    _initActiveVstockIndex--;
    //    if (_initActiveVstockIndex < 0)
    //    {
    //        _initActiveVstockIndex = 0;
    //        Debug.LogWarning("Vストックがもう存在しません！");
    //        return;
    //    }

    //    if ( _initActiveVstockIndex < 2)
    //    {
    //        _vstockList[_initActiveVstockIndex].color = _hideColor;
    //        _vstockList[_initActiveVstockIndex].sprite = _unAssignSoulImage;
    //        _vstockFlash[_initActiveVstockIndex].SetActive(false);
    //    }
    //    else
    //    {
    //        Vector2 soulPos = _soulObject.transform.localPosition;
    //        soulPos.y = _soulFirstPos;
    //        _soulRect.localPosition = soulPos;
    //        _maxCeil = _soulFirstPos;
    //        _bottleMax.SetActive(false);

    //    }
    //}

    //public void UpdateVStockDisplay(int vstock)
    //{
    //    for (int i = 0; i < _vstockList.Count; i++)
    //    {
    //        bool isActive = i < vstock;
    //        _vstockList[i].color = isActive ? _activeColor : _hideColor;
    //        _vstockList[i].sprite = isActive ? _assignSoulImage : _unAssignSoulImage;
    //        _vstockFlash[i].SetActive(isActive);
    //    }

    //    // 瓶の液体処理なども必要に応じてここで調整
    //    if (vstock >= _vstockList.Count)
    //    {
    //        _bottleMax.SetActive(true);
    //    }
    //    else
    //    {
    //        _bottleMax.SetActive(false);
    //    }
    //}


    ///// <summary>
    ///// メーターのカウントテキストを設定するメソッド
    ///// </summary>
    ///// <param name="count">現在所持しているソウル数</param>
    //public void SetCount(int count)
    //{
    //    _soulCountText.text = count.ToString();
    //}
}
