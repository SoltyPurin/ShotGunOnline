using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseButton : MonoBehaviour
{
    private InputPause _input = default;

    public static string SceneName = "Title";

    private readonly string CUSHION_SCENE = "Cushion";

    private void Start()
    {
        _input = GetComponent<InputPause>();
    }
    public void Resume()
    {
        _input.Resume();
    }

    public void ReStart()
    {
        Time.timeScale = 1.0f;
        SceneName = SceneManager.GetActiveScene().name;
        if (Application.CanStreamedLevelBeLoaded(CUSHION_SCENE))
        {
            SceneManager.LoadScene(CUSHION_SCENE);
        }
        else
        {
            SceneManager.LoadScene(SceneName);
        }
    }

    public void ReturnTitle()
    {
        Time.timeScale = 1.0f;
        SceneName = "Title";
        if (Application.CanStreamedLevelBeLoaded(CUSHION_SCENE))
        {
            SceneManager.LoadScene(CUSHION_SCENE);
        }
        else
        {
            SceneManager.LoadScene("Title");
        }
    }
}