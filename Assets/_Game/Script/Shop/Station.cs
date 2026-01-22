using UnityEngine;

namespace Shop
{
    public class Station : MonoBehaviour
    {
        [Header("What this station provides")]
        public ProductId productId = ProductId.WaterBottle;

        [Header("Where employee should stand to interact")]
        public Transform stationPoint; // WaterStationPoint

        [Header("Gather settings")]
        public float gatherTime = 3f;

        [Header("Prefab employee carries after gather")]
        public GameObject carryItemPrefab;

        [Header("Progress UI (ring)")]
        public StationProgressUI progressUI;

        [Header("Economy")]
        public int price = 10;

        public void ShowProgress(float normalized01)
        {
            if (progressUI != null) progressUI.Show(normalized01);
        }

        public void HideProgress()
        {
            if (progressUI != null) progressUI.Hide();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (stationPoint == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(stationPoint.position, 0.2f);
            Gizmos.DrawLine(transform.position, stationPoint.position);
        }
#endif
    }
}
