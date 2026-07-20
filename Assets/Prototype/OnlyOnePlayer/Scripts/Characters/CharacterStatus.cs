using System.Collections;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Characters
{
    public sealed class CharacterStatus : MonoBehaviour
    {
        [Header("Stun")]
        [SerializeField] private bool isStunned;

        private Coroutine stunCoroutine;
        private float stunnedUntilTime;

        public bool IsStunned => isStunned;

        public void Stun(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            stunnedUntilTime = Time.time + duration;

            if (stunCoroutine == null)
            {
                stunCoroutine = StartCoroutine(StunRoutine());
            }

            isStunned = true;
        }

        public void ClearStun()
        {
            stunnedUntilTime = 0f;
            isStunned = false;

            if (stunCoroutine != null)
            {
                StopCoroutine(stunCoroutine);
                stunCoroutine = null;
            }
        }

        private IEnumerator StunRoutine()
        {
            while (Time.time < stunnedUntilTime)
            {
                yield return null;
            }

            isStunned = false;
            stunCoroutine = null;
        }
    }
}
