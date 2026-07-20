using System.Collections.Generic;
using OnlyOnePlayer.Prototype.Characters;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class CheckpointWaitZone2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CheckpointAccessController2D accessController;
        [SerializeField] private Collider2D triggerCollider;

        [Header("Permission")]
        [SerializeField, Min(0f)] private float requiredWaitSeconds = 3f;
        [SerializeField] private bool resetProgressWhenLeaving = true;

        private readonly Dictionary<CharacterIdentity, float> waitTimers = new();

        private void Awake()
        {
            if (accessController == null)
            {
                accessController = GetComponentInParent<CheckpointAccessController2D>();
            }

            SetupTrigger();
        }

        private void OnEnable()
        {
            SetupTrigger();
        }

        private void Update()
        {
            if (waitTimers.Count == 0)
            {
                return;
            }

            CharacterIdentity[] actors = new CharacterIdentity[waitTimers.Count];
            waitTimers.Keys.CopyTo(actors, 0);

            foreach (CharacterIdentity actor in actors)
            {
                if (actor == null)
                {
                    waitTimers.Remove(actor);
                    continue;
                }

                waitTimers[actor] += Time.deltaTime;
                if (waitTimers[actor] >= requiredWaitSeconds)
                {
                    accessController?.GrantPermission(actor);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor != null && !waitTimers.ContainsKey(actor))
            {
                waitTimers.Add(actor, 0f);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            CharacterIdentity actor = other.GetComponent<CharacterIdentity>();
            if (actor == null || !resetProgressWhenLeaving)
            {
                return;
            }

            waitTimers.Remove(actor);
        }

        private void Reset()
        {
            accessController = GetComponentInParent<CheckpointAccessController2D>();
            triggerCollider = GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void SetupTrigger()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            triggerCollider.isTrigger = true;
        }
    }
}
