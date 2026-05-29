using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateWavesTexts : MonoBehaviour
{
    [SerializeField, Header("複製するウェーブのオブジェクト")]
    private GameObject _waveObj = default;
    [SerializeField, Header("前の文字からどれくらい下げるか")]
    private float _verticalOffset = 10;

    [SerializeField, Header("生成する文字の親")]
    private Transform _parent = default;

    private List<GameObject> _wavesObjects = new List<GameObject>();

    public void CreateWaves(int waves, string[] scoreData)
    {
        Debug.Log("各ウェーブスコア生成");
        for(int i = 1;  i < waves+1; i++)
        {
            GameObject waveText = Instantiate(_waveObj, transform.position, Quaternion.identity,_parent);
            Vector3 textPos = waveText.transform.position;
            textPos.y -= _verticalOffset * i;
            waveText.transform.position = textPos;
            Text text = waveText.GetComponent<Text>();
            if(scoreData[i-1] != null)
            {
                text.text += "Wave " + i + ": " + scoreData[i - 1];
            }
            else
            {
                text.text += "Wave " + i + ": test";
            }
            _wavesObjects.Add(waveText);
            _wavesObjects[i - 1].SetActive(false);
        }
    }


    /// <summary>
    /// 配列に登録された各ウェーブごとのスコアを表示
    /// </summary>
    public void ShowWaveScores()
    {
        for(int i = 0; i < _wavesObjects.Count; i++)
        {
            _wavesObjects [i].SetActive(true);
        }
    }

    /// <summary>
    /// 配列に登録された各ウェーブのスコアを非表示にする
    /// </summary>
    public void HideWaveScores()
    {
        for (int i = 0; i < _wavesObjects.Count; i++)
        {
            _wavesObjects[i].SetActive(false);
        }
    }
}
