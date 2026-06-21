using UnityEngine;
using UnityEngine.UI;

namespace Cardmong.Util
{
    public static class Extensions
    {
        public static void SetAlpha(this Image image, float alpha)
        {
            var color = image.color;
            color.a   = alpha;
            image.color = color;
        }

        public static bool IsNullOrEmpty(this string value)
            => string.IsNullOrEmpty(value);

        public static Vector3 WithZ(this Vector2 v, float z = 0f)
            => new Vector3(v.x, v.y, z);
    }
}
