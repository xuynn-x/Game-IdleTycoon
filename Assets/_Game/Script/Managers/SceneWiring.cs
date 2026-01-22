using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SceneWiring : MonoBehaviour
{
    public EmployeeFlow employee;
    public CustomerFlow customer;

    private bool wired;

    private void Awake()
    {
        AutoFindIfNull();
        TryWire("Awake");
    }

    private void Start()
    {
        AutoFindIfNull();
        TryWire("Start");
    }

    private void AutoFindIfNull()
    {
        if (employee == null) employee = Object.FindFirstObjectByType<EmployeeFlow>();
        if (customer == null) customer = Object.FindFirstObjectByType<CustomerFlow>();
    }

    private void TryWire(string from)
    {
        if (wired) return;

        if (employee == null || customer == null)
        {
            Debug.LogError($"[SceneWiring:{from}] Missing refs employee={employee} customer={customer}");
            return;
        }

        employee.SetTargetCustomer(customer);
        wired = true;

        Debug.Log($"[SceneWiring:{from}] Wired Employee -> Customer: {employee.name} -> {customer.name}");
    }
}
