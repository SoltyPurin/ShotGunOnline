using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

/// <summary>
/// 非同期で結果（TResult）を返すモーダルダイアログの基底クラス。
/// 背面ブロッカー制御と結果待機のライフサイクルを管理する。
/// </summary>
public abstract class LobbyModalDialogBase<TResult> : LobbyViewBase
{
    private UniTaskCompletionSource<TResult> _utcs;
    private GameObject _blocker;

    /// <summary>現在ダイアログが結果入力待ち状態かどうか</summary>
    protected bool IsAwaitingResult => _utcs != null;

    /// <summary>現在モーダルダイアログとして入力を受け付け可能か</summary>
    protected bool CanProcessModalInput =>
        _utcs != null &&
        gameObject.activeInHierarchy &&
        IsTopActiveView &&
        (_canvasGroup == null || _canvasGroup.interactable);

    protected override void Awake()
    {
        _viewType = UIViewType.ModalDialog;
        base.Awake();
    }

    protected override void OnDestroy()
    {
        _utcs?.TrySetCanceled();
        _utcs = null;
        base.OnDestroy();
    }

    /// <summary>
    /// 背面操作ブロック用の ScreenBlocker を確保・最背面配置する
    /// </summary>
    protected void EnsureModalBlocker(Color blockerColor)
    {
        if (_blocker == null)
        {
            _blocker = UIBlockerHelper.EnsureBlocker(transform, blockerColor);
        }
        else
        {
            _blocker.SetActive(true);
            _blocker.transform.SetAsFirstSibling();
        }
    }

    protected void HideModalBlocker()
    {
        if (_blocker != null)
        {
            _blocker.SetActive(false);
        }
    }

    /// <summary>
    /// 新しい結果待機タスクを開始する（前回の未完了待機があれば安全にキャンセルする）
    /// </summary>
    protected UniTaskCompletionSource<TResult> BeginResultWait()
    {
        _utcs?.TrySetCanceled();
        _utcs = new UniTaskCompletionSource<TResult>();
        return _utcs;
    }

    /// <summary>
    /// 表示アニメーションを再生する（表示演出途中でキャンセルされてもダイアログ結果待ちは継続する）
    /// </summary>
    protected async UniTask ShowModalAnimatedSafelyAsync(Action onAfterShowStarted = null)
    {
        try
        {
            var showTask = base.ShowAsync();
            onAfterShowStarted?.Invoke();
            await showTask;
        }
        catch (OperationCanceledException)
        {
            // 表示演出のキャンセルは無視し、結果の確定を待つ
        }
    }

    /// <summary>
    /// ダイアログを非表示にして結果（result）を呼び出し元へ確定通知する
    /// </summary>
    protected async UniTaskVoid CloseModalWithResultAsync(TResult result, Action onBeforeHide = null)
    {
        if (_utcs == null) return;
        var tcs = _utcs;
        _utcs = null;

        onBeforeHide?.Invoke();
        HideModalBlocker();

        try
        {
            await HideAsync();
        }
        catch (OperationCanceledException)
        {
            // 非表示演出がキャンセル・破棄された場合でもデッドロック防止のため結果は必ず確定させる
        }
        finally
        {
            tcs.TrySetResult(result);
        }
    }
}
