using UnityEngine;
using UnityEngine.AI;

/// <summary>Drives the guide's locomotion, response gestures, and emotion lighting.</summary>
public sealed class AgentAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Renderer[] emotionRenderers;
    [SerializeField] private Light statusLight;
    [SerializeField] private NavMeshAgent navigationAgent;
    private Transform motionRoot;
    private Transform head;
    private Transform leftArm;
    private Transform rightArm;
    private Vector3 motionRootRestPosition;
    private Quaternion headRestRotation;
    private Quaternion leftArmRestRotation;
    private Quaternion rightArmRestRotation;
    private string activeAction = "idle";
    private float actionUntil;
    private float speakingUntil;
    private string activeClipState;
    private Vector3 previousPosition;

    private void Awake()
    {
        animator ??= GetComponentInChildren<Animator>();
        navigationAgent ??= GetComponent<NavMeshAgent>();
        if (animator != null) animator.applyRootMotion = false;
        previousPosition = transform.position;
        RuntimeAnimatorController sharedHumanoidController = Resources.Load<RuntimeAnimatorController>("LumenPeople/QuaterniusHumanoid");
        if (animator != null && animator.avatar != null && animator.avatar.isHuman && sharedHumanoidController != null)
            animator.runtimeAnimatorController = sharedHumanoidController;
        if (emotionRenderers == null || emotionRenderers.Length == 0)
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
            var modelRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer renderer in allRenderers)
                if (renderer != null && renderer.transform != transform) modelRenderers.Add(renderer);
            emotionRenderers = modelRenderers.Count > 0 ? modelRenderers.ToArray() : allRenderers;
        }
        if (statusLight == null) statusLight = GetComponentInChildren<Light>();

        if (emotionRenderers.Length > 0)
        {
            Transform candidate = emotionRenderers[0].transform;
            while (candidate.parent != null && candidate.parent != transform) candidate = candidate.parent;
            if (candidate != transform) motionRoot = candidate;
        }
        if (motionRoot != null) motionRootRestPosition = motionRoot.localPosition;
        head = FindNamedChild(transform, "head");
        leftArm = FindNamedChild(transform, "left_upperarm") ?? FindNamedChild(transform, "left_shoulder");
        rightArm = FindNamedChild(transform, "right_upperarm") ?? FindNamedChild(transform, "right_shoulder");
        if (head != null) headRestRotation = head.localRotation;
        if (leftArm != null) leftArmRestRotation = leftArm.localRotation;
        if (rightArm != null) rightArmRestRotation = rightArm.localRotation;
    }

    public void ApplyResponseTags(string action, string emotion)
    {
        ApplyEmotion(emotion);
        PlayAction(action);
    }

    public void ApplyEmotion(string emotion)
    {
        string normalized = string.IsNullOrWhiteSpace(emotion) ? "neutral" : emotion.Trim().ToLowerInvariant();
        Color colour = normalized switch
        {
            "excited" => new Color(0.25f, 0.95f, 0.65f),
            "concerned" => new Color(1f, 0.58f, 0.32f),
            "thinking" => new Color(0.42f, 0.66f, 1f),
            "confident" => new Color(0.78f, 0.45f, 1f),
            _ => new Color(0.30f, 0.88f, 1f)
        };
        if (statusLight != null) statusLight.color = colour;
    }

    public void PlayAction(string action)
    {
        activeAction = string.IsNullOrWhiteSpace(action) ? "idle" : action.Trim().ToLowerInvariant();
        actionUntil = Time.unscaledTime + (activeAction == "idle" || activeAction == "listen" ? 0.6f : 1.8f);
        if (animator == null || animator.runtimeAnimatorController == null) return;

        string trigger = activeAction switch
        {
            "nod" => "nod", "gesture" or "show_catalog" => "gesture", "point" or "show_model" => "point",
            "explain" => "explain", "encourage" => "encourage", _ => "idle"
        };
        if (HasParameter(trigger, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(trigger);
        else PlayMatchingAnimation(activeAction);
    }

    public void SpeakFor(float seconds)
    {
        speakingUntil = Time.unscaledTime + Mathf.Clamp(seconds, 0.8f, 18f);
        PlayAction("talk");
    }

    private void Update()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        float speed = 0f;
        if (navigationAgent != null && navigationAgent.enabled && navigationAgent.isOnNavMesh)
        {
            speed = navigationAgent.velocity.magnitude;
            if (HasParameter("Speed", AnimatorControllerParameterType.Float)) animator.SetFloat("Speed", speed);
            if (HasParameter("MotionSpeed", AnimatorControllerParameterType.Float))
                animator.SetFloat("MotionSpeed", Mathf.Clamp01(speed / Mathf.Max(0.1f, navigationAgent.speed)));
            if (HasParameter("Grounded", AnimatorControllerParameterType.Bool)) animator.SetBool("Grounded", true);
        }
        else if (Time.deltaTime > 0.0001f)
        {
            speed = Vector3.Distance(transform.position, previousPosition) / Time.deltaTime;
        }
        previousPosition = transform.position;
        if (!HasParameter("Speed", AnimatorControllerParameterType.Float) && Time.unscaledTime >= actionUntil && Time.unscaledTime >= speakingUntil)
            PlayMatchingAnimation(speed > 0.18f ? "walk" : "idle");
    }

    private void LateUpdate()
    {
        if (motionRoot == null) return;
        float wave = Mathf.Sin(Time.unscaledTime * 1.7f);
        motionRoot.localPosition = motionRootRestPosition + Vector3.up * (0.018f + wave * 0.018f);
        bool acting = Time.unscaledTime < actionUntil || Time.unscaledTime < speakingUntil;
        float pulse = acting ? Mathf.Sin((actionUntil - Time.unscaledTime) * 4.2f) : 0f;
        if (head != null)
        {
            float nod = activeAction == "nod" ? pulse * 10f : activeAction == "thinking" ? -5f : 0f;
            head.localRotation = headRestRotation * Quaternion.Euler(nod, 0f, 0f);
        }
        float gesture = acting && (activeAction == "talk" || activeAction == "gesture" || activeAction == "explain" || activeAction == "point" || activeAction == "show_model" || activeAction == "show_catalog" || activeAction == "encourage") ? 18f + pulse * 8f : 0f;
        if (leftArm != null) leftArm.localRotation = leftArmRestRotation * Quaternion.Euler(0f, 0f, gesture * 0.6f);
        if (rightArm != null) rightArm.localRotation = rightArmRestRotation * Quaternion.Euler(0f, 0f, -gesture);
    }

    private bool HasParameter(string name, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == name && parameter.type == type) return true;
        return false;
    }

    private void PlayMatchingAnimation(string action)
    {
        if (animator == null || animator.runtimeAnimatorController == null || animator.avatar == null || !animator.avatar.isHuman) return;
        string[] keywords = action switch
        {
            "walk" => new[] { "walk", "walking", "jog" },
            "talk" => new[] { "talk", "gesture", "wave", "idle" },
            "nod" => new[] { "nod", "agree", "idle" },
            "point" or "show_model" or "show_catalog" => new[] { "point", "show", "gesture", "wave", "idle" },
            "explain" or "encourage" or "gesture" => new[] { "talk", "gesture", "wave", "idle" },
            _ => new[] { "idle", "stand" }
        };
        foreach (string keyword in keywords)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null || clip.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) < 0 || activeClipState == clip.name) continue;
                activeClipState = clip.name;
                animator.CrossFadeInFixedTime(clip.name, 0.22f, 0, 0f);
                return;
            }
        }
    }

    private static Transform FindNamedChild(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }
}
