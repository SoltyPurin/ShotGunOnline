using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMControl_Ver2 : MonoBehaviour
{
    //====================内部データ部分========================================
    //BGMのプレイリストを取得
    [SerializeField, Header("BGMプレイリスト")]
    private AudioClip[] bgmPlayList;

    //オーディオソースを取得
    [SerializeField, Header("オーディオソース")]
    private AudioSource audioSource;

    [Header("BGMの音量")]
    public float _bgmVolume = 1f;

    AudioLowPassFilter lpFilter;
    /*
    ハイカット用変数
    --------------------タイマー--------------------
    HiCut_InTime_Timer          ハイカットにかける時間を測るタイマー
    HiCut_InTime_TimerSwitch    上のタイマーのON・OFFを管理するbool変数
    HiCut_InTime                ハイカットを入れて目標値に到達する間の時間
    HiCut_TargetFrequency       ハイカットの目標値
    HiCut_StartFrequency        ハイカットを始めたときの開始値（通常はStartで取得）
    */
    float HiCut_InTime_Timer = 0f;
    bool HiCut_InTime_TimerSwitch = false;
    float HiCut_InTime = default;
    float HiCut_TargetFrequency = default;
    float HiCut_StartFrequency = default;

    /*
    フェード用変数
    --------------------タイマー--------------------
    Fade_InTime_Timer          ハイカットにかける時間を測るタイマー
    Fade_InTime_TimerSwitch    上のタイマーのON・OFFを管理するbool変数
    Fade_InTime                ハイカットを入れて目標値に到達する間の時間
    Fade_TargetFrequency       ハイカットの目標値
    Fade_StartFrequency        ハイカットを始めたときの開始値（通常はStartで取得）
    */
    float Fade_InTime_Timer = 0f;
    bool Fade_InTime_TimerSwitch = false;
    float Fade_InTime = default;
    float Fade_TargetFrequency = default;
    float Fade_StartFrequency = default;

    //====================実行部分========================================
    void Start()
    {
        lpFilter = GetComponent<AudioLowPassFilter>();
        //オーディオソースが存在するかの存在確認
        if (audioSource == null)
        {
            Debug.LogWarning("audioSource1が設定していません");
            return;
        }
        // AudioLowPassFilter を取得
        if (lpFilter == null)
        {
            Debug.LogWarning("AudioLowPassFilter が見つかりませんでした");
            return;
        }

        audioSource.volume = _bgmVolume;

        //初期化
        lpFilter.cutoffFrequency = 22000f;
    }

    void Update()
    {
        //ハイカットかける用タイマー
        if (HiCut_InTime_TimerSwitch)
        {
            //っタイマーに今の経過時間を加算する
            HiCut_InTime_Timer += Time.deltaTime;

            //
            lpFilter.cutoffFrequency = Mathf.Lerp(HiCut_StartFrequency, HiCut_TargetFrequency, HiCut_InTime_Timer / HiCut_InTime);

            if (HiCut_InTime_Timer > HiCut_InTime)
            {
                HiCut_InTime_TimerSwitch = false;//タイマーをオフにして初期化する
            }
        }
        else
        {
            HiCut_InTime_Timer = 0f;//タイマー初期化
        }

        //フェードかける用タイマー
        if (Fade_InTime_TimerSwitch)
        {
            Fade_InTime_Timer += Time.deltaTime;//時間を測る
            audioSource.volume = Mathf.Lerp(Fade_StartFrequency, Fade_TargetFrequency, Fade_InTime_Timer / Fade_InTime);

            if (Fade_InTime_Timer > Fade_InTime)
            {
                Fade_InTime_TimerSwitch = false;//タイマーをオフにして初期化する
            }
        }
        else
        {
            Fade_InTime_Timer = 0f;//タイマー初期化
        }
    }

    //====================BGMスタート、ストップを管理========================================
    //BGMスタート
    public void BGM_Start(int BGMNumber)
    {
        //test
        audioSource.clip = bgmPlayList[BGMNumber];
        audioSource.Play();
    }
    //BGMストップ
    public void BGM_Stop()
    {
        audioSource.Stop();
    }

    public void BGM_Pause()
    {
        audioSource.Pause();
    }

    public void BGM_UnPause()
    {
        audioSource.UnPause();
    }

    //====================フェードイン、アウトを管理========================================
    //フェードインを管理
    public void BGMFade_In(float inTime, float targetVolume)
    {
        Fade_InTime = inTime;
        Fade_StartFrequency = audioSource.volume;
        Fade_TargetFrequency = targetVolume;
        Fade_InTime_Timer = 0f;
        Fade_InTime_TimerSwitch = true;
    }

    public void BGMFade_Out(float outTime, float targetVolume)
    {
        Fade_InTime = outTime;
        Fade_StartFrequency = audioSource.volume;
        Fade_TargetFrequency = targetVolume;
        Fade_InTime_Timer = 0f;
        Fade_InTime_TimerSwitch = true;
    }


    //====================エフェクター、フィルターを管理========================================
    //待機時間にハイカットを実行する
    /*
    変数内容
    Input_HiCut_TargetFrequency 目標のハイカット周波数
    HiCut_StartFrequency        ハイカットを始めたときの開始値（通常はStartで取得）
    HiCut_InTime                ハイカットにかける時間の受け渡し
    */
    public void HiCutOn(float Input_HiCut_TargetFrequency, float Input_HiCut_InTime)
    {
        HiCut_TargetFrequency = Input_HiCut_TargetFrequency;//目標数値を入力する
        HiCut_StartFrequency = lpFilter.cutoffFrequency;//今のハイカットの周波数を取得してくる
        HiCut_InTime = Input_HiCut_InTime; //ハイカットにかける時間の受け渡し
        HiCut_InTime_TimerSwitch = true;//タイマーを起動(実行)
    }
    public void HiCutOff(float Input_HiCut_TargetFrequency, float Input_HiCut_InTime)//HiCutOnを使いまわし
    {
        HiCutOn(Input_HiCut_TargetFrequency, Input_HiCut_InTime);
    }
}