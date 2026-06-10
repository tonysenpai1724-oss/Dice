using System.Collections;
using UnityEngine;

public class DiceHand : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Animation legacyAnimation;
    [SerializeField] private string animationName = "dice";
    [SerializeField, Range(0f, 1f)] private float pauseNormalizedTime = 0.15f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool deactivateOnComplete;

    Coroutine routine;
    bool released;

    void Reset()
    {
        animator = GetComponent<Animator>();
        legacyAnimation = GetComponent<Animation>();
    }

    void OnEnable()
    {
        if (playOnEnable)
            Prepare();
    }

    void OnDisable()
    {
        StopRoutine();
    }

    public void Prepare()
    {

        released = false;
        gameObject.SetActive(true);
        StopRoutine();
        routine = StartCoroutine(PrepareRoutine());
    }

    public void Release()
    {
        if (released)
            return;

        released = true;
        StopRoutine();
        routine = StartCoroutine(ReleaseRoutine());
    }

    IEnumerator PrepareRoutine()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play(animationName, 0, 0f);
            yield return null;

            while (!released)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(animationName) && stateInfo.normalizedTime >= pauseNormalizedTime)
                    break;

                yield return null;
            }

            if (!released)
                animator.speed = 0f;

            routine = null;
            yield break;
        }

        if (legacyAnimation != null && legacyAnimation[animationName] != null)
        {
            AnimationState state = legacyAnimation[animationName];
            state.enabled = true;
            state.time = 0f;
            state.speed = 1f;
            legacyAnimation.Play(animationName);

            float pauseTime = state.length * pauseNormalizedTime;
            while (!released && state.time < pauseTime)
                yield return null;

            if (!released)
            {
                state.time = pauseTime;
                state.speed = 0f;
                legacyAnimation.Sample();
            }
        }

        routine = null;
    }

    IEnumerator ReleaseRoutine()
    {
        if (animator != null)
        {

            animator.speed = 1.5f;

            while (true)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log(stateInfo.normalizedTime + "state:" + stateInfo.IsName(animationName));
                if (stateInfo.IsName(animationName) && stateInfo.normalizedTime >= 1f)
                    break;

                yield return null;
            }

            CompleteRelease();
            yield break;
        }

        if (legacyAnimation != null && legacyAnimation[animationName] != null)
        {
            AnimationState state = legacyAnimation[animationName];
            state.speed = 1f;
            legacyAnimation.Play(animationName);

            while (state.time < state.length)
                yield return null;
        }

        CompleteRelease();
    }

    void CompleteRelease()
    {
        routine = null;

        if (deactivateOnComplete)
            gameObject.SetActive(false);
    }

    void StopRoutine()
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }
}
