using UnityEngine;
using UnityEngine.UI;

public class LoadingOverlayController : MonoBehaviour
{
    [SerializeField] private Text _loadingContext;

    public void SetLoadingState(bool isLoading)
    {
        gameObject.SetActive(isLoading);
    }

    public void SetLoadingState(bool isLoading, string context)
    {
        _loadingContext.text = context;
        gameObject.SetActive(isLoading);
    }
}
