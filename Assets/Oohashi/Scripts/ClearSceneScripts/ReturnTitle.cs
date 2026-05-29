using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnTitle : MonoBehaviour
{
    public void ReturnTitleButton()
    {
        SceneManager.LoadScene("Title");
    }
}
