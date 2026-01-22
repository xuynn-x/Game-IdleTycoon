using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class NavMeshFaceController : MonoBehaviour
{
    [Header("Rotate Root")]
    public Transform rotateRoot; // object sẽ xoay (thường là root/visual root)

    [Header("Turn")]
    public float turnSpeedDeg = 720f;
    public bool invertForward = false;
    public float yawOffsetDeg = 0f;

    [Header("Agent")]
    public NavMeshAgent agent;
    public bool disableAgentUpdateRotation = true;

    private Transform _lockTarget;
    private bool _lockSnap;
    private bool _lockPointEnabled;
    private Vector3 _lockPoint;

    void Awake()
    {
        if (rotateRoot == null) rotateRoot = transform;
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (disableAgentUpdateRotation && agent != null)
            agent.updateRotation = false;
    }

    void Update()
    {
        // 1) LOCK nhìn target/point: không cần agent cũng quay được
        if (_lockTarget != null)
        {
            RotateTo(_lockTarget.position, _lockSnap);
            return;
        }
        if (_lockPointEnabled)
        {
            RotateTo(_lockPoint, _lockSnap);
            return;
        }

        // 2) Không lock: quay theo hướng di chuyển của agent
        if (agent == null || !agent.enabled) return;

        // QUAN TRỌNG: tránh lỗi "isStopped" khi agent chưa đứng trên NavMesh
        if (!agent.isOnNavMesh) return;

        // Nếu agent đang đứng yên thì thôi
        if (agent.isStopped) return;

        Vector3 dir = agent.desiredVelocity;

        if (dir.sqrMagnitude < 0.01f)
            dir = agent.velocity;

        if (dir.sqrMagnitude < 0.01f && agent.hasPath)
            dir = agent.steeringTarget - rotateRoot.position;

        if (dir.sqrMagnitude > 0.01f)
            FaceDirection(dir);
    }

    // ===== API =====
    public void LookAt(Transform target, bool snap = false)
    {
        _lockTarget = target;
        _lockPointEnabled = false;
        _lockSnap = snap;
    }

    public void FaceTarget(Vector3 worldPos, bool snap = false)
    {
        _lockTarget = null;
        _lockPoint = worldPos;
        _lockPointEnabled = true;
        _lockSnap = snap;
    }

    public void ClearLook()
    {
        _lockTarget = null;
        _lockPointEnabled = false;
        _lockSnap = false;
    }

    public void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0004f) return;
        RotateToDir(dir.normalized, false);
    }

    // ===== Internal =====
    private void RotateTo(Vector3 worldPos, bool snap)
    {
        Vector3 dir = worldPos - rotateRoot.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        RotateToDir(dir.normalized, snap);
    }

    private void RotateToDir(Vector3 dirNorm, bool snap)
    {
        if (invertForward) dirNorm = -dirNorm;

        Quaternion targetRot =
            Quaternion.LookRotation(dirNorm, Vector3.up) *
            Quaternion.Euler(0f, yawOffsetDeg, 0f);

        if (snap)
            rotateRoot.rotation = targetRot;
        else
            rotateRoot.rotation = Quaternion.RotateTowards(
                rotateRoot.rotation,
                targetRot,
                turnSpeedDeg * Time.deltaTime
            );
    }
}
