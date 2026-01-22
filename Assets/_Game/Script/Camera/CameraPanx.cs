using UnityEngine;

public class CameraPanX : MonoBehaviour
{
    [Header("Target (camera root)")]
    public Transform target; // nếu để null -> tự dùng transform của object gắn script

    [Header("Pan Settings")]
    public float sensitivityMouse = 0.02f;   // độ nhạy chuột
    public float sensitivityTouch = 0.02f;   // độ nhạy touch
    public float smooth = 12f;               // độ mượt
    public float inertia = 0.90f;            // 0..1 (càng gần 1 càng trôi lâu)

    [Header("Clamp X")]
    public float minX = -20f;
    public float maxX =  20f;

    // runtime
    private bool dragging;
    private Vector2 lastPos;
    private float velocityX; // tốc độ trượt theo X

    void Awake()
    {
        if (target == null) target = transform;
    }

    void Update()
    {
        // ======= INPUT: Mouse =======
        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButtonDown(0))
            {
                dragging = true;
                lastPos = Input.mousePosition;
                velocityX = 0f;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }

            if (dragging)
            {
                Vector2 cur = Input.mousePosition;
                Vector2 delta = cur - lastPos;
                lastPos = cur;

                // Kéo lên/xuống màn hình -> đổi X (delta.y)
                float dx = -delta.y * sensitivityMouse;

                velocityX = Mathf.Lerp(velocityX, dx / Mathf.Max(Time.deltaTime, 0.0001f), 0.5f);
                MoveX(dx);
            }
        }

        // ======= INPUT: Touch =======
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                dragging = true;
                lastPos = t.position;
                velocityX = 0f;
            }
            else if (t.phase == TouchPhase.Moved && dragging)
            {
                Vector2 delta = t.position - lastPos;
                lastPos = t.position;

                float dx = -delta.y * sensitivityTouch;

                velocityX = Mathf.Lerp(velocityX, dx / Mathf.Max(Time.deltaTime, 0.0001f), 0.5f);
                MoveX(dx);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                dragging = false;
            }
        }

        // ======= Inertia (thả ra vẫn trôi nhẹ) =======
        if (!dragging)
        {
            velocityX *= inertia;
            if (Mathf.Abs(velocityX) > 0.01f)
            {
                float dx = (velocityX * Time.deltaTime);
                MoveX(dx);
            }
        }
    }

    void MoveX(float dx)
    {
        Vector3 p = target.position;
        float targetX = Mathf.Clamp(p.x + dx, minX, maxX);

        // mượt
        p.x = Mathf.Lerp(p.x, targetX, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        target.position = p;
    }
}
