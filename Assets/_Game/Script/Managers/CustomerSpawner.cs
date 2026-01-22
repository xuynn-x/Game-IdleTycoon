using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner I;

    [Header("Prefab")]
    public CustomerFlow customerPrefab;

    [Header("Points")]
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Slots (same order)")]
    public List<Transform> queuePoints = new List<Transform>();
    public List<Transform> employeeInteractPoints = new List<Transform>(); // phải cùng số lượng với queuePoints

    [Header("Spawn")]
    public float spawnInterval = 5f;
    public int maxCustomers = 6;

    // ===== Slot system =====
    CustomerFlow[] _slots; // _slots[i] là customer đang đứng tại slot i (hoặc null)
    readonly Dictionary<CustomerFlow, int> _slotOf = new(); // tra slot của customer
    float _nextSpawnTime;

    // “FrontCustomer” giờ hiểu là: customer ở slot gần quầy nhất (index nhỏ nhất) và đã tới vị trí
    public CustomerFlow FrontCustomer
    {
        get
        {
            if (_slots == null) return null;
            for (int i = 0; i < _slots.Length; i++)
            {
                var c = _slots[i];
                if (c != null && c.ArrivedQueue) return c;
            }
            return null;
        }
    }

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    private void Start()
    {
        int slotCount = queuePoints != null ? queuePoints.Count : 0;
        if (slotCount <= 0)
        {
            Debug.LogWarning("[CustomerSpawner] queuePoints đang rỗng.");
            return;
        }

        if (employeeInteractPoints == null || employeeInteractPoints.Count != slotCount)
            Debug.LogWarning("[CustomerSpawner] employeeInteractPoints phải CÙNG SỐ LƯỢNG và CÙNG THỨ TỰ với queuePoints.");

        _slots = new CustomerFlow[slotCount];

        // Nếu trong scene đã có CustomerFlow sẵn -> gán vào slot trống gần nhất (hoặc theo index)
        var existing = FindObjectsByType<CustomerFlow>(FindObjectsSortMode.None);
        foreach (var c in existing)
        {
            if (c == null) continue;
            if (_slotOf.ContainsKey(c)) continue;

            int free = GetFirstFreeSlotIndex();
            if (free == -1) break;

            AssignCustomerToSlot(c, free, forceMove: true);
        }

        _nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        if (customerPrefab == null || spawnPoint == null || exitPoint == null) return;
        if (_slots == null || _slots.Length == 0) return;

        int slotLimit = _slots.Length;
        int hardMax = Mathf.Min(maxCustomers, slotLimit);
        if (hardMax <= 0) return;

        // Nếu tổng số customer đang có >= hardMax thì không spawn thêm
        if (CountOccupied() >= hardMax) return;

        if (Time.time >= _nextSpawnTime)
        {
            TrySpawnIntoFreeSlot();
            _nextSpawnTime = Time.time + spawnInterval;
        }
    }

    int CountOccupied()
    {
        int n = 0;
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] != null) n++;
        return n;
    }

    int GetFirstFreeSlotIndex()
    {
        if (_slots == null) return -1;
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i] == null) return i;
        return -1;
    }

    void TrySpawnIntoFreeSlot()
    {
        int slot = GetFirstFreeSlotIndex();
        if (slot == -1) return;

        var c = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);

        // đặt customer lên NavMesh để tránh lỗi "isStopped can only be called..."
        var agent = c.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (NavMesh.SamplePosition(spawnPoint.position, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        AssignCustomerToSlot(c, slot, forceMove: true);
    }

    void AssignCustomerToSlot(CustomerFlow c, int slot, bool forceMove)
    {
        _slots[slot] = c;
        _slotOf[c] = slot;

        c.OnExited += HandleCustomerExited;

        Transform q = queuePoints[slot];
        Transform interact =
            (employeeInteractPoints != null && employeeInteractPoints.Count > slot)
            ? employeeInteractPoints[slot]
            : null;

        // SetSlot: customer sẽ đi tới q và sau khi tới sẽ đứng chờ
        c.SetSlot(q, interact, exitPoint, forceMove);
    }

    void HandleCustomerExited(CustomerFlow c)
    {
        if (c == null) return;

        c.OnExited -= HandleCustomerExited;

        // clear slot
        if (_slotOf.TryGetValue(c, out int slot))
        {
            if (slot >= 0 && slot < _slots.Length && _slots[slot] == c)
                _slots[slot] = null;

            _slotOf.Remove(c);
        }

        Destroy(c.gameObject);

        // KHÔNG dồn ai lên. Slot trống sẽ được spawn vào theo spawnInterval.
    }
  public CustomerFlow GetNextWaitingCustomer(CustomerFlow exclude = null)
{
    if (_slots == null || _slots.Length == 0) return null;

    // Slot nhỏ đứng trước -> ưu tiên theo thứ tự queue
    for (int i = 0; i < _slots.Length; i++)
    {
        var c = _slots[i];
        if (c == null) continue;
        if (c == exclude) continue;

        // chỉ lấy khách đang Waiting (đứng chờ ở queuePoint)
        if (c.ArrivedQueue) return c;
    }

    return null;
}


}
