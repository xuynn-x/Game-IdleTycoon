using UnityEngine;
using UnityEngine.UI;

public class StationProgressUI : MonoBehaviour
{
    public GameObject root;   // StationProgressCanvas (GameObject)
    public Image ring;        // RingImage (Image component)

    private void Awake()
    {
        // auto bind nếu quên kéo
        if (root == null) root = gameObject;
        if (ring == null) ring = GetComponentInChildren<Image>(true);

        // ép kiểu Filled - Radial 360 để fillAmount chạy đúng
        if (ring != null)
        {
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;
        }

        // tắt lúc đầu cho khỏi hiện sẵn
        Hide();
    }

    public void Show(float normalized01)
    {
        if (root != null && !root.activeSelf) root.SetActive(true);

        if (ring != null)
            ring.fillAmount = Mathf.Clamp01(normalized01);
    }

    public void Hide()
    {
        if (ring != null) ring.fillAmount = 0f;
        if (root != null) root.SetActive(false);
    }
}
