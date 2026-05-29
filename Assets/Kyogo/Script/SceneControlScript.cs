using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneControlScript : MonoBehaviour
{
    [SerializeField,Header("RetryCursor")]
    GameObject RetryCursor = default;
    [SerializeField,Header("TitleCursor")]
    GameObject TitleCursor = default;
    bool buttonMove = default;

    private void Update()
    {
        if (buttonMove)
        {
            RetryCursor.SetActive(true);
            TitleCursor.SetActive(false);
            buttonMove = !buttonMove;
        }
        else
        {
            RetryCursor.SetActive(false);
            TitleCursor.SetActive(true);
            buttonMove = !buttonMove;
        }
    }
}
