using System.Collections.Generic;
using UnityEngine;

namespace Shop
{
    public class StationRegistry : MonoBehaviour
    {
        public static StationRegistry I { get; private set; }

        private readonly List<Station> _stations = new();

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;

            _stations.Clear();
            _stations.AddRange(UnityEngine.Object.FindObjectsByType<Station>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
));

        }

        public Station FindStation(ProductId productId, Vector3 fromPosition)
        {
            Station best = null;
            float bestDist = float.MaxValue;

            foreach (var s in _stations)
            {
                if (s == null || s.productId != productId) continue;
                if (s.stationPoint == null) continue;

                float d = Vector3.Distance(fromPosition, s.stationPoint.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }

            return best;
        }
    }
}
