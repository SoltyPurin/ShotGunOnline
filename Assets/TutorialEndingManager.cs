using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// チュートリアル終了時のムービーを管理するスクリプト
/// </summary>
public class TutorialEndingManager : MonoBehaviour
{
    #region 【変数】
    [Header("オーディオソース")]
    [SerializeField, Tooltip("シーン遷移の音を流す用")]
    private AudioSource _audioSource = default;
    [SerializeField, Tooltip("リバーブ&ディレイをかけたSource")]
    private AudioSource _audioSourceAmbience = default;
    [SerializeField, Tooltip("チャージ音に使われるオーディオソース")]
    private AudioSource _chargeAudioSource = default;

    [Header("オーディオクリップ")]
    [SerializeField, Tooltip("シーン遷移の音")]
    private AudioClip _audioClip = default;
    [SerializeField, Tooltip("着地の音")]
    private AudioClip _landingAudio = default;
    #endregion

    /// <summary>
    /// タイムライン側からタイムスケールを操作するためのメソッド
    /// </summary>
    /// <param name="time">タイムスケールの値（0～1）</param>
    public void TimeScaleChange(float time)
    {
        Time.timeScale = time;
    }

    /// <summary>
    /// タイムライン側から音を鳴らすためのメソッド
    /// </summary>
    public void SceneTransitionSound()
    {
        _audioSource.PlayOneShot(_audioClip);
    }

    /// <summary>
    /// タイムライン側から音を鳴らすためのメソッド
    /// </summary>
    public void LandingAudio()
    {
        _audioSourceAmbience.PlayOneShot(_landingAudio);
    }

    /// <summary>
    /// タイムライン側からシーンを変更させるメソッド
    /// </summary>
    public void ChangeScene()
    {
        SceneManager.LoadScene("Honpen");
    }

    public void StopChargeAudio()
    {
        _chargeAudioSource.loop = false;
        _chargeAudioSource.Stop();
    }
}
