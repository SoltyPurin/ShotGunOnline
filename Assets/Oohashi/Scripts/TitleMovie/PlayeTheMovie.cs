using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class PlayeTheMovie : MonoBehaviour
{
    [SerializeField, Header("ムービープレイヤー")]
    private VideoPlayer _videoPlayer = default;
    [SerializeField,Header("フェードアウトさせるスクリプト")]
    private TitleFadeOut _titleFadeOut = default;

    private readonly string TITLE_NAME = "Title";
    private void Awake()
    {
        _videoPlayer.loopPointReached += LoopPointReached;
        _videoPlayer.Play();
    }

    private void LoopPointReached(VideoPlayer vp)
    {
        _titleFadeOut.CanFadeStart = true;
    }
}
