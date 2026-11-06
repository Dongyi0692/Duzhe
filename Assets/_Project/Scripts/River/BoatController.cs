using Duzhe.Chapters;
using UnityEngine;

namespace Duzhe.River
{
    /// Steers horizontally and sails upward in world space.
    public class BoatController : MonoBehaviour
    {
        [Tooltip("World y of the boat line during the crossing.")]
        public float boatLineY = -3f;

        private RiverSettings settings;
        private float boatX;
        private float forwardDistance;
        public const float VoyageDistance = 5f;
        public float BoatY { get { return boatLineY + forwardDistance; } }

        public float BoatX { get { return boatX; } }

        public void Bind(RiverSettings s)
        {
            settings = s;
            boatX = 0f;
            forwardDistance = 0f;
            ApplyTransform();
        }

        public void ResetPosition()
        {
            boatX = 0f;
            forwardDistance = 0f;
            ApplyTransform();
        }

        /// Continuous steering; clamped to the configured range.
        public void Steer(float axisInput, float dt)
        {
            if (settings == null) return;
            boatX += axisInput * settings.steerSpeed * dt;
            if (boatX > settings.steerRangeX) boatX = settings.steerRangeX;
            if (boatX < -settings.steerRangeX) boatX = -settings.steerRangeX;
            ApplyTransform();
        }

        public void Advance(float dt)
        {
            if (settings == null || dt <= 0f) return;
            forwardDistance = Mathf.Min(VoyageDistance, forwardDistance + VoyageDistance * dt / Mathf.Max(0.1f, settings.sailDuration));
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            transform.position = new Vector3(boatX, BoatY, 0f);
        }
    }
}
