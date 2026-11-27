using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ボタンにクールダウンを追加するスクリプト(通信が発生するボタンなどに)
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonCooldown : MonoBehaviour
{
    [SerializeField] private float _cooldownTime = 1f;

    private Button _button;
    private Coroutine _cooldownCoroutine;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(StartCooldown);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(StartCooldown);
        }
    }

    private void OnDisable()
    {
        if (_cooldownCoroutine != null)
        {
            StopCoroutine(_cooldownCoroutine);
            _cooldownCoroutine = null;
            if (_button != null)
            {
                _button.interactable = true;
            }
        }
    }

    public void StartCooldown()
    {
        if (!gameObject.activeInHierarchy || _button == null) return;
        if (_cooldownCoroutine != null)
        {
            StopCoroutine(_cooldownCoroutine);
        }
        _cooldownCoroutine = StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        bool wasSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        _button.interactable = false;
        float elapsed = 0f;
        while (elapsed < _cooldownTime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        _button.interactable = true;
        _cooldownCoroutine = null;

        if (wasSelected && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == null &&
            gameObject.activeInHierarchy && _button.IsInteractable())
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
