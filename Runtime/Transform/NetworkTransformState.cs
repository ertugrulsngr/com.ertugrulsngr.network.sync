using NetworkSync.Core;
using NetworkSync.Utility;
using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Transform
{
    public struct NetworkTransformState : ITickStamped
    {
        public int Tick { get; set; }

        public INetworkAnchor Anchor;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool IsWorldPosition;
        public bool IsWorldRotation;
        public bool IsWorldScale;
        public bool Teleported;

        /// <summary>Gets world-space position and world-space rotation.</summary>
        public readonly void GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation)
        {
            if (Anchor?.NetworkBehaviour == null)
            {
                worldPosition = Position;
                worldRotation = Rotation;
                return;
            }

            Anchor.GetPositionAndRotation(out Vector3 anchorPosition, out Quaternion anchorRotation);

            if (IsWorldPosition)
            {
                worldPosition = Position;
            }
            else
            {
                worldPosition = AnchoredTransformUtility.GetWorldPosition(
                    Position, anchorPosition, anchorRotation, Anchor.GetWorldScale());
            }

            if (IsWorldRotation)
            {
                worldRotation = Rotation;
            }
            else
            {
                worldRotation = AnchoredTransformUtility.GetWorldRotation(Rotation, anchorRotation);
            }
        }

        /// <summary>Gets world-space scale.</summary>
        public readonly Vector3 GetWorldScale()
        {
            if (Anchor?.NetworkBehaviour != null && !IsWorldScale)
            {
                return AnchoredTransformUtility.GetWorldScale(Scale, Anchor.GetWorldScale());
            }

            return Scale;
        }
    }
}
