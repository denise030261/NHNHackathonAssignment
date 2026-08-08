using System.Collections.Generic;
using NHNHackathon.Dance;
using UnityEngine;

namespace NHNHackathon.AudioSystem
{
    [DisallowMultipleComponent]
    public sealed class DanceZoneSfxEmitter : MonoBehaviour
    {
        [Header("Dancing Zone Member")]
        [SerializeField, Tooltip("Uses the single Dance AI assigned to this zone's DanceSyncJudge.")]
        private bool useJudgeDanceAI = true;
        [SerializeField, Tooltip("Optional explicit members. Normally this can remain empty.")]
        private List<RandomAnimationSfxEmitter> assignedDancers = new();

        [Header("Playback")]
        [SerializeField, Range(0f, 1f)] private float volumeScale = 0.75f;
        [SerializeField, Min(0f), Tooltip("Events arriving together from several dolls are played only once per zone.")]
        private float minimumInterval = 0.05f;
        [SerializeField] private bool avoidImmediateRepeat = true;

        private AudioClip lastPlayedClip;
        private float lastPlayedTime = float.NegativeInfinity;

        private void OnEnable()
        {
            BindDancers();
        }

        private void OnDisable()
        {
            foreach (RandomAnimationSfxEmitter dancer in assignedDancers)
            {
                if (dancer != null)
                {
                    dancer.AssignSharedZone(null);
                }
            }
        }

        public void PlayRandomDollMovementSfx()
        {
            if (Time.unscaledTime - lastPlayedTime < minimumInterval)
            {
                return;
            }

            IReadOnlyList<AudioClip> clips = GameSfxPlayer.Library?.DollMovementClips;
            if (!TrySelectClip(clips, out AudioClip clip))
            {
                return;
            }

            lastPlayedClip = clip;
            lastPlayedTime = Time.unscaledTime;
            GameSfxPlayer.PlayAtPoint(clip, transform.position, volumeScale);
        }

        private void BindDancers()
        {
            if (useJudgeDanceAI)
            {
                DanceSyncJudge judge = GetComponent<DanceSyncJudge>();
                RandomAnimationSfxEmitter judgeDancer = judge != null && judge.DanceAI != null
                    ? judge.DanceAI.GetComponentInChildren<RandomAnimationSfxEmitter>(true)
                    : null;
                if (judgeDancer != null)
                {
                    AddAndBind(judgeDancer);
                }
            }

            foreach (RandomAnimationSfxEmitter dancer in assignedDancers)
            {
                if (dancer != null)
                {
                    dancer.AssignSharedZone(this);
                }
            }
        }

        private void AddAndBind(RandomAnimationSfxEmitter dancer)
        {
            if (dancer == null)
            {
                return;
            }

            if (!assignedDancers.Contains(dancer))
            {
                assignedDancers.Add(dancer);
            }
            dancer.AssignSharedZone(this);
        }

        private bool TrySelectClip(IReadOnlyList<AudioClip> clips, out AudioClip selectedClip)
        {
            selectedClip = null;
            if (clips == null || clips.Count == 0)
            {
                return false;
            }

            int candidateCount = 0;
            foreach (AudioClip clip in clips)
            {
                if (clip != null && (!avoidImmediateRepeat || clip != lastPlayedClip))
                {
                    candidateCount++;
                }
            }

            if (candidateCount == 0 && lastPlayedClip != null)
            {
                lastPlayedClip = null;
                return TrySelectClip(clips, out selectedClip);
            }
            if (candidateCount == 0)
            {
                return false;
            }

            int selectedIndex = Random.Range(0, candidateCount);
            foreach (AudioClip clip in clips)
            {
                if (clip == null || avoidImmediateRepeat && clip == lastPlayedClip)
                {
                    continue;
                }
                if (selectedIndex-- == 0)
                {
                    selectedClip = clip;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            minimumInterval = Mathf.Max(0f, minimumInterval);
        }
    }
}
