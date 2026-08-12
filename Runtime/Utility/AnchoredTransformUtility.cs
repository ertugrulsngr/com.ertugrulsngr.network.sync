using UnityEngine;

namespace NetworkSync.Utility
{
    public static class AnchoredTransformUtility
    {
        public static Vector3 GetLocalPosition(
            in Vector3 worldPosition,
            in Vector3 anchorPosition,
            in Quaternion anchorRotation,
            in Vector3 anchorScale)
        {
            Vector3 unrotated = Quaternion.Inverse(anchorRotation) * (worldPosition - anchorPosition);
            return GetLocalScale(unrotated, anchorScale);
        }

        public static Quaternion GetLocalRotation(in Quaternion worldRotation, in Quaternion anchorRotation)
        {
            return Quaternion.Inverse(anchorRotation) * worldRotation;
        }

        public static Vector3 GetLocalScale(in Vector3 worldScale, in Vector3 anchorScale)
        {
            return new Vector3(
                anchorScale.x != 0f ? worldScale.x / anchorScale.x : worldScale.x,
                anchorScale.y != 0f ? worldScale.y / anchorScale.y : worldScale.y,
                anchorScale.z != 0f ? worldScale.z / anchorScale.z : worldScale.z);
        }

        public static Vector3 GetWorldPosition(
            in Vector3 localPosition,
            in Vector3 anchorPosition,
            in Quaternion anchorRotation,
            in Vector3 anchorScale)
        {
            return anchorPosition + (anchorRotation * Vector3.Scale(localPosition, anchorScale));
        }

        public static Quaternion GetWorldRotation(in Quaternion localRotation, in Quaternion anchorRotation)
        {
            return anchorRotation * localRotation;
        }

        public static Vector3 GetWorldScale(in Vector3 localScale, in Vector3 anchorScale)
        {
            return Vector3.Scale(localScale, anchorScale);
        }
    }
}
