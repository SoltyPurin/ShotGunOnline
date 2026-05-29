using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonMove : MonoBehaviour, ISelectHandler
{
    [SerializeField] GameObject sankakut;
    [SerializeField] GameObject sankakuf;
    public void OnClick()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("Title");
    }
    public void OnSelect(BaseEventData eventData)
    {
        sankakut.SetActive(true);
        sankakuf.SetActive(false);
    }
}
