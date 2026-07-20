using System.Collections.Generic;
using UnityEngine;

namespace OnlyOnePlayer.Prototype.Characters
{
    public static class CharacterCollisionRegistry2D
    {
        private static readonly List<Collider2D> RegisteredColliders = new();

        public static void Register(Collider2D collider2D)
        {
            if (collider2D == null || RegisteredColliders.Contains(collider2D))
            {
                return;
            }

            foreach (Collider2D registeredCollider in RegisteredColliders)
            {
                if (registeredCollider != null)
                {
                    Physics2D.IgnoreCollision(collider2D, registeredCollider, true);
                }
            }

            RegisteredColliders.Add(collider2D);
        }

        public static void Unregister(Collider2D collider2D)
        {
            if (collider2D == null)
            {
                return;
            }

            RegisteredColliders.Remove(collider2D);
        }
    }
}
