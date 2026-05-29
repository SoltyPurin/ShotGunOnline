using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;


public class RetryBottunMove : MonoBehaviour, ISelectHandler
{

    [SerializeField] GameObject sankaku;
    [SerializeField] GameObject sankaku2;


    public void OnClick()
    {
        SceneManager.LoadScene("Honpen");
    }
    public void OnSelect(BaseEventData eventData)
    {
        sankaku.SetActive(true);
        sankaku2.SetActive(false);
    }
}
