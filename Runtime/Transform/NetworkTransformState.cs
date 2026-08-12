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
        public bool WorldPosition;
        public bool WorldRotation;
        public bool WorldScale;
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

            if (WorldPosition)
            {
                worldPosition = Position;
            }
            else
            {
                Vector3 anchorScale = WorldScale ? Vector3.one : Anchor.GetWorldScale();
                worldPosition = AnchoredTransformUtility.GetWorldPosition(Position, anchorPosition, anchorRotation, anchorScale);
            }

            if (WorldRotation)
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
            if (Anchor?.NetworkBehaviour != null && !WorldScale)
            {
                return AnchoredTransformUtility.GetWorldScale(Scale, Anchor.GetWorldScale());
            }

            return Scale;
        }
    }
}
