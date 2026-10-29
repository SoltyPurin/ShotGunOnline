using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyTakeDamage : NetworkBehaviour
{
    protected NetworkVariable<int> _enemyHP = new NetworkVariable<int>(
        1000, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> EnemyHP
    {
        get { return _enemyHP; }
    }

    protected NetworkVariable<int> _maxHP = new NetworkVariable<int>(
        1000,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> MaxHP
    {
        get { return _maxHP; }
    }

    /// <summary>
    /// 最大HPと現在HPをまとめて設定し同期する
    /// </summary>
    public virtual void SetMaxHP(int hp)
    {
        if (IsServer)
        {
            _enemyHP.Value = hp;
            _maxHP.Value = hp;
        }
        if (_hpUI != null)
        {
            _hpUI.Initialize(hp);
        }
    }

    [SerializeField, Header("ダメージに乗算する値")]
    protected int _damageMultiplier = 499;
    [SerializeField, Header("ノーチャージの時の最低ダメージ")]
    protected int _minDamage = 200;
    [SerializeField, Header("爆発の時の乗算値")]
    protected int _explosionMultiplier = 1000;
    [SerializeField, Header("死ぬまでのタイムラグ")]
    protected float _deathTimeMultiplier = 2.5f;
    [SerializeField, Header("移動のスクリプト")]
    protected EnemyMove _enemyMove = default;
    [SerializeField, Header("HPを表示するUI")]
    protected EnemyDamageUI _hpUI = default;
    private PlayerCoinDrop _playerCoinDrop = default;
    [SerializeField, Header("アニメーションのオブジェクト")]
    protected GameObject _deathObject = default;
    [SerializeField, Header("ダメージエフェクトのアニメーター")]
    protected Animator _damageAnimator = default;
    [Header("スプライトアニメ管理")]
    public DamageMaterial _damageMaterial = default;

    // プレイヤーのオブジェクト及びそれに付随するスクリプト達
    protected GameObject _playerObject = default;
    protected SoulKeep _coinKeep = default;
    private readonly string PLAYERTAGNAME = "Player";
    // SEのスクリプト
    protected SEManager _playTheSEManager = default;

    protected readonly string COMBOCOUNTER_TAGNAME = "ComboCounter";

    protected bool _isDead = false;
    public bool IsDead
    {
        get { return _isDead; }
    }

    // 直近で攻撃を与えたプレイヤー（ソウル吸い込み先特定用）
    protected GameObject _lastAttackerPlayer = null;

    public virtual void Start()
    {
        JsonSaver.Instance.LoadAllConfigs();
        GameObject seManager = GameObject.FindWithTag("SEManager");
        if (seManager != null)
        {
            _playTheSEManager = seManager.GetComponent<SEManager>();
        }
        _playerCoinDrop = GetComponent<PlayerCoinDrop>();
    }

    [Rpc(SendTo.Server)]
    public void RequestTakeDamageServerRpc(float chargeTime, PlayerState state, RpcParams rpcParams = default)
    {
        ResolveAttacker(rpcParams.Receive.SenderClientId);
        SetTakeDamege(chargeTime, state);
    }

    [Rpc(SendTo.Server)]
    public void RequestMirrorDamageServerRpc(float chargeTime, RpcParams rpcParams = default)
    {
        ResolveAttacker(rpcParams.Receive.SenderClientId);
        SetMirrorDamage(chargeTime);
    }

    private void ResolveAttacker(ulong senderClientId)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                _lastAttackerPlayer = client.PlayerObject.gameObject;
            }
        }
    }

    protected GameObject GetTargetPlayer()
    {
        if (_lastAttackerPlayer != null && _lastAttackerPlayer.activeInHierarchy)
        {
            return _lastAttackerPlayer;
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYERTAGNAME);
        if (players == null || players.Length == 0) return null;

        GameObject closest = null;
        float minDst = float.MaxValue;
        Vector3 curPos = transform.position;

        foreach (var p in players)
        {
            if (p == null || !p.activeInHierarchy) continue;
            float dst = Vector3.Distance(curPos, p.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                closest = p;
            }
        }
        return closest;
    }

    protected void AssurePlayerReference()
    {
        if (_playerObject != null && _coinKeep != null) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag(PLAYERTAGNAME);
        foreach (GameObject p in players)
        {
            var netObj = p.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null && netObj.IsLocalPlayer)
            {
                _playerObject = p;
                _coinKeep = p.GetComponent<SoulKeep>();
                break;
            }
        }
    }

    [ClientRpc]
    private void PlayDamageEffectsClientRpc()
    {
        if (_damageAnimator != null)
        {
            _damageAnimator.SetTrigger("Damage");
        }

        if (_damageMaterial != null)
        {
            _damageMaterial.Damage();
        }

        var animManager = GetComponentInChildren<EnemyAnimationManager>();
        if (animManager != null)
        {
            animManager.ForceKnockBack();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (_hpUI != null)
        {
            int initialMax = _maxHP.Value > 0 ? _maxHP.Value : _enemyHP.Value;
            _hpUI.Initialize(initialMax);
            _hpUI.UpdateHP(_enemyHP.Value);

            _enemyHP.OnValueChanged += OnHPChanged;
            _maxHP.OnValueChanged += OnMaxHPChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (_hpUI != null)
        {
            _enemyHP.OnValueChanged -= OnHPChanged;
            _maxHP.OnValueChanged -= OnMaxHPChanged;
        }
    }

    private void OnMaxHPChanged(int previousValue, int newValue)
    {
        if (_hpUI != null)
        {
            _hpUI.Initialize(newValue);
            _hpUI.UpdateHP(_enemyHP.Value);
        }
    }

    private void OnHPChanged(int previousValue, int newValue)
    {
        if (_hpUI != null)
        {
            _hpUI.UpdateHP(newValue);
        }
    }

    /// <summary>
    /// ダメージを喰らったときのメソッド、ウルトと通常で挙動が変わる
    /// </summary>
    public virtual void SetTakeDamege(float chargeTime, PlayerState state)
    {
        if (IsServer && !_isDead)
        {
            PlayDamageEffectsClientRpc();
            if (_enemyMove != null)
            {
                _enemyMove.EnemyState = EnemyState.knockback;
            }

            if (state == PlayerState.Ultimate)
            {
                _isDead = true;
                _enemyHP.Value = 0;
                if (_hpUI != null) _hpUI.UpdateHP(0);
                float delayInSeconds = 1.0f;
                float fakeCharge = delayInSeconds * _deathTimeMultiplier;
                StartCoroutine(DeathProtocol(fakeCharge));
            }
            else
            {
                float damage = chargeTime * _damageMultiplier;
                damage = Mathf.Max(damage, _minDamage);
                _enemyHP.Value = (_enemyHP.Value - (int)damage);
                if (_hpUI != null) _hpUI.UpdateHP(_enemyHP.Value);
                if (_playTheSEManager != null) _playTheSEManager.PlayEnemyDamageSound();

                if (_enemyHP.Value <= 0)
                {
                    _isDead = true;
                    StartCoroutine(DeathProtocol(chargeTime));
                }
            }
        }
    }

    /// <summary>
    /// 反射弾を喰らったときのダメージ
    /// </summary>
    public virtual void SetMirrorDamage(float chargeTime)
    {
        if (IsServer && !_isDead)
        {
            float damage = chargeTime * _damageMultiplier;
            damage = Mathf.Max(damage, _minDamage);
            _enemyHP.Value = (_enemyHP.Value - (int)damage);
            if (_hpUI != null) _hpUI.UpdateHP(_enemyHP.Value);
            if (_playTheSEManager != null) _playTheSEManager.PlayEnemyDamageSound();
            if (_enemyHP.Value <= 0)
            {
                _isDead = true;
                StartCoroutine(DeathProtocol(chargeTime));
            }
        }
    }

    /// <summary>
    /// 落下のダメージ、基本は一発で死ぬ
    /// </summary>
    public virtual void FallDamage()
    {
        if (IsServer)
        {
            if (_isDead) return;
            _isDead = true;
            if (_enemyMove != null) _enemyMove.EnemyState = EnemyState.fall;
            _enemyHP.Value = 0;
            if (gameObject.activeInHierarchy)
            {
                if (_playTheSEManager != null) _playTheSEManager.PlayDropSound();
                StartCoroutine(FallDeathProtocol());
            }
        }
    }

    /// <summary>
    /// 落下の演出及びコンボ加算
    /// </summary>
    private IEnumerator FallDeathProtocol()
    {
        if (IsServer)
        {
            if (!gameObject.scene.isLoaded) yield break;
            _isDead = true;

            GameObject counterObj = GameObject.FindWithTag(COMBOCOUNTER_TAGNAME);
            if (counterObj != null)
            {
                ComboCounter counter = counterObj.GetComponent<ComboCounter>();
                if (counter != null && gameObject.activeInHierarchy)
                {
                    counter.ComboPlus();
                }
            }

            yield return new WaitForSeconds(1.8f);

            PlayDeathEffectsClientRpc();

            if (_playerCoinDrop != null)
            {
                _playerCoinDrop.CoinDrop(1, GetTargetPlayer());
            }

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }

    /// <summary>
    /// 死亡処理を行うコルーチン
    /// </summary>
    public virtual IEnumerator DeathProtocol(float chargeTime)
    {
        if (!IsServer) yield break;
        if (!gameObject.scene.isLoaded) yield break;

        _isDead = true;

        if (_playerCoinDrop != null)
        {
            _playerCoinDrop.CoinDrop(1, GetTargetPlayer());
        }

        PlayDeathEffectsClientRpc();

        float timeLeftUntilDeath = chargeTime / _deathTimeMultiplier;
        yield return new WaitForSeconds(timeLeftUntilDeath);

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    /// <summary>
    /// 岩に接触したときのメソッド
    /// </summary>
    public virtual void ContactStoneMethod()
    {
        if (IsServer)
        {
            if (!gameObject.scene.isLoaded) return;

            _isDead = true;
            _enemyHP.Value = 0;

            PlayRoadKillEffectClientRpc();

            if (_playerCoinDrop != null)
            {
                _playerCoinDrop.CoinDrop(1, GetTargetPlayer());
            }

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }

    /// <summary>
    /// 全クライアント端末上で死亡音と死亡エフェクトを再生するRPC
    /// </summary>
    [ClientRpc]
    private void PlayDeathEffectsClientRpc()
    {
        if (_playTheSEManager != null)
        {
            _playTheSEManager.PlayEnemyDeathSound();
            _playTheSEManager.PlayMoneyDropSound();
        }

        if (_deathObject != null)
        {
            Instantiate(_deathObject, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// 全クライアント端末上で轢殺音と死亡エフェクトを再生するRPC
    /// </summary>
    [ClientRpc]
    private void PlayRoadKillEffectClientRpc()
    {
        if (_playTheSEManager != null)
        {
            _playTheSEManager.PlayRoadKill();
        }

        if (_deathObject != null)
        {
            Instantiate(_deathObject, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// 爆発のダメージ
    /// </summary>
    public virtual void SetExplosionDamgage(float chargeTime)
    {
        if (IsServer && !_isDead)
        {
            float damage = chargeTime * _explosionMultiplier;
            _enemyHP.Value = (_enemyHP.Value - (int)damage);
            if (_hpUI != null) _hpUI.UpdateHP(_enemyHP.Value);
            if (_enemyHP.Value <= 0)
            {
                _isDead = true;
                StartCoroutine(DeathProtocol(chargeTime));
            }
        }
    }
}
