using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingUI : MonoBehaviour
{
    [Header("Assign LoadingBar_Fill Image here")]
    [SerializeField] private Image fillImage;

    [Header("Scene to load (must be in Build Profiles > Scene List)")]
    [SerializeField] private string sceneName = "Game";

    [Header("Smoothing")]
    [Tooltip("Tốc độ tăng fill (đơn vị: fill/giây). Càng lớn càng nhanh.")]
    [SerializeField] private float fillSpeed = 1.0f;

    [Header("Minimum loading time (seconds)")]
    [Tooltip("Giữ màn hình loading tối thiểu (ví dụ 2s) để tránh nháy.")]
    [SerializeField] private float minLoadingTime = 2.0f;

    [Header("Optional: call GameState.StartGame after scene activated")]
    [SerializeField] private bool startGameAfterLoad = true;

    private void Start()
    {
        if (fillImage != null) fillImage.fillAmount = 0f;
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        float elapsed = 0f;
        float shown = 0f;

        // Load scene dạng Single để Boot biến mất khi vào Game
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (op == null)
        {
            Debug.LogError($"Cannot load scene '{sceneName}'. Check Build Profiles > Scene List.");
            yield break;
        }

        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            elapsed += Time.unscaledDeltaTime;

            // Tiến độ thật (0..1). Unity load tới 0.9 rồi dừng chờ activate.
            float real = Mathf.Clamp01(op.progress / 0.9f);

            // Tiến độ theo thời gian tối thiểu (0..1) -> chạy đều
            float timeBased = (minLoadingTime <= 0f) ? 1f : Mathf.Clamp01(elapsed / minLoadingTime);

            // Thanh hiển thị: chạy đều theo timeBased nhưng không vượt quá load thật
            shown = Mathf.Min(timeBased, real);

            if (fillImage != null)
                fillImage.fillAmount = shown;

            // Chỉ cho chuyển scene khi:
            // - Load thật đã đạt 0.9
            // - Và thanh đã chạy đủ thời gian (timeBased = 1)
            if (op.progress >= 0.9f && timeBased >= 1f)
                op.allowSceneActivation = true;

            yield return null;
        }


        // Scene Game đã active xong
        if (startGameAfterLoad && GameState.I != null)
            GameState.I.StartGame();
    }
}
