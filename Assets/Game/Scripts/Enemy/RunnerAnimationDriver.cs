using UnityEngine;

namespace EchoesOfTheFog.Enemy
{
    [RequireComponent(typeof(RunnerBrain))]
    public sealed class RunnerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float attackClipLength1 = 1, attackClipLength2 = 1;
        private RunnerBrain brain;
        private void Awake()
        {
            brain = GetComponent<RunnerBrain>();
            if (animator == null || animator.runtimeAnimatorController == null)
            { Debug.LogError("RunnerAnimationDriver needs its Animator and controller.", this); enabled = false; }
        }
        private void OnEnable() { if (brain != null) { brain.StateChanged += OnState; brain.AttackStarted += OnAttack; } }
        private void OnDisable() { if (brain != null) { brain.StateChanged -= OnState; brain.AttackStarted -= OnAttack; } }
        private void Update()
        {
            animator.SetFloat("Speed", brain.MovementSpeed, 0.1f, Time.deltaTime);
            animator.SetBool("Dead", brain.State == RunnerState.Dead);
        }
        private void OnState(RunnerState state)
        {
            if (state == RunnerState.HitReact || state == RunnerState.Dead) animator.ResetTrigger("Attack");
            if (state == RunnerState.HitReact) animator.SetTrigger("Hit");
            if (state == RunnerState.Dead) { animator.ResetTrigger("Hit"); animator.SetBool("Dead", true); }
        }
        private void OnAttack(int index)
        {
            animator.SetInteger("AttackIndex", index);
            animator.SetFloat("AttackPlayback", (index == 0 ? attackClipLength1 : attackClipLength2) / brain.Tuning.AttackDuration(index));
            animator.SetTrigger("Attack");
        }
    }
}
