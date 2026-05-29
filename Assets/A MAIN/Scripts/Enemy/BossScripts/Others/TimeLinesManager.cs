using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeLinesManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _popTimeLine = default;
    public void OnPopTimeLine()
    {
        _popTimeLine.SetActive(true);
    }
}
