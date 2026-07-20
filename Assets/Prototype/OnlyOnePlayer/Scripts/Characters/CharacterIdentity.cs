using UnityEngine;

namespace OnlyOnePlayer.Prototype.Characters
{
    public sealed class CharacterIdentity : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private CharacterActorType actorType = CharacterActorType.Unknown;
        [SerializeField] private string displayName = "Character";

        public CharacterActorType ActorType => actorType;
        public string DisplayName => displayName;
        public Transform TargetTransform => transform;
        public bool IsRealPlayer => actorType == CharacterActorType.RealPlayer || GetComponent<RealPlayerIdentity>() != null;

        public void Configure(CharacterActorType type, string actorName)
        {
            actorType = type;
            displayName = actorName;
        }
    }
}
