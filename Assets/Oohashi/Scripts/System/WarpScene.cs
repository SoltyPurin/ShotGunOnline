using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WarpScene : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadSceneAsync(PauseButton.SceneName);
        StartCoroutine(DestroyThisScene());
    }

    private IEnumerator DestroyThisScene()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name);
    }

}
