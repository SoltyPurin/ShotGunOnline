using UnityEngine;
using UnityEngine.SceneManagement;

public class MoveScene : MonoBehaviour
{
    [SerializeField] private string _title = "Title";
    [SerializeField] private string _nextScene = "TutorialScene";
    public void LoadTitle()
    {
        SceneManager.LoadScene(_title);
    }
    public void LoadHonpen()
    {
        SceneManager.LoadScene(_nextScene);
    }

    public void KonamiCommand()
    {
        _nextScene = "HPHonpen";
    }

    public void ReturnKonami()
    {
        _nextScene = "TutorialScene";
    }

}
