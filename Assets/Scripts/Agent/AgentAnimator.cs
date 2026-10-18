using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Retargeted locomotion and conversational gestures; navigation owns the root transform.</summary>
public sealed class AgentAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Renderer[] emotionRenderers;
    [SerializeField] private Light statusLight;
    [SerializeField] private NavMeshAgent navigationAgent;
    private readonly Dictionary<string, int> states = new Dictionary<string, int>();
    private Transform head;
    private Transform viewer;
    private CharacterFacialAnimation face;
    private Quaternion headRest;
    private Vector3 previousPosition;
    private string activeAction = "idle";
    private float actionUntil;
    private float speakingUntil;
    private float nextViewerLookup;
    private float speed;
    private int activeState;
    private bool hasSpeed;
    private bool hasMotionSpeed;
    private bool hasGrounded;
    private bool listening;
    private bool human;
    private AudioSource speech;
    private AmbientNpcRoutine ambientRoutine;

    private void Awake() { RefreshVisual(); }

    public void RefreshVisual()
    {
        Transform visual=transform.Find("LumenGuideVisual")??transform.Find("NpcAssetVisual")??transform.Find("CitizenVisual");
        animator = visual!=null?visual.GetComponentInChildren<Animator>():GetComponentInChildren<Animator>();
        ambientRoutine=GetComponent<AmbientNpcRoutine>();
        navigationAgent = GetComponent<NavMeshAgent>();
        previousPosition = transform.position;
        states.Clear();
        activeState = 0;
        head = null;
        human = animator != null && animator.avatar != null && animator.avatar.isHuman && animator.avatar.isValid;
        hasSpeed = hasMotionSpeed = hasGrounded = false;
        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (human)
            {
                RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("LumenPeople/QuaterniusHumanoid");
                if (controller != null) animator.runtimeAnimatorController = controller;
                head = animator.GetBoneTransform(HumanBodyBones.Head);
            }
            if (animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.name == "Speed" && parameter.type == AnimatorControllerParameterType.Float) hasSpeed = true;
                    if (parameter.name == "MotionSpeed" && parameter.type == AnimatorControllerParameterType.Float) hasMotionSpeed = true;
                    if (parameter.name == "Grounded" && parameter.type == AnimatorControllerParameterType.Bool) hasGrounded = true;
                }
                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip == null) continue;
                    string name = clip.name.ToLowerInvariant();
                    int hash = Animator.StringToHash(clip.name);
                    if (name.EndsWith("|idle_loop")) states["idle"] = hash;
                    else if (name.EndsWith("|walk_loop")) states["walk"] = hash;
                    else if (name.EndsWith("|idle_talking_loop")) states["talk"] = hash;
                    else if (name.EndsWith("|interact")) states["point"] = hash;
                }
            }
        }
        if (head == null)
            foreach (Transform item in GetComponentsInChildren<Transform>())
                if (item.name.Equals("head", System.StringComparison.OrdinalIgnoreCase)) { head = item; break; }
        if (head != null) headRest = head.localRotation;
        if (statusLight == null) statusLight = GetComponentInChildren<Light>();
        face = GetComponent<CharacterFacialAnimation>();
        if (face == null) face = gameObject.AddComponent<CharacterFacialAnimation>();
        face.RefreshVisual();
    }

    public void ApplyResponseTags(string action, string emotion)
    {
        ApplyEmotion(emotion);
        PlayAction(action);
    }

    public void ApplyEmotion(string emotion)
    {
        if (face != null) face.SetEmotion(emotion);
        if (statusLight == null) return;
        statusLight.color = (emotion ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "excited" => new Color(0.25f, 0.95f, 0.65f),
            "concerned" => new Color(1f, 0.58f, 0.32f),
            "thinking" => new Color(0.42f, 0.66f, 1f),
            "confident" => new Color(0.78f, 0.45f, 1f),
            _ => new Color(0.30f, 0.88f, 1f)
        };
    }

    public void PlayAction(string action)
    {
        activeAction = string.IsNullOrWhiteSpace(action) ? "idle" : action.Trim().ToLowerInvariant();
        actionUntil = Time.unscaledTime + (activeAction == "idle" || activeAction == "listen" ? 0.6f : 2.2f);
    }

    public void SetListening(bool value) { listening = value; }

    public void SpeakFor(float seconds)
    {
        speakingUntil = Time.unscaledTime + Mathf.Clamp(seconds, 0.8f, 90f);
        if(ambientRoutine!=null&&ambientRoutine.enabled)ambientRoutine.ReactToConversation(seconds);
    }

    public void BeginSpeech(AudioSource source,System.Func<float> nativeLevel=null)
    {
        speech = source;
        speakingUntil = 0f;
        listening = false;
        if (face != null) face.BeginSpeech(source,nativeLevel);
        if(ambientRoutine!=null&&ambientRoutine.enabled)ambientRoutine.ReactToConversation(source!=null&&source.clip!=null?source.clip.length:4.2f);
    }

    public void EndSpeech()
    {
        speech = null;
        speakingUntil = 0f;
        if (face != null) face.EndSpeech();
    }

    private void Update()
    {
        if (navigationAgent == null) navigationAgent = GetComponent<NavMeshAgent>();
        Vector3 delta = transform.position - previousPosition;
        delta.y = 0f;
        float measuredSpeed = navigationAgent != null && navigationAgent.enabled && navigationAgent.isOnNavMesh
            ? navigationAgent.velocity.magnitude : Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
        previousPosition = transform.position;
        speed = Mathf.Lerp(speed, Mathf.Min(measuredSpeed, 6f), 1f - Mathf.Exp(-Time.deltaTime * 10f));
        if(ambientRoutine!=null&&ambientRoutine.enabled){if(speech!=null&&speech.isPlaying)ambientRoutine.HoldConversation();return;} // Routine owns body clips; this component owns speech/face/gaze.
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (hasSpeed) animator.SetFloat("Speed", speed);
        if (hasMotionSpeed) animator.SetFloat("MotionSpeed", Mathf.Clamp01(speed / 1.4f));
        if (hasGrounded) animator.SetBool("Grounded", true);
        if (hasSpeed) return;

        bool talking = speech != null ? speech.isPlaying : Time.unscaledTime < speakingUntil;
        bool gesturing = Time.unscaledTime < actionUntil && activeAction != "idle" && activeAction != "listen";
        string desired = speed > 0.16f ? "walk" : talking || gesturing ? "talk" : "idle";
        if (speed <= 0.16f && Time.unscaledTime < actionUntil &&
            (activeAction == "point" || activeAction == "show_model" || activeAction == "show_catalog")) desired = "point";
        if (!states.TryGetValue(desired, out int state) && !states.TryGetValue("idle", out state)) return;
        if (state == activeState || !animator.HasState(0, state)) return;
        activeState = state;
        animator.CrossFadeInFixedTime(state, 0.24f, 0, 0f);
    }

    private void LateUpdate()
    {
        if (head == null) return;
        if (viewer == null && Time.unscaledTime >= nextViewerLookup)
        {
            nextViewerLookup = Time.unscaledTime + 1f;
            if (Camera.main != null) viewer = Camera.main.transform;
        }
        bool talking = speech != null ? speech.isPlaying : Time.unscaledTime < speakingUntil;
        float nod = Time.unscaledTime < actionUntil && activeAction == "nod" ? Mathf.Sin(Time.unscaledTime * 7f) * 5f : talking ? Mathf.Sin(Time.unscaledTime * 2.4f) * 1.3f : 0f;
        float yaw = 0f;
        float pitch = 0f;
        if (viewer != null && speed < 0.18f)
        {
            Vector3 direction = transform.InverseTransformDirection(viewer.position - head.position);
            float distance = direction.magnitude;
            if (distance < 6f && direction.z > 0f)
            {
                yaw = Mathf.Clamp(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, -28f, 28f) * (listening ? 0.9f : 0.65f);
                pitch = -Mathf.Clamp(Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg, -15f, 15f);
            }
        }
        // Humanoid Animator writes a fresh base pose each frame; add small head motion without flattening limb animation.
        if(!human||animator==null||!animator.enabled||animator.runtimeAnimatorController==null)head.localRotation=headRest;
        // Source head bones have different local axes. Apply gaze around the
        // companion's world axes rather than rolling a source-specific bone.
        head.rotation=Quaternion.AngleAxis(yaw,transform.up)*Quaternion.AngleAxis(pitch+nod,transform.right)*
            Quaternion.AngleAxis(listening?2f:0f,transform.forward)*head.rotation;
    }

    private void OnDisable() { EndSpeech(); }
}
