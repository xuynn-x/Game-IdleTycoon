using UnityEngine;
using UnityEngine.AI;

public class CustomerFlow : MonoBehaviour
{
    public enum State
    {
        MoveToQueue,
        Waiting,
        Leaving
    }

    [Header("Refs")]
    public NavMeshAgent agent;
    public NavMeshFaceController face;
    public CustomerRequest3D request;

    [Header("Points (assigned by spawner)")]
    public Transform queuePoint;                 // QueuePoint_i
    public Transform employeeInteractPoint;      // EmployeeInteractPoint_i
    public Transform exitPoint;                  // CustomerExitPoint

    [Header("Tuning")]
    public float arriveSnapDistance = 0.12f;
    public float keepSnapDistance = 0.03f;
    public float setDestinationCooldown = 0.3f;

    [Header("State")]
    public State state = State.MoveToQueue;

    public bool ArrivedQueue => state == State.Waiting;
    public Transform EmployeeInteractPoint => employeeInteractPoint;

    public System.Action<CustomerFlow> OnExited;

    float _nextRepathTime;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        face = GetComponent<NavMeshFaceController>();
        request = GetComponent<CustomerRequest3D>();
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (face == null) face = GetComponent<NavMeshFaceController>();
        if (request == null) request = GetComponent<CustomerRequest3D>();

        if (agent != null) agent.updateRotation = false;

        RemoveObstacleIfAny();
    }

    private void OnEnable()
    {
        RemoveObstacleIfAny();
    }

    private void RemoveObstacleIfAny()
    {
        var obs = GetComponent<NavMeshObstacle>();
        if (obs != null) Destroy(obs);
    }

    private void Start()
    {
        if (request != null) request.HideRequest();
        if (queuePoint != null) GoToQueue(force: true);
    }

    private void Update()
    {
        if (agent == null) return;

        switch (state)
        {
            case State.MoveToQueue:
                TickMoveToQueue();
                break;

            case State.Waiting:
                TickWaiting();
                break;

            case State.Leaving:
                TickLeaving();
                break;
        }
    }

    public void SetSlot(Transform qPoint, Transform interactPoint, Transform exPoint, bool forceMove)
    {
        queuePoint = qPoint;
        employeeInteractPoint = interactPoint;
        exitPoint = exPoint;

        if (forceMove) GoToQueue(force: true);
    }

    public void GoToQueue(bool force)
    {
        if (queuePoint == null) return;

        // Bắt đầu di chuyển -> bỏ lock nhìn để không đi lùi
        face?.ClearLook();

        state = State.MoveToQueue;
        EnsureAgentEnabled();
        agent.isStopped = false;

        if (force)
        {
            agent.ResetPath();
            agent.SetDestination(queuePoint.position);
            _nextRepathTime = Time.time + setDestinationCooldown;
        }
    }

    public void Leave()
    {
        if (exitPoint == null) return;

        // rời đi -> bỏ lock nhìn
        face?.ClearLook();

        state = State.Leaving;
        EnsureAgentEnabled();
        agent.isStopped = false;

        agent.ResetPath();
        agent.SetDestination(exitPoint.position);
        _nextRepathTime = Time.time + setDestinationCooldown;
    }

    void TickMoveToQueue()
    {
        if (queuePoint == null) return;

        EnsureAgentEnabled();
        agent.isStopped = false;

        if (Time.time >= _nextRepathTime)
        {
            if (!agent.hasPath)
                agent.SetDestination(queuePoint.position);

            _nextRepathTime = Time.time + setDestinationCooldown;
        }

        if (HasArrived(queuePoint.position))
            EnterWaitingSnapToPoint();
    }

    void EnterWaitingSnapToPoint()
    {
        state = State.Waiting;

        EnsureAgentEnabled();
        agent.isStopped = true;
        agent.ResetPath();

        WarpTo(queuePoint.position);

        // đứng hàng -> nhìn vào đúng employeeInteractPoint tương ứng
        if (face != null && employeeInteractPoint != null)
            face.FaceTarget(employeeInteractPoint.position, true);
    }

    void TickWaiting()
    {
        // giữ đúng vị trí queuePoint
        if (queuePoint != null)
        {
            float d = Vector3.Distance(transform.position, queuePoint.position);
            if (d > keepSnapDistance)
                WarpTo(queuePoint.position);
        }

        // giữ nhìn vào employeeInteractPoint
        if (face != null && employeeInteractPoint != null)
            face.FaceTarget(employeeInteractPoint.position, false);
        else
            face?.ClearLook();
    }

    void TickLeaving()
    {
        if (exitPoint == null) return;

        EnsureAgentEnabled();
        agent.isStopped = false;

        if (Time.time >= _nextRepathTime)
        {
            if (!agent.hasPath)
                agent.SetDestination(exitPoint.position);

            _nextRepathTime = Time.time + setDestinationCooldown;
        }

        if (HasArrived(exitPoint.position))
            OnExited?.Invoke(this);
    }

    bool HasArrived(Vector3 targetPos)
    {
        if (agent.pathPending) return false;

        float dist = Vector3.Distance(transform.position, targetPos);
        float threshold = Mathf.Max(arriveSnapDistance, agent.stoppingDistance + 0.02f);

        if (agent.hasPath)
        {
            if (agent.remainingDistance > threshold) return false;
            return true;
        }

        return dist <= threshold;
    }

    void WarpTo(Vector3 pos)
    {
        if (agent != null && agent.enabled)
            agent.Warp(pos);
        else
            transform.position = pos;
    }

    void EnsureAgentEnabled()
    {
        if (agent != null && !agent.enabled)
            agent.enabled = true;
    }
}
