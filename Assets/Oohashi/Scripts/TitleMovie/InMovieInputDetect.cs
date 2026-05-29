using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InMovieInputDetect : MonoBehaviour
{
    private readonly string TITLE_NAME = "Title";

    private void Update()
    {
        if (IsInputDetected())
        {
            SceneManager.LoadScene(TITLE_NAME);
        }
    }

    bool IsInputDetected()
    {
        // キー入力
        if (Input.anyKey) return true;

        // スティック入力
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f) return true;

        return false;
    }

}
