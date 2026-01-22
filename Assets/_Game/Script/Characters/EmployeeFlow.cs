using UnityEngine;
using UnityEngine.AI;
using Shop;

public class EmployeeFlow : MonoBehaviour
{
    public enum State
    {
        IdleAtHome,
        GoToInteract,
        ActivateRequest,
        GoToStation,
        Gathering,
        ReturnToCustomer,
        Deliver
    }

    [Header("Refs")]
    public NavMeshAgent agent;
    public NavMeshFaceController face;

    [Header("Points")]
    public Transform idlePoint;       // EmployeeIdlePoint_0 (gán ở Scene instance)
    public Transform interactPoint;   // EmployeeInteractPoint_x (lấy từ customer slot)

    [Header("Carry")]
    public Transform carrySlot;       // HandSlot
    private GameObject carriedItem;

    [Header("Target Customer")]
    public CustomerFlow targetCustomerFlow;
    public CustomerRequest3D targetCustomerRequest;

    [Header("Runtime")]
    public State state = State.IdleAtHome;

    private Station currentStation;
    private float gatherT;

    [Header("Facing (GoToStation)")]
    [Tooltip("Còn xa quầy: quay theo hướng di chuyển. Vào gần (<= distance): quay mặt vào quầy.")]
    public float faceStationDistance = 1.2f;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        face = GetComponent<NavMeshFaceController>();
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (face == null) face = GetComponent<NavMeshFaceController>();

        if (agent != null) agent.updateRotation = false; // Update Rotation OFF (bằng code)
    }

    private void Start()
    {
        face?.ClearLook();
        GoIdle();
    }

    // ===== NavMesh safety =====
    bool EnsureOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        // snap nhẹ về navmesh gần nhất để tránh lỗi đỏ
        if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }
        return false;
    }

    void StartMoveTo(Vector3 pos)
    {
        if (!EnsureOnNavMesh()) return;

        // đang di chuyển thì KHÔNG lock nhìn mục tiêu
        face?.ClearLook();

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(pos);
    }

    void StopMove()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private void Update()
    {
        
        if (agent == null) return;
        if (!EnsureOnNavMesh()) return;

        switch (state)
        {
            case State.IdleAtHome:
            {
                // Nếu đã có target customer và customer đã tới queue -> đi tới interact
                if (targetCustomerFlow != null && targetCustomerFlow.ArrivedQueue)
                {
                    GoToInteractPoint();
                    break;
                }

                // Nếu chưa có target -> thử lấy khách đang Waiting
                TryPickNextCustomerOrIdle();
                break;
            }

            case State.GoToInteract:
            {
                // đang đi -> face tự quay theo hướng chạy (do ClearLook khi StartMoveTo)
                if (HasArrived())
                {
                    StopMove();
                    state = State.ActivateRequest;

                    // tới khách -> quay nhìn khách
                    if (targetCustomerFlow != null)
                        face?.LookAt(targetCustomerFlow.transform, true);
                }
                break;
            }

            case State.ActivateRequest:
            {
                // giữ nhìn khách
                if (targetCustomerFlow != null)
                    face?.LookAt(targetCustomerFlow.transform, false);

                ActivateCustomerRequest();
                DecideAndGoToStation();
                break;
            }

            case State.GoToStation:
            {
                // ===== CÁCH A: xa thì quay theo hướng chạy, gần quầy thì nhìn quầy =====
                if (currentStation != null && currentStation.stationPoint != null)
                {
                    float d = Vector3.Distance(transform.position, currentStation.stationPoint.position);

                    if (d <= faceStationDistance)
                        face?.FaceTarget(currentStation.stationPoint.position, false); // nhìn quầy
                    else
                        face?.ClearLook(); // bỏ lock để quay theo hướng chạy
                }

                if (HasArrived())
                {
                    StopMove();
                    BeginGather(); // snap nhìn quầy
                }
                break;
            }

            case State.Gathering:
            {
                // đang gather -> luôn nhìn vào quầy
                if (currentStation != null && currentStation.stationPoint != null)
                    face?.FaceTarget(currentStation.stationPoint.position, false);

                TickGather();
                break;
            }

            case State.ReturnToCustomer:
            {
                // đang đi -> quay theo hướng chạy
                if (HasArrived())
                {
                    StopMove();
                    state = State.Deliver;

                    if (targetCustomerFlow != null)
                        face?.LookAt(targetCustomerFlow.transform, true);
                }
                break;
            }

            case State.Deliver:
            {
                // giữ nhìn khách
                if (targetCustomerFlow != null)
                    face?.LookAt(targetCustomerFlow.transform, false);

                var served = DeliverAndClearTarget(); // phục vụ xong, trả về khách vừa phục vụ

                // lấy khách tiếp theo đang Waiting (trừ khách vừa served)
                var next = (CustomerSpawner.I != null)
                    ? CustomerSpawner.I.GetNextWaitingCustomer(served)
                    : null;

                if (next != null && next.ArrivedQueue)
                {
                    // ✅ có khách tiếp theo -> đi thẳng tới interact của khách đó
                    SetTargetCustomer(next);
                    GoToInteractPoint();
                }
                else
                {
                    // ✅ không còn khách -> về idle
                    GoIdle();
                }
                break;
            }
            
        }
        
    }

    // ===== 핵: pick next customer when idle =====
    void TryPickNextCustomerOrIdle()
    {
        var next = (CustomerSpawner.I != null)
            ? CustomerSpawner.I.GetNextWaitingCustomer()
            : null;

        if (next != null && next.ArrivedQueue)
        {
            SetTargetCustomer(next);
            GoToInteractPoint();
        }
        else
        {
            KeepAtIdlePoint();
        }
    }

    // ===== deliver =====
    private CustomerFlow DeliverAndClearTarget()
    {
        var served = targetCustomerFlow;

    // Ẩn request đúng nghĩa
    if (targetCustomerRequest != null)
        targetCustomerRequest.HideRequest();

    // Bỏ item trên tay
    ClearCarryItem();

    // Cộng tiền
    if (MoneyManager.I != null)
    {
        int add = 0;

        // Ưu tiên lấy giá từ station nếu có
        if (currentStation != null)
            add = Mathf.Max(0, currentStation.price);

        // Nếu chưa có station/price thì test tạm:
        // add = 10;

        MoneyManager.I.Add(add);
    }

    // Cho khách rời đi
    if (served != null)
        served.Leave();

    // Clear target để nhận khách mới
    targetCustomerFlow = null;
    targetCustomerRequest = null;
    interactPoint = null;
    currentStation = null;

    face?.ClearLook();
    return served;
    }

    public void SetTargetCustomer(CustomerFlow customerFlow)
    {
        targetCustomerFlow = customerFlow;
        targetCustomerRequest = customerFlow != null ? customerFlow.GetComponent<CustomerRequest3D>() : null;

        // dùng đúng interact point của slot customer
        if (customerFlow != null && customerFlow.EmployeeInteractPoint != null)
            interactPoint = customerFlow.EmployeeInteractPoint;
    }

    // ===== idle / interact =====
    private void KeepAtIdlePoint()
    {
        if (idlePoint == null) return;

        if (!IsNear(idlePoint.position))
            StartMoveTo(idlePoint.position);
    }

    private void GoIdle()
    {
        state = State.IdleAtHome;
        if (idlePoint == null) return;

        StartMoveTo(idlePoint.position);
    }

    private void GoToInteractPoint()
    {
        // nếu chưa set, lấy từ customer
        if (interactPoint == null && targetCustomerFlow != null)
            interactPoint = targetCustomerFlow.EmployeeInteractPoint;

        if (interactPoint == null) return;

        state = State.GoToInteract;
        StartMoveTo(interactPoint.position);
    }

    // ===== request / station =====
    private void ActivateCustomerRequest()
    {
        if (targetCustomerRequest == null) return;

        // demo: luôn WaterBottle
        if (targetCustomerRequest.requestedProduct == Shop.ProductId.WaterBottle)
            targetCustomerRequest.ShowRequest(Shop.ProductId.WaterBottle);
    }

    private void DecideAndGoToStation()
    {
        if (targetCustomerRequest == null) { GoIdle(); return; }
        if (StationRegistry.I == null) { GoIdle(); return; }

        var productId = targetCustomerRequest.requestedProduct;

        currentStation = StationRegistry.I.FindStation(productId, transform.position);
        if (currentStation == null || currentStation.stationPoint == null)
        {
            GoIdle();
            return;
        }

        state = State.GoToStation;
        StartMoveTo(currentStation.stationPoint.position);
    }

    private void BeginGather()
    {
        state = State.Gathering;
        gatherT = 0f;

        if (currentStation != null && currentStation.stationPoint != null)
            face?.FaceTarget(currentStation.stationPoint.position, true); // snap nhìn quầy

        if (currentStation != null && currentStation.progressUI != null)
            currentStation.progressUI.Show(0f);
    }

    private void TickGather()
    {
        if (currentStation == null) { GoIdle(); return; }

        float dur = Mathf.Max(0.1f, currentStation.gatherTime);
        gatherT += Time.deltaTime;

        float t01 = Mathf.Clamp01(gatherT / dur);
        if (currentStation.progressUI != null)
            currentStation.progressUI.Show(t01);

        if (gatherT >= dur)
        {
            if (currentStation.progressUI != null)
                currentStation.progressUI.Hide();

            SpawnCarryItem();
            GoReturnToCustomer();
        }
    }

    private void SpawnCarryItem()
    {
        ClearCarryItem();

        if (currentStation == null || currentStation.carryItemPrefab == null) return;
        if (carrySlot == null) return;

        carriedItem = Instantiate(currentStation.carryItemPrefab, carrySlot);
        carriedItem.transform.localPosition = Vector3.zero;
        carriedItem.transform.localRotation = Quaternion.identity;
    }

    private void ClearCarryItem()
    {
        if (carriedItem != null)
        {
            Destroy(carriedItem);
            carriedItem = null;
        }
    }

    private void GoReturnToCustomer()
    {
        // luôn dùng đúng interact point của customer
        if (targetCustomerFlow != null && targetCustomerFlow.EmployeeInteractPoint != null)
            interactPoint = targetCustomerFlow.EmployeeInteractPoint;

        if (interactPoint == null) { GoIdle(); return; }

        state = State.ReturnToCustomer;
        StartMoveTo(interactPoint.position);
    }

    // ===== utils =====
    private bool HasArrived()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return false;
        if (agent.pathPending) return false;
        if (!agent.hasPath) return false;

        return agent.remainingDistance <= agent.stoppingDistance + 0.05f;
    }

    private bool IsNear(Vector3 pos)
    {
        return Vector3.Distance(transform.position, pos) <= 0.3f;
    }
}
