using UnityEngine;

namespace OnlyOnePlayer.Prototype.Utilities
{
    public static class CameraBoundsUtility2D
    {
        public static Vector2 ClampToMainCamera(Vector2 position, float padding)
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic)
            {
                return position;
            }

            float safePadding = Mathf.Max(0f, padding);
            float verticalExtent = Mathf.Max(0f, camera.orthographicSize - safePadding);
            float horizontalExtent = Mathf.Max(0f, camera.orthographicSize * camera.aspect - safePadding);
            Vector3 cameraPosition = camera.transform.position;

            float minX = cameraPosition.x - horizontalExtent;
            float maxX = cameraPosition.x + horizontalExtent;
            float minY = cameraPosition.y - verticalExtent;
            float maxY = cameraPosition.y + verticalExtent;

            return new Vector2(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY));
        }
    }
}
