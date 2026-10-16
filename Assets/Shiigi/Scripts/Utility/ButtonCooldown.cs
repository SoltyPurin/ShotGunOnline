using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボタンにクールダウンを追加するスクリプト(通信が発生するボタンなどに)
/// </summary>
public class ButtonCooldown : MonoBehaviour
{
    [SerializeField] private float _cooldownTime = 1f;

    private Button _button;

    private void Start()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(StartCooldown);
    }

    public void StartCooldown()
    {
        StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        _button.interactable = false;
        yield return new WaitForSeconds(_cooldownTime);
        _button.interactable = true;
    }
}
