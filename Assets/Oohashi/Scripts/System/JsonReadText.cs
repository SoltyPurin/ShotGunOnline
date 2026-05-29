using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JsonReadText : MonoBehaviour
{
    private Text _jsonText = default;
    [SerializeField, Header("Jsonのテキストを消すまでの時間")]
    private float _deleteTextTime = 3;

    public void ShowReadJson()
    {
        _jsonText = GetComponent<Text>();
        if(_jsonText != null)
        {
            _jsonText.text = "Json読み込み完了！";
            StartCoroutine(DisableJasonReadText());
        }
    }

    private IEnumerator DisableJasonReadText()
    {
        yield return new WaitForSecondsRealtime(_deleteTextTime);
        _jsonText.text = " ";
    }
}
