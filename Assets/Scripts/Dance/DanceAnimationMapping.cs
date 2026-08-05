using System;
using UnityEngine;

namespace NHNHackathon.Dance
{
    [Serializable]
    public sealed class DanceAnimationMapping
    {
        [SerializeField] private int danceId = 1;
        [SerializeField] private AnimationClip animationClip;
        [SerializeField, Min(0.01f)] private float playbackSpeed = 1f;

        public int DanceId => danceId;
        public AnimationClip AnimationClip => animationClip;
        public float PlaybackSpeed => playbackSpeed;
    }
}
