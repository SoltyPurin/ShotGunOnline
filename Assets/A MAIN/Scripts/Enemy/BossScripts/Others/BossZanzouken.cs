using UnityEngine;

/// <summary>
/// パンチのエフェクトを生成するメソッド
/// </summary>
public class BossZanzouken : MonoBehaviour
{
    #region[変数名]
    //---GameObject,Script,Animator等---------------------------------
    [SerializeField, Header("生成する残像")]
    private GameObject _bossGrow = default;
    [SerializeField, Header("生成した残像リスト")]
    private GameObject[] _grows = new GameObject[4];
    [SerializeField, Header("パンチのエフェクト")]
    private GameObject _effect = default;
    [SerializeField, Header("生成する残像カラー")]
    private Color[] _growsColors = new Color[4];

    //---int,floatなどの数値---------------------------------
    [SerializeField, Header("残像がボス自身に戻っていく速度")]
    private float _returnSpeed = 0;
    [SerializeField, Header("残像がフェードアウトする速度")]
    private float _fadeSpeed = 0;

    #endregion

    public void InstantiateGrowByIndex(int index)
    {
        if (index < 0 || index >= _grows.Length)
        {
            return;
        }
        _grows[index] = Instantiate(_bossGrow, transform.position, transform.rotation);
        _grows[index].GetComponent<SpriteRenderer>().color = _growsColors[index];
    }

    public void MoveGrow()
    {
        for (int i = 0; i < _grows.Length; i++)
        {
            if (_grows[i] != null)
            {
                _grows[i].transform.position = Vector3.Lerp(_grows[i].transform.position,
                    transform.position,
                    _returnSpeed * Time.fixedDeltaTime);
            }
        }
    }

    public void DelGrow()
    {
        for (int i = 0; i < _grows.Length; i++)
        {
            if (_grows[i] != null)
            {
                Destroy(_grows[i]);
                _grows[i] = null; // 参照をクリア
            }
        }
        _effect.SetActive(false);
    }

    public void PunchEffect()
    {
        _effect.SetActive(true);
    }

}
