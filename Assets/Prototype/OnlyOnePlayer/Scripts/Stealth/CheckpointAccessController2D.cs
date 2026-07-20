using System.Collections.Generic;
using OnlyOnePlayer.Prototype.Characters;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public sealed class CheckpointAccessController2D : MonoBehaviour
    {
        [Header("Permission")]
        [SerializeField] private bool consumePermissionOnPass = true;

        private readonly HashSet<CharacterIdentity> permittedActors = new();

        public void GrantPermission(CharacterIdentity actor)
        {
            if (actor != null)
            {
                permittedActors.Add(actor);
            }
        }

        public bool HasPermission(CharacterIdentity actor)
        {
            return actor != null && permittedActors.Contains(actor);
        }

        public void MarkPassed(CharacterIdentity actor)
        {
            if (actor != null && consumePermissionOnPass)
            {
                permittedActors.Remove(actor);
            }
        }
    }
}
