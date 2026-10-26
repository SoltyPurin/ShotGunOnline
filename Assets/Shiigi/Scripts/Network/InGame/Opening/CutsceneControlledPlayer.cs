using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// 開始ムービー中にプレイヤーを単一のTimeline再生時間に合わせて自動制御するコンポーネント (SRP: パターンB)。
/// Playerプレハブのルートに付与します。
/// 
/// 【タイムライン（12.5秒）に同期する自動演出】
/// ・0.0s 〜 8.667s: 倒れ状態（各スキン固有のDownスプライト）、銃(Burrel)/照準線/UIを非表示
/// ・8.0s 〜 8.6s: 震え演出（PlayerSpritesが小刻みに横揺れ）
/// ・8.667s 〜 9.167s: 起き上がり演出（KnockBackスプライト、銃が出現し垂直ホップ）
/// ・9.167s 〜 12.5s: 立ち状態（Idleスプライト）
/// ・12.5s（終了時）: 照準線/UI解禁、Animator再開、物理/同期復帰、シームレスに操作開始
/// </summary>
[DisallowMultipleComponent]
public class CutsceneControlledPlayer : MonoBehaviour
{
    [Serializable]
    public struct CutsceneSkinSprites
    {
        [Tooltip("スロットまたはスキンIDに対応する倒れスプライト")]
        public Sprite DownSprite;
        [Tooltip("起き上がりホップ時のスプライト")]
        public Sprite KnockBackSprite;
        [Tooltip("立ち・待機スプライト")]
        public Sprite IdleSprite;
    }

    [Header("スキン別スプライト設定 (0:P1, 1:P2, 2:P3, 3:P4)")]
    [SerializeField] private CutsceneSkinSprites[] _skinSprites;

    [Header("自動制御対象（未指定時はAwakeで自動検索）")]
    [SerializeField] private Transform _playerSpritesTransform;
    [SerializeField] private GameObject _burrelObject;
    [SerializeField] private GameObject _lineRendererObject;
    [SerializeField] private GameObject _playerUIObject;
    [SerializeField] private SpriteRenderer _frontSpriteRenderer;
    [SerializeField] private Animator _frontSpriteAnimator;

    private NetworkObject _networkObject;
    private NetworkTransform[] _networkTransforms;
    private Rigidbody2D _rigidbody2D;
    private Rigidbody _rigidbody;

    private bool[] _networkTransformEnabledCache;
    private bool _rigidbody2DSimulatedCache;
    private bool _rigidbodyKinematicCache;

    private Vector3 _playerSpritesOriginalLocalPos;
    private Vector3 _burrelOriginalLocalPos;
    private bool _cachedOriginalPositions;

    private int _assignedSlotIndex = 0;
    private Sprite _originalFrontSprite;

    public bool IsInCutscene { get; private set; }
    public ulong OwnerClientId => _networkObject != null ? _networkObject.OwnerClientId : 0;

    [Tooltip("Timeline の Animation Track に割り当てる Animator（ルートの Transform を動かすもの）。未設定ならルートの Animator を使用し、無ければ自動で追加します")]
    [SerializeField] private Animator _cutsceneAnimator;

    /// <summary>
    /// Timeline の枠トラックに割り当てる Animator
    /// </summary>
    public Animator CutsceneAnimator
    {
        get
        {
            if (_cutsceneAnimator == null)
            {
                _cutsceneAnimator = GetComponent<Animator>();
                if (_cutsceneAnimator == null)
                {
                    _cutsceneAnimator = gameObject.AddComponent<Animator>();
                }
            }
            return _cutsceneAnimator;
        }
    }

    private void Awake()
    {
        _networkObject = GetComponent<NetworkObject>();
        _networkTransforms = GetComponentsInChildren<NetworkTransform>(true);
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _rigidbody = GetComponent<Rigidbody>();

        ResolveChildReferences();
        CacheOriginalPositions();
    }

    private void ResolveChildReferences()
    {
        if (_playerSpritesTransform == null)
        {
            var t = transform.Find("PlayerSprites");
            if (t != null) _playerSpritesTransform = t;
        }

        if (_frontSpriteRenderer == null)
        {
            var frontObj = transform.Find("PlayerSprites/SpriteManager/FrontSprite");
            if (frontObj != null)
            {
                _frontSpriteRenderer = frontObj.GetComponent<SpriteRenderer>();
                _frontSpriteAnimator = frontObj.GetComponent<Animator>();
            }
        }

        if (_burrelObject == null)
        {
            var burrel = transform.Find("Burrel");
            if (burrel != null) _burrelObject = burrel.gameObject;
        }

        if (_lineRendererObject == null)
        {
            var lr = transform.Find("Burrel/GunBurrel/LineLenderer");
            if (lr != null)
            {
                _lineRendererObject = lr.gameObject;
            }
            else
            {
                var lineComp = GetComponentInChildren<LineRenderer>(true);
                if (lineComp != null) _lineRendererObject = lineComp.gameObject;
            }
        }

        if (_playerUIObject == null)
        {
            var ui = transform.Find("PlayerUI");
            if (ui != null) _playerUIObject = ui.gameObject;
        }
    }

    private void CacheOriginalPositions()
    {
        if (_cachedOriginalPositions) return;

        if (_playerSpritesTransform != null)
        {
            _playerSpritesOriginalLocalPos = _playerSpritesTransform.localPosition;
        }

        if (_burrelObject != null)
        {
            _burrelOriginalLocalPos = _burrelObject.transform.localPosition;
        }

        if (_frontSpriteRenderer != null)
        {
            _originalFrontSprite = _frontSpriteRenderer.sprite;
        }

        _cachedOriginalPositions = true;
    }

    /// <summary>
    /// ムービー制御状態に入る（物理・ネット同期・アニメーターを一時停止し、初期フレームを適用）
    /// </summary>
    /// <param name="slotIndex">プレイヤー枠番号 (0..3)</param>
    public void EnterCutscene(int slotIndex = 0)
    {
        if (IsInCutscene) return;
        IsInCutscene = true;
        _assignedSlotIndex = slotIndex;

        ResolveChildReferences();
        CacheOriginalPositions();

        // 物理演算停止
        if (_rigidbody2D != null)
        {
            _rigidbody2DSimulatedCache = _rigidbody2D.simulated;
            _rigidbody2D.linearVelocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
            _rigidbody2D.simulated = false;
        }

        if (_rigidbody != null)
        {
            _rigidbodyKinematicCache = _rigidbody.isKinematic;
            if (!_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            _rigidbody.isKinematic = true;
        }

        // 非オーナー側の NetworkTransform 適用停止
        _networkTransformEnabledCache = new bool[_networkTransforms.Length];
        for (int i = 0; i < _networkTransforms.Length; i++)
        {
            var nt = _networkTransforms[i];
            _networkTransformEnabledCache[i] = nt.enabled;
            if (!nt.CanCommitToTransform)
            {
                nt.enabled = false;
            }
        }

        // FrontSpriteのAnimatorを無効化（ムービー中のスプライト切り替えとの競合を防ぐ）
        if (_frontSpriteAnimator != null)
        {
            _frontSpriteAnimator.enabled = false;
        }

        // 先頭フレーム（倒れ状態）を即座に反映
        UpdateCutscenePose(0f);
    }

    /// <summary>
    /// タイムライン再生時間（director.time）に合わせて倒れ・震え・起き上がり演出を適用する
    /// </summary>
    /// <param name="timelineTime">タイムラインの現在時刻(秒)</param>
    public void UpdateCutscenePose(float timelineTime)
    {
        if (!IsInCutscene) return;

        var skin = GetCurrentSkinSprites();

        const float shakeStartTime = 8.0f;
        const float wakeUpStartTime = 8.6667f;
        const float standUpTime = 9.1667f;
        const float uiActivationTime = 12.4833f;

        if (timelineTime < wakeUpStartTime)
        {
            // === 倒れフェーズ ===
            if (_burrelObject != null && _burrelObject.activeSelf) _burrelObject.SetActive(false);
            if (_lineRendererObject != null && _lineRendererObject.activeSelf) _lineRendererObject.SetActive(false);
            if (_playerUIObject != null && _playerUIObject.activeSelf) _playerUIObject.SetActive(false);

            if (_frontSpriteRenderer != null && skin.DownSprite != null)
            {
                _frontSpriteRenderer.sprite = skin.DownSprite;
            }

            if (timelineTime >= shakeStartTime && _playerSpritesTransform != null)
            {
                // 震え演出: ±0.05 の周期揺れ
                float shakeOffset = Mathf.Sin((timelineTime - shakeStartTime) * Mathf.PI * 30f) * 0.05f;
                _playerSpritesTransform.localPosition = _playerSpritesOriginalLocalPos + new Vector3(shakeOffset, 0f, 0f);
            }
            else if (_playerSpritesTransform != null)
            {
                _playerSpritesTransform.localPosition = _playerSpritesOriginalLocalPos;
            }
        }
        else if (timelineTime < standUpTime)
        {
            // === 起き上がりホップフェーズ ===
            if (_burrelObject != null && !_burrelObject.activeSelf) _burrelObject.SetActive(true);
            if (_lineRendererObject != null && _lineRendererObject.activeSelf) _lineRendererObject.SetActive(false);
            if (_playerUIObject != null && _playerUIObject.activeSelf) _playerUIObject.SetActive(false);

            if (_frontSpriteRenderer != null && skin.KnockBackSprite != null)
            {
                _frontSpriteRenderer.sprite = skin.KnockBackSprite;
            }

            float hopProgress = Mathf.Clamp01((timelineTime - wakeUpStartTime) / (standUpTime - wakeUpStartTime));
            float hopY = Mathf.Sin(hopProgress * Mathf.PI) * 0.5f;

            if (_playerSpritesTransform != null)
            {
                _playerSpritesTransform.localPosition = _playerSpritesOriginalLocalPos + new Vector3(0f, hopY, 0f);
            }

            if (_burrelObject != null)
            {
                _burrelObject.transform.localPosition = _burrelOriginalLocalPos + new Vector3(0f, hopY, 0f);
            }
        }
        else
        {
            // === 立ち・準備完了フェーズ ===
            if (_burrelObject != null && !_burrelObject.activeSelf) _burrelObject.SetActive(true);
            if (_playerSpritesTransform != null) _playerSpritesTransform.localPosition = _playerSpritesOriginalLocalPos;
            if (_burrelObject != null) _burrelObject.transform.localPosition = _burrelOriginalLocalPos;

            if (_frontSpriteRenderer != null && skin.IdleSprite != null)
            {
                _frontSpriteRenderer.sprite = skin.IdleSprite;
            }

            bool enableUI = timelineTime >= uiActivationTime;
            if (_lineRendererObject != null && _lineRendererObject.activeSelf != enableUI) _lineRendererObject.SetActive(enableUI);
            if (_playerUIObject != null && _playerUIObject.activeSelf != enableUI) _playerUIObject.SetActive(enableUI);
        }
    }

    /// <summary>
    /// ムービー制御状態を抜ける（物理・ネット同期・アニメーターを復旧し、最終位置を確定）
    /// </summary>
    public void ExitCutscene()
    {
        if (!IsInCutscene) return;
        IsInCutscene = false;

        // 全オブジェクトの表示と座標を復旧
        if (_burrelObject != null)
        {
            _burrelObject.SetActive(true);
            _burrelObject.transform.localPosition = _burrelOriginalLocalPos;
        }

        if (_lineRendererObject != null) _lineRendererObject.SetActive(true);
        if (_playerUIObject != null) _playerUIObject.SetActive(true);

        if (_playerSpritesTransform != null)
        {
            _playerSpritesTransform.localPosition = _playerSpritesOriginalLocalPos;
        }

        // Animator を再開（PlayerAnimation / PlayerSkinSync へ制御を戻す）
        if (_frontSpriteAnimator != null)
        {
            _frontSpriteAnimator.enabled = true;
        }

        // NetworkTransform を復旧
        for (int i = 0; i < _networkTransforms.Length; i++)
        {
            if (_networkTransformEnabledCache != null && i < _networkTransformEnabledCache.Length)
            {
                _networkTransforms[i].enabled = _networkTransformEnabledCache[i];
            }
            else
            {
                _networkTransforms[i].enabled = true;
            }
        }

        // 物理演算復旧
        if (_rigidbody2D != null)
        {
            _rigidbody2D.simulated = _rigidbody2DSimulatedCache;
            _rigidbody2D.linearVelocity = Vector2.zero;
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = _rigidbodyKinematicCache;
        }

        // オーナー端末が最終位置へ Teleport して全端末の位置を同期確定
        foreach (var nt in _networkTransforms)
        {
            if (nt != null && nt.enabled && nt.IsSpawned && nt.CanCommitToTransform)
            {
                var t = nt.transform;
                var pos = nt.InLocalSpace ? t.localPosition : t.position;
                var rot = nt.InLocalSpace ? t.localRotation : t.rotation;
                nt.Teleport(pos, rot, t.localScale);
            }
        }
    }

    private CutsceneSkinSprites GetCurrentSkinSprites()
    {
        if (_skinSprites != null && _skinSprites.Length > 0)
        {
            int index = Mathf.Clamp(_assignedSlotIndex, 0, _skinSprites.Length - 1);
            return _skinSprites[index];
        }

        return new CutsceneSkinSprites
        {
            DownSprite = _originalFrontSprite,
            KnockBackSprite = _originalFrontSprite,
            IdleSprite = _originalFrontSprite
        };
    }

    private void OnDisable()
    {
        ExitCutscene();
    }
}
