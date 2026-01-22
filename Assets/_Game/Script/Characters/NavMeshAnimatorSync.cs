using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class NavMeshAnimatorSync : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;

    public string speedParam = "Speed";
    public float damp = 0.12f;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!agent || !animator) return;

        // Chặn warning: Animator không có Controller
        if (animator.runtimeAnimatorController == null) return;

        // Nếu agent chưa đứng trên navmesh -> set speed = 0
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            animator.SetFloat(speedParam, 0f, damp, Time.deltaTime);
            return;
        }

        float speed = agent.velocity.magnitude;
        if (speed < 0.001f) speed = agent.desiredVelocity.magnitude;

        animator.SetFloat(speedParam, speed, damp, Time.deltaTime);
    }
}
