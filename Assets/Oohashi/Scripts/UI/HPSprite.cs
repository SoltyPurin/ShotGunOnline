using UnityEngine;
using UnityEngine.SceneManagement;

public class HPSprite : MonoBehaviour
{
    [SerializeField, Header("HPのスプライトを登録する配列")]
    private GameObject[] _hpSprites = new GameObject[5];

    private int _initHPValue = 0;

    private SaveHardOptionSetting _hardOption = default;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "HPHonpen")
        {
            return;
        }
        if (_hardOption != null)
        {
            _initHPValue = 5;
            for (int i = 0; i <= _initHPValue-1; i++)
            {
                _hpSprites[i].SetActive(true);
            }
        }
        else
        {
            _initHPValue = SaveHardOptionSetting._heartValue;
            for (int i = 0; i <= _initHPValue - 1; i++)
            {
                _hpSprites[i].SetActive(true);
            }
        }
    }

    public void ReduceHP()
    {
        if(SceneManager.GetActiveScene().name == "HPHonpen")
        {
            _hpSprites[_initHPValue-1].SetActive(false);
            _initHPValue--;
        }
    }
}
