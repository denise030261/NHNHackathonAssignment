using UnityEngine;

namespace OnlyOnePlayer.Prototype.Stealth
{
    public enum OneWayDirection2D
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class OneWayDirection2DExtensions
    {
        public static Vector2 ToVector(this OneWayDirection2D direction)
        {
            return direction switch
            {
                OneWayDirection2D.Up => Vector2.up,
                OneWayDirection2D.Down => Vector2.down,
                OneWayDirection2D.Left => Vector2.left,
                OneWayDirection2D.Right => Vector2.right,
                _ => Vector2.right
            };
        }
    }
}
