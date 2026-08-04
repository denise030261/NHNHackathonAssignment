using System.Collections.Generic;
using NHNHackathon.Characters;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DanceZoneCameraTrigger : MonoBehaviour
    {
        [Header("Perspective")]
        [SerializeField] private CameraPerspective enterPerspective = CameraPerspective.ThirdPerson;
        [SerializeField] private CameraPerspective exitPerspective = CameraPerspective.FirstPerson;
        [SerializeField, Min(0f)] private float transitionDuration = 0.6f;

        private static readonly Dictionary<PlayerCameraController, HashSet<DanceZoneCameraTrigger>>
            ActiveZones = new();

        private readonly Dictionary<PlayerCameraController, int> colliderCounts = new();

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerCameraController controller =
                other.GetComponentInParent<PlayerCameraController>();
            if (controller == null)
            {
                return;
            }

            colliderCounts.TryGetValue(controller, out int count);
            colliderCounts[controller] = count + 1;
            if (count > 0)
            {
                return;
            }

            if (!ActiveZones.TryGetValue(controller, out HashSet<DanceZoneCameraTrigger> zones))
            {
                zones = new HashSet<DanceZoneCameraTrigger>();
                ActiveZones.Add(controller, zones);
            }

            zones.Add(this);
            controller.RequestPerspective(enterPerspective, transitionDuration);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerCameraController controller =
                other.GetComponentInParent<PlayerCameraController>();
            if (controller == null || !colliderCounts.TryGetValue(controller, out int count))
            {
                return;
            }

            if (count > 1)
            {
                colliderCounts[controller] = count - 1;
                return;
            }

            colliderCounts.Remove(controller);
            LeaveZone(controller);
        }

        private void OnDisable()
        {
            foreach (PlayerCameraController controller in
                     new List<PlayerCameraController>(colliderCounts.Keys))
            {
                LeaveZone(controller);
            }
            colliderCounts.Clear();
        }

        private void LeaveZone(PlayerCameraController controller)
        {
            if (!ActiveZones.TryGetValue(controller, out HashSet<DanceZoneCameraTrigger> zones))
            {
                return;
            }

            zones.Remove(this);
            if (zones.Count > 0)
            {
                return;
            }

            ActiveZones.Remove(controller);
            if (controller != null)
            {
                controller.RequestPerspective(exitPerspective, transitionDuration);
            }
        }
    }
}
