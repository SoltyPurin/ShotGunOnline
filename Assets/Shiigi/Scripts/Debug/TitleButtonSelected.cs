using UnityEngine;
using UnityEngine.EventSystems;

public class TitleButtonSelected : MonoBehaviour
{
    private float _count = 0;
    private const float LOG_INTERVAL = 0.5f;

    private void Update() {
        _count += Time.deltaTime;

        if (_count >= LOG_INTERVAL)
        {
            Debug.Log($"selected: {EventSystem.current.currentSelectedGameObject.gameObject.name}");
            _count = 0;
        }
    }
}
