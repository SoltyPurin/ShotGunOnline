using NUnit.Framework.Api;
using Unity.Netcode;
using UnityEngine;

public class PlayerSkinSync : NetworkBehaviour
{
    [SerializeField, Header("正面方向の置き換え対象のアニメーター")]
    private Animator _flontPlayerAnimator = default;

    [SerializeField, Header("後ろ向きの置き換え対象のアニメーター")]
    private Animator _backPlayerAnimator = default;

    [SerializeField, Header("キャラごとの正面オーバーライド(0: P1用, 1: P2用 ...)")]
    private AnimatorOverrideController[] _frontSkinOverrideControllers;
    [SerializeField,Header("キャラごとの背後オーバーライド(0: P1用, 1: P2用 ...)")]
    private AnimatorOverrideController[] _backSkinOverrideControllers;

    private readonly NetworkVariable<int> _skinId = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private PlayerNumberSave _numberSave = default;

    public override void OnNetworkSpawn()
    {
        // 1. 値が変更・同期されたときに全端末で見た目を更新するイベント登録
        _skinId.OnValueChanged += OnSkinIdChanged;

        // 2. サーバー（ホスト）側で、プレイヤーの接続IDに応じてスキンIDを決定する
        if (IsServer)
        {
            // 例: Host (ClientId:0) -> 0番目のスキン, Client (ClientId:1) -> 1番目のスキン
            int assignedSkinId = (int)(OwnerClientId % (ulong)_frontSkinOverrideControllers.Length);
            _skinId.Value = assignedSkinId;
        }
        _numberSave = this.GetComponent<PlayerNumberSave>();

        // 3. スポーンした時点の見た目を適用
        ApplySkin(_skinId.Value);


    }

    private void OnSkinIdChanged(int previousValue, int newValue)
    {
        ApplySkin(newValue);
    }

    /// <summary>
    /// Animator の Controller を差し替えて見た目を適用する
    /// </summary>
    private void ApplySkin(int skinId)
    {
        if (_frontSkinOverrideControllers == null || _frontSkinOverrideControllers.Length == 0) return;
        if (skinId < 0 || skinId >= _frontSkinOverrideControllers.Length) return;

        if (_flontPlayerAnimator != null && _frontSkinOverrideControllers[skinId] != null)
        {
            if (_backPlayerAnimator != null)
            {
                _flontPlayerAnimator.runtimeAnimatorController = _frontSkinOverrideControllers[skinId];
                _backPlayerAnimator.runtimeAnimatorController = _backSkinOverrideControllers[skinId];
                _numberSave.SetPlayerNumber(skinId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        // イベントの登録解除（メモリリーク防止）
        _skinId.OnValueChanged -= OnSkinIdChanged;
    }
}
