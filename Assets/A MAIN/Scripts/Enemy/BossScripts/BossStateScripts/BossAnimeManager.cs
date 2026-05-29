using UnityEngine;
/// <summary>
/// ボスのAnimation管理用のメソッド
/// </summary>
public class BossAnimeManager : MonoBehaviour
{
    [SerializeField] private Animator _bossAnimator;

    public void PlayWalk() => _bossAnimator.SetBool("IsWalk", true);
    public void StopWalk() => _bossAnimator.SetBool("IsWalk", false);
    public void PlayJumpAtack() => _bossAnimator.SetTrigger("IsFall");
    public void PlayMachPunch() => _bossAnimator.SetTrigger("IsPunch");
    public void ResetAllTriggers()
    {
        _bossAnimator.ResetTrigger("IsFall");
        _bossAnimator.ResetTrigger("IsSpecialMove");
        _bossAnimator.ResetTrigger("IsPunch");
    }
}
