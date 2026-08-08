using System.Collections.Generic;
using UnityEngine;

namespace NHNHackathon.AudioSystem
{
    [DisallowMultipleComponent]
    public sealed class RandomAnimationSfxEmitter : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField, Range(0f, 1f)] private float volumeScale = 0.75f;
        [SerializeField, Tooltip("Avoid playing the exact same clip twice in succession.")]
        private bool avoidImmediateRepeat = true;
        [SerializeField, Tooltip("When enabled, this animation receiver stays silent unless a Dancing Zone owns it.")]
        private bool requireSharedZone;

        private AudioClip lastPlayedClip;
        private DanceZoneSfxEmitter sharedZoneEmitter;

        public void AssignSharedZone(DanceZoneSfxEmitter zoneEmitter)
        {
            sharedZoneEmitter = zoneEmitter;
        }

        // Add this function to an Animation Event at any desired frame.
        public void PlayRandomDollMovementSfx()
        {
            if (sharedZoneEmitter != null)
            {
                sharedZoneEmitter.PlayRandomDollMovementSfx();
                return;
            }
            if (requireSharedZone)
            {
                return;
            }

            IReadOnlyList<AudioClip> clips = GameSfxPlayer.Library?.DollMovementClips;
            if (clips == null || clips.Count == 0)
            {
                return;
            }

            int validCandidateCount = 0;
            foreach (AudioClip clip in clips)
            {
                if (clip != null && (!avoidImmediateRepeat || clip != lastPlayedClip))
                {
                    validCandidateCount++;
                }
            }

            if (validCandidateCount == 0)
            {
                lastPlayedClip = null;
                PlayRandomDollMovementSfx();
                return;
            }

            int selectedCandidate = Random.Range(0, validCandidateCount);
            foreach (AudioClip clip in clips)
            {
                if (clip == null || avoidImmediateRepeat && clip == lastPlayedClip)
                {
                    continue;
                }
                if (selectedCandidate-- > 0)
                {
                    continue;
                }

                lastPlayedClip = clip;
                GameSfxPlayer.PlayAtPoint(clip, transform.position, volumeScale);
                return;
            }
        }
    }
}
