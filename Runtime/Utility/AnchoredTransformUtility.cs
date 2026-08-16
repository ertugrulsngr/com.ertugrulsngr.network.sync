using UnityEngine;

namespace NetworkSync.Utility
{
    public static class AnchoredTransformUtility
    {
        public static Vector3 GetLocalPosition(
            Vector3 worldPosition,
            Vector3 anchorPosition,
            Quaternion anchorRotation,
            Vector3 anchorScale)
        {
            Vector3 unrotated = Quaternion.Inverse(anchorRotation) * (worldPosition - anchorPosition);
            return GetLocalScale(unrotated, anchorScale);
        }

        public static Quaternion GetLocalRotation(Quaternion worldRotation, Quaternion anchorRotation)
        {
            return Quaternion.Inverse(anchorRotation) * worldRotation;
        }

        public static Vector3 GetLocalScale(Vector3 worldScale, Vector3 anchorScale)
        {
            return new Vector3(
                anchorScale.x != 0f ? worldScale.x / anchorScale.x : worldScale.x,
                anchorScale.y != 0f ? worldScale.y / anchorScale.y : worldScale.y,
                anchorScale.z != 0f ? worldScale.z / anchorScale.z : worldScale.z);
        }

        public static Vector3 GetWorldPosition(
            Vector3 localPosition,
            Vector3 anchorPosition,
            Quaternion anchorRotation,
            Vector3 anchorScale)
        {
            return anchorPosition + (anchorRotation * Vector3.Scale(localPosition, anchorScale));
        }

        public static Quaternion GetWorldRotation(Quaternion localRotation, Quaternion anchorRotation)
        {
            return anchorRotation * localRotation;
        }

        public static Vector3 GetWorldScale(Vector3 localScale, Vector3 anchorScale)
        {
            return Vector3.Scale(localScale, anchorScale);
        }
    }
}
