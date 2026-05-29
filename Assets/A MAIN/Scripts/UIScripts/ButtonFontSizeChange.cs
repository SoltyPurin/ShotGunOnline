using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonFontSizeChange : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField]
    private Text buttonText;

    private void Start()
    {
        buttonText.color = Color.white;
    }

    public void OnSelect(BaseEventData eventData)
    {
        buttonText.color = Color.red;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        buttonText.color=Color.white;
    }
}
