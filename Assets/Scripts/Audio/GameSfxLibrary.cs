using System.Collections.Generic;
using NHNHackathon.MainMenu;
using UnityEngine;

namespace NHNHackathon.AudioSystem
{
    [CreateAssetMenu(fileName = "GameSfxLibrary", menuName = "NHN Hackathon/Audio/Game SFX Library")]
    public sealed class GameSfxLibrary : ScriptableObject
    {
        [Header("Item Pickup")]
        [SerializeField] private AudioClip keyPickup;
        [SerializeField] private AudioClip paperPickup;
        [SerializeField] private AudioClip flashlightPickup;

        [Header("World Interaction")]
        [SerializeField] private AudioClip flashlightToggle;
        [SerializeField] private AudioClip regularDoor;
        [SerializeField] private AudioClip doorSlam;
        [SerializeField] private AudioClip lightFlicker;
        [SerializeField] private AudioClip firstExitUnlock;
        [SerializeField] private AudioClip exitUnlockLoop;

        [Header("Doll Animation")]
        [SerializeField] private List<AudioClip> dollMovementClips = new();

        [Header("UI")]
        [SerializeField] private AudioClip uiHovered;
        [SerializeField] private AudioClip uiClick;

        public AudioClip KeyPickup => keyPickup;
        public AudioClip PaperPickup => paperPickup;
        public AudioClip FlashlightPickup => flashlightPickup;
        public AudioClip FlashlightToggle => flashlightToggle;
        public AudioClip RegularDoor => regularDoor;
        public AudioClip DoorSlam => doorSlam;
        public AudioClip LightFlicker => lightFlicker;
        public AudioClip FirstExitUnlock => firstExitUnlock;
        public AudioClip ExitUnlockLoop => exitUnlockLoop;
        public IReadOnlyList<AudioClip> DollMovementClips => dollMovementClips;
        public AudioClip UiHovered => uiHovered;
        public AudioClip UiClick => uiClick;
    }

    public static class GameSfxPlayer
    {
        private const string ResourceName = "GameSfxLibrary";
        private static GameSfxLibrary library;

        public static GameSfxLibrary Library
        {
            get
            {
                library ??= Resources.Load<GameSfxLibrary>(ResourceName);
                return library;
            }
        }

        public static void PlayAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null)
            {
                return;
            }

            float volume = Mathf.Clamp01(AudioSettingsController.SavedSfxVolume * volumeScale);
            GameSfxPool.Instance.Play(clip, position, volume);
        }

        public static void PlayKeyPickup(Vector3 position) =>
            PlayAtPoint(Library != null ? Library.KeyPickup : null, position);

        public static void PlayPaperPickup(Vector3 position) =>
            PlayAtPoint(Library != null ? Library.PaperPickup : null, position);

        public static void PlayFlashlightPickup(Vector3 position) =>
            PlayAtPoint(Library != null ? Library.FlashlightPickup : null, position);

        public static void PlayFlashlightToggle(Vector3 position) =>
            PlayAtPoint(Library != null ? Library.FlashlightToggle : null, position);

        public static void PlayRegularDoor(Vector3 position) =>
            PlayAtPoint(Library != null ? Library.RegularDoor : null, position);

        public static void PlayDoorSlam(Vector3 position) =>
            PlayAtPoint(Library != null ? Library.DoorSlam : null, position);

        public static void PlayLightFlicker(Vector3 position) =>
            PlayAtPoint(Library != null ? Library.LightFlicker : null, position);

        public static void PlayFirstExitUnlock(Vector3 position) =>
            PlayAtPoint(Library != null ? Library.FirstExitUnlock : null, position);
    }
}
