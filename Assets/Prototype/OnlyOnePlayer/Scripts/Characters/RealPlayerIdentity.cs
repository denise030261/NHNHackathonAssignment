using UnityEngine;

namespace OnlyOnePlayer.Prototype.Characters
{
    public sealed class RealPlayerIdentity : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Real Player";

        public string DisplayName => displayName;
        public Transform TargetTransform => transform;
        public GameObject TargetGameObject => gameObject;

        public CharacterIdentity CharacterIdentity
        {
            get
            {
                CharacterIdentity identity = GetComponent<CharacterIdentity>();
                if (identity == null)
                {
                    identity = gameObject.AddComponent<CharacterIdentity>();
                    identity.Configure(CharacterActorType.RealPlayer, displayName);
                }

                return identity;
            }
        }
    }
}
