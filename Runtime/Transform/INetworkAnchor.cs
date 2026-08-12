using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Transform
{
    /// <summary>Provides world-space anchor data for transform synchronization.</summary>
    public interface INetworkAnchor
    {
        /// <summary>Network behaviour of this world-space anchor.</summary>
        NetworkBehaviour NetworkBehaviour { get; }

        /// <summary>Gets world-space position and world-space rotation.</summary>
        void GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation);

        /// <summary>Gets world-space scale.</summary>
        Vector3 GetWorldScale();
    }
}
