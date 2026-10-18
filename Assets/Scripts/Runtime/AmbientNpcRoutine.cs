using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Small believable idle, conversation, work and wander loops for ambient people.</summary>
public sealed class AmbientNpcRoutine : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private Transform head;
    private Transform leftArm;
    private Transform rightArm;
    private Transform leftLeg;
    private Transform rightLeg;
    private Quaternion headRest;
    private Quaternion leftArmRest;
    private Quaternion rightArmRest;
    private Quaternion leftLegRest;
    private Quaternion rightLegRest;
    private System.Random random;
    private Vector3 home;
    private float wanderRadius;
    private float nextDecision;
    private float nextNavMeshRecovery;
    private float actionUntil;
    private float phase;
    private bool isCafeGuest;
    private bool isBarista;
    private bool canWander;
    private bool allowSeated;
    private string activeClip;
    private string activity = "idle";

    public void Configure(string role, bool wander, float radius, int seed)
    {
        string normalized = (role ?? string.Empty).ToLowerInvariant();
        isCafeGuest = normalized.Contains("guest") || normalized.Contains("citizen");
        isBarista = normalized.Contains("barista") || normalized.Contains("seller");
        canWander = wander;
        allowSeated = normalized.Contains("cafe guest") && !wander;
        wanderRadius = Mathf.Max(0.6f, radius);
        random = new System.Random(seed);
        home = transform.position;
        animator = GetComponentInChildren<Animator>();
        KeepAnimationOnVisualRoot();
        agent = GetComponent<NavMeshAgent>();
        CacheBones();
        TryRegisterOnNavMesh();
        nextDecision = Time.time + RandomRange(0.5f, 2.5f);
    }

    public void ReactToConversation(float seconds=4.2f)
    {
        activity = "talk";
        actionUntil = Mathf.Max(actionUntil,Time.time+Mathf.Clamp(seconds,.8f,90f));
        nextDecision = actionUntil;
        if(agent!=null&&agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;
        TryPlay(isBarista ? new[] { "talk", "wave", "gesture", "idle" } : new[] { "talk", "wave", "gesture", "sit", "idle" });
        Transform player = FindAnyObjectByType<CafeWalkController>()?.transform;
        if (player != null) StartCoroutine(FacePlayerBriefly(player));
    }

    public void HoldConversation(){if(activity=="talk"){actionUntil=Mathf.Max(actionUntil,Time.time+.3f);nextDecision=actionUntil;}}

    public void ReactToOrder(bool ready = false)
    {
        activity = ready ? "serve" : "work";
        actionUntil = Time.time + (ready ? 3.2f : 5f);
        nextDecision = actionUntil;
        TryPlay(ready ? new[] { "serve", "work", "gesture", "talk", "idle" } : new[] { "work", "serve", "gesture", "talk", "idle" });
    }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        KeepAnimationOnVisualRoot();
        agent = GetComponent<NavMeshAgent>();
        home = transform.position;
        random ??= new System.Random(gameObject.name.GetHashCode() ^ transform.position.GetHashCode());
        CacheBones();
    }

    private void Update()
    {
        phase += Time.deltaTime;
        if(agent!=null&&agent.enabled&&agent.isOnNavMesh&&agent.isStopped&&Time.time>=actionUntil)agent.isStopped=false;
        if (canWander && agent != null && agent.enabled && !agent.isOnNavMesh && Time.time >= nextNavMeshRecovery)
        {
            nextNavMeshRecovery = Time.time + 0.75f;
            TryRegisterOnNavMesh();
        }
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (agent.velocity.sqrMagnitude > 0.05f) TryPlay(new[] { "walk", "walk forward", "idle" });
            else if (Time.time >= nextDecision && Time.time >= actionUntil) ChooseActivity();
        }
        else if (Time.time >= nextDecision && Time.time >= actionUntil) ChooseActivity();

        AnimateProceduralFallback();
    }

    private void ChooseActivity()
    {
        nextDecision = Time.time + RandomRange(3.4f, 7.2f);
        if (isCafeGuest)
        {
            int choice = random.Next(0, 4);
            if (choice == 0) { activity = "eat"; TryPlay(new[] { "eat", "drink", "sit", "idle" }); }
            else if (choice == 1) { activity = "look"; TryPlay(new[] { "look", "sit", "idle" }); }
            else if (choice == 2) { activity = "sit"; TryPlay(new[] { "sit", "look", "idle" }); }
            else { activity = "idle"; TryPlay(new[] { "idle", "sit" }); }
            actionUntil = Time.time + RandomRange(2f, 3.4f);
        }
        else if (isBarista)
        {
            int choice = random.Next(0, 3);
            activity = choice == 0 ? "work" : "idle";
            TryPlay(choice == 0 ? new[] { "work", "serve", "idle" } : choice == 1 ? new[] { "look", "idle" } : new[] { "idle", "work" });
            actionUntil = Time.time + RandomRange(2f, 3.2f);
            if (canWander && random.NextDouble() < 0.3) WalkToNearbyPoint();
        }
        else
        {
            int choice = random.Next(0, 3);
            if (choice == 0 && canWander && WalkToNearbyPoint()) return;
            activity = choice == 1 ? "look" : "idle";
            TryPlay(choice == 1 ? new[] { "look", "idle" } : new[] { "idle", "look" });
            actionUntil = Time.time + RandomRange(2f, 3f);
        }
    }

    private bool WalkToNearbyPoint()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return false;
        Vector2 offset = UnityEngine.Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = home + new Vector3(offset.x, 0f, offset.y);
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.8f, NavMesh.AllAreas)) return false;
        if (Vector3.Distance(home, hit.position) > wanderRadius + 1.8f) return false;
        TryPlay(new[] { "walk", "walk forward", "idle" });
        return agent.SetDestination(hit.position);
    }

    private void TryRegisterOnNavMesh()
    {
        if (!canWander) return;
        if (agent != null)
        {
            if (agent.enabled && !agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit existingHit, 4f, agent.areaMask))
                agent.Warp(existingHit.position);
            return;
        }
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.2f, NavMesh.AllAreas)) return;
        agent = gameObject.AddComponent<NavMeshAgent>();
        agent.radius = 0.25f;
        agent.height = 1.7f;
        agent.baseOffset = 0f;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        agent.speed = RandomRange(0.95f, 1.4f);
        agent.acceleration = 6f;
        agent.angularSpeed = 300f;
        agent.stoppingDistance = 0.22f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
        agent.Warp(hit.position);
    }

    private void TryPlay(string[] keywords)
    {
        if (animator == null || animator.runtimeAnimatorController == null || animator.avatar == null || !animator.avatar.isHuman) return;
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (string keyword in keywords)
        {
            AnimationClip bestClip = null;
            int bestScore = -1;

            foreach (AnimationClip clip in clips)
            {
                if (clip == null) continue;
                string lower = clip.name.ToLowerInvariant();
                // Never play swimming or combat animations during ambient routine
                if (lower.Contains("swim") || lower.Contains("death") || lower.Contains("pistol") || lower.Contains("sword") || lower.Contains("spell"))
                    continue;
                if(!allowSeated&&(lower.Contains("sit")||lower.Contains("crouch")))continue;

                int score = -1;
                if (lower.EndsWith(keyword + "_loop") || lower.EndsWith("|" + keyword + "_loop"))
                    score = 100;
                else if (lower.EndsWith(keyword) || lower.EndsWith("|" + keyword))
                    score = 90;
                else if (lower.Contains(keyword) && lower.Contains("loop"))
                    score = 70;
                else if (lower.Contains(keyword) && !lower.Contains("enter") && !lower.Contains("exit"))
                    score = 50;
                else if (lower.Contains(keyword))
                    score = 20;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestClip = clip;
                }
            }

            if (bestClip == null) continue;
            if (activeClip == bestClip.name) return;
            activeClip = bestClip.name;
            animator.CrossFadeInFixedTime(bestClip.name, 0.24f, 0, 0f);
            return;
        }
    }

    private void KeepAnimationOnVisualRoot()
    {
        foreach (Animator visualAnimator in GetComponentsInChildren<Animator>(true))
            if (visualAnimator != null) visualAnimator.applyRootMotion = false;
    }

    private void CacheBones()
    {
        if (animator != null && animator.avatar != null && animator.avatar.isHuman)
        {
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        }
        head ??= FindChild("head");
        leftArm ??= FindChild("left_arm", "arm_l", "arm left", "leftarm");
        rightArm ??= FindChild("right_arm", "arm_r", "arm right", "rightarm");
        leftLeg ??= FindChild("left_leg", "leg_l", "leg left", "leftleg");
        rightLeg ??= FindChild("right_leg", "leg_r", "leg right", "rightleg");
        headRest = head != null ? head.localRotation : Quaternion.identity;
        leftArmRest = leftArm != null ? leftArm.localRotation : Quaternion.identity;
        rightArmRest = rightArm != null ? rightArm.localRotation : Quaternion.identity;
        leftLegRest = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
        rightLegRest = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
    }

    private Transform FindChild(params string[] names)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        foreach (string name in names)
            if (child.name.Replace(" ", "_").Equals(name.Replace(" ", "_"), StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }

    private void AnimateProceduralFallback()
    {
        if (animator != null && animator.avatar != null && animator.avatar.isHuman && animator.runtimeAnimatorController != null) return;
        bool speaking = Time.time < actionUntil;
        float stride = agent != null && agent.enabled && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.05f ? Mathf.Sin(phase * 8f) * 24f : 0f;
        float gesture = speaking ? Mathf.Sin(phase * 4.3f) * 10f + 17f : Mathf.Sin(phase * 1.8f) * 2.5f;
        if (head != null) head.localRotation = headRest * Quaternion.Euler(Mathf.Sin(phase * 1.4f) * 2.5f, 0f, 0f);
        float leftGesture = activity == "eat" && speaking ? -58f + Mathf.Sin(phase * 3.8f) * 5f : gesture * 0.6f;
        float rightGesture = activity == "work" && speaking ? -37f + Mathf.Sin(phase * 4f) * 8f : -gesture;
        if (leftArm != null) leftArm.localRotation = leftArmRest * Quaternion.Euler(stride, 0f, leftGesture);
        if (rightArm != null) rightArm.localRotation = rightArmRest * Quaternion.Euler(-stride, 0f, rightGesture);
        if (leftLeg != null) leftLeg.localRotation = leftLegRest * Quaternion.Euler(stride, 0f, 0f);
        if (rightLeg != null) rightLeg.localRotation = rightLegRest * Quaternion.Euler(-stride, 0f, 0f);
    }

    private System.Collections.IEnumerator FacePlayerBriefly(Transform player)
    {
        float stop = Time.time + 0.7f;
        while (player != null && Time.time < stop)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.03f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            yield return null;
        }
    }

    private float RandomRange(float min, float max) => random == null ? UnityEngine.Random.Range(min, max) : min + (float)random.NextDouble() * (max - min);
}
