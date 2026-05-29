using UnityEngine;

public class BossBGMStart : MonoBehaviour
{
    private GameObject _bgmManager = default;
    private BGMControl_Ver2 _bgmControll = default;
    private void Start()
    {
        _bgmManager = GameObject.FindGameObjectWithTag("BGMManager");
        _bgmControll=_bgmManager.GetComponent<BGMControl_Ver2>();
        _bgmControll.BGM_Start(1);
    }
}
