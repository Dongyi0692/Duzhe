using UnityEngine;

namespace Duzhe.Player
{
    /// Orthographic follow camera clamped inside the background art so no
    /// blank area outside the background is ever visible (spec section 1).
    [DefaultExecutionOrder(10)]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform followTarget;
        [Tooltip("Follow smoothing; 0 snaps instantly.")]
        public float followDamping = 3.5f;
        [Tooltip("Vertical look-ahead offset in world units (River raises it so whirlpools are visible >=1.5s early).")]
        public float yOffset = 0f;

        private Camera cam;
        private Bounds? worldBounds;
        private Vector3 velocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null) cam.orthographicSize = Core.GameBootstrap.CameraHalfHeight;
        }

        /// Pass the background world bounds (or null to stop clamping).
        public void BindBounds(Bounds? bounds)
        {
            worldBounds = bounds;
        }

        public void SnapToTarget()
        {
            velocity = Vector3.zero;
            if (followTarget != null)
            {
                transform.position = ComputeTargetPosition(followTarget.position);
            }
        }

        private void LateUpdate()
        {
            if (followTarget == null) return;
            Vector3 target = ComputeTargetPosition(followTarget.position);
            float t = followDamping <= 0f ? 1f : 1f - Mathf.Exp(-followDamping * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);
        }

        private Vector3 ComputeTargetPosition(Vector3 focus)
        {
            Vector3 pos = new Vector3(focus.x, focus.y + yOffset, transform.position.z);
            if (worldBounds.HasValue)
            {
                Bounds b = worldBounds.Value;
                float halfH = cam != null ? cam.orthographicSize : Core.GameBootstrap.CameraHalfHeight;
                float halfW = halfH * (16f / 9f); // reference 16:9 viewport
                float minX = b.min.x + halfW;
                float maxX = b.max.x - halfW;
                float minY = b.min.y + halfH;
                float maxY = b.max.y - halfH;
                // If the background is smaller than the viewport, centre it.
                if (minX > maxX) { float cx = (b.min.x + b.max.x) * 0.5f; pos.x = cx; }
                else pos.x = Mathf.Clamp(pos.x, minX, maxX);
                if (minY > maxY) { float cy = (b.min.y + b.max.y) * 0.5f; pos.y = cy; }
                else pos.y = Mathf.Clamp(pos.y, minY, maxY);
            }
            return pos;
        }
    }
}
