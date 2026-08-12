using NetworkSync.Core;
using NetworkSync.Utility;
using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Transform
{
    [GenerateSerializationForType(typeof(NetworkTransformPayload))]
    public class NetworkTransformSync : InterpolatedNetworkStateSync<NetworkTransformState, NetworkTransformPayload>, INetworkAnchor
    {
        /// <summary>How many ticks ahead of authoritative time a client stamp may stay on relay.</summary>
        public const int MaxClientTickAhead = 2;

        [SerializeField] private bool _syncPositionX = true;
        [SerializeField] private bool _syncPositionY = true;
        [SerializeField] private bool _syncPositionZ = true;
        [SerializeField] private bool _syncRotation = true;
        [SerializeField] private bool _compressRotation = true;
        [SerializeField] private bool _syncScaleX = true;
        [SerializeField] private bool _syncScaleY = true;
        [SerializeField] private bool _syncScaleZ = true;
        
        [SerializeField] private float _positionThreshold = 0.001f;
        [SerializeField, Range(0f, 180f)] private float _rotationAngleThreshold = 0.01f;
        [SerializeField] private float _scaleThreshold = 0.01f;
        [SerializeField] private bool _relativePosition = true;
        [SerializeField] private bool _relativeRotation = true;
        [SerializeField] private bool _relativeScale = true;

        

        public INetworkAnchor Anchor { get; set; }

        /// <summary>When true, the next send is marked teleported, then this is cleared.</summary>
        public bool Teleported { get; set; }

        public void GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation)
        {
            transform.GetPositionAndRotation(out worldPosition, out worldRotation);
        }

        public Vector3 GetWorldScale()
        {
            return transform.lossyScale;
        }

        /// <summary>Clamps and monotonically restamps client tick before reliable relay.</summary>
        protected override bool ServerValidatePayload(ref NetworkTransformPayload payload, ulong senderClientId)
        {
            int serverTick = SendTick;
            int tick = payload.Tick;
            int maxAllowedTick = serverTick + MaxClientTickAhead;
            int lastRelayedTick = LastSyncedState.HasValue ? LastSyncedState.Value.Tick : -1;

            if (tick < serverTick) tick = serverTick;
            if (tick <= lastRelayedTick) tick = lastRelayedTick + 1;
            if (tick > maxAllowedTick) tick = maxAllowedTick;
                

            payload.Tick = tick;
            return true;
        }

        protected override NetworkTransformState GetState()
        {
            GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation);
            Vector3 worldScale = GetWorldScale();

            NetworkTransformState state = new NetworkTransformState
            {
                Tick = SendTick,
                Teleported = Teleported
            };

            if (Anchor == null || Anchor.NetworkObject == null)
            {
                state.Anchor = null;
                state.Position = worldPosition;
                state.Rotation = worldRotation;
                state.Scale = worldScale;
                state.WorldPosition = true;
                state.WorldRotation = true;
                state.WorldScale = true;
                return state;
            }

            Anchor.GetPositionAndRotation(out Vector3 anchorPosition, out Quaternion anchorRotation);
            Vector3 anchorScale = Anchor.GetWorldScale();

            state.Anchor = Anchor;
            state.WorldPosition = !_relativePosition;
            state.WorldRotation = !_relativeRotation;
            state.WorldScale = !_relativeScale;

            if (_relativePosition)
            {
                Vector3 positionScale = _relativeScale ? anchorScale : Vector3.one;
                state.Position = AnchoredTransformUtility.GetLocalPosition(worldPosition, anchorPosition, anchorRotation, positionScale);
            }
            else
            {
                state.Position = worldPosition;
            }

            if (_relativeRotation)
            {
                state.Rotation = AnchoredTransformUtility.GetLocalRotation(worldRotation, anchorRotation);
            }
            else
            {
                state.Rotation = worldRotation;
            }

            if (_relativeScale)
            {
                state.Scale = AnchoredTransformUtility.GetLocalScale(worldScale, anchorScale);
            }
            else
            {
                state.Scale = worldScale;
            }

            return state;
        }

        protected override NetworkTransformPayload EncodeState(in NetworkTransformState current, bool forSynchronize = false)
        {
            NetworkTransformPayload payload = new NetworkTransformPayload
            {
                Tick = current.Tick,
                Position = current.Position,
                Rotation = current.Rotation,
                Scale = current.Scale
            };

            bool includeAll = forSynchronize || !LastSyncedState.HasValue;

            if (_syncPositionX &&
                (includeAll ||
                 Mathf.Abs(current.Position.x - LastSyncedState.Value.Position.x) >= _positionThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasPositionX;
            }

            if (_syncPositionY &&
                (includeAll ||
                 Mathf.Abs(current.Position.y - LastSyncedState.Value.Position.y) >= _positionThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasPositionY;
            }

            if (_syncPositionZ &&
                (includeAll ||
                 Mathf.Abs(current.Position.z - LastSyncedState.Value.Position.z) >= _positionThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasPositionZ;
            }

            if (_syncRotation &&
                (includeAll ||
                 Quaternion.Angle(current.Rotation, LastSyncedState.Value.Rotation) >= _rotationAngleThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasRotation;
                if (_compressRotation)
                {
                    payload.Flags |= NetworkTransformPayloadFlags.CompressRotation;
                }
            }

            if (_syncScaleX &&
                (includeAll ||
                 Mathf.Abs(current.Scale.x - LastSyncedState.Value.Scale.x) >= _scaleThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasScaleX;
            }

            if (_syncScaleY &&
                (includeAll ||
                 Mathf.Abs(current.Scale.y - LastSyncedState.Value.Scale.y) >= _scaleThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasScaleY;
            }

            if (_syncScaleZ &&
                (includeAll ||
                 Mathf.Abs(current.Scale.z - LastSyncedState.Value.Scale.z) >= _scaleThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasScaleZ;
            }

            if (current.Anchor != null && current.Anchor.NetworkObject != null)
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasAnchor;
                payload.AnchorReference = current.Anchor.NetworkObject;
            }

            if (current.Teleported)
            {
                payload.Flags |= NetworkTransformPayloadFlags.Teleported;
                Teleported = false;
            }

            if (current.WorldPosition)
            {
                payload.Flags |= NetworkTransformPayloadFlags.WorldPosition;
            }

            if (current.WorldRotation)
            {
                payload.Flags |= NetworkTransformPayloadFlags.WorldRotation;
            }

            if (current.WorldScale)
            {
                payload.Flags |= NetworkTransformPayloadFlags.WorldScale;
            }

            return payload;
        }

        protected override bool ShouldSendPayload(in NetworkTransformPayload payload)
        {
            return payload.HasData;
        }

        protected override NetworkTransformState DecodePayload(in NetworkTransformPayload payload)
        {
            NetworkTransformState state = default;
            if (LastSyncedState.HasValue)
            {
                state = LastSyncedState.Value;
            }
            state.Tick = payload.Tick;
            state.Teleported = payload.Teleported;

            Vector3 position = state.Position;
            if (payload.HasPositionX) position.x = payload.Position.x;
            if (payload.HasPositionY) position.y = payload.Position.y;
            if (payload.HasPositionZ) position.z = payload.Position.z;
            state.Position = position;

            if (payload.HasRotation)
            {
                state.Rotation = payload.Rotation;
            }

            Vector3 scale = state.Scale;
            if (payload.HasScaleX) scale.x = payload.Scale.x;
            if (payload.HasScaleY) scale.y = payload.Scale.y;
            if (payload.HasScaleZ) scale.z = payload.Scale.z;
            state.Scale = scale;

            if (payload.HasAnchor && payload.AnchorReference.TryGet(out NetworkObject networkObject, NetworkManager))
            {
                state.Anchor = networkObject.GetComponent<INetworkAnchor>();
            }

            state.WorldPosition = payload.WorldPosition;
            state.WorldRotation = payload.WorldRotation;
            state.WorldScale = payload.WorldScale;

            return state;
        }

        protected override void SetState(in NetworkTransformState state)
        {
            state.GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            transform.localScale = state.GetWorldScale();
        }

        protected override NetworkTransformState Interpolate(in NetworkTransformState from, in NetworkTransformState to, float t)
        {
            if (to.Teleported) return from;

            if (from.Anchor?.NetworkObject == to.Anchor?.NetworkObject)
            {
                return new NetworkTransformState
                {
                    Tick = to.Tick,
                    Anchor = from.Anchor,
                    Position = Vector3.Lerp(from.Position, to.Position, t),
                    Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t),
                    Scale = Vector3.Lerp(from.Scale, to.Scale, t),
                    WorldPosition = to.WorldPosition,
                    WorldRotation = to.WorldRotation,
                    WorldScale = to.WorldScale
                };
            }

            from.GetPositionAndRotation(out Vector3 fromWorldPosition, out Quaternion fromWorldRotation);
            to.GetPositionAndRotation(out Vector3 toWorldPosition, out Quaternion toWorldRotation);

            return new NetworkTransformState
            {
                Tick = to.Tick,
                Anchor = null,
                Position = Vector3.Lerp(fromWorldPosition, toWorldPosition, t),
                Rotation = Quaternion.Slerp(fromWorldRotation, toWorldRotation, t),
                Scale = Vector3.Lerp(from.GetWorldScale(), to.GetWorldScale(), t),
                WorldPosition = true,
                WorldRotation = true,
                WorldScale = true
            };
        }

#if UNITY_EDITOR
        public static class PropertyNames
        {
            public const string SyncPositionX = nameof(_syncPositionX);
            public const string SyncPositionY = nameof(_syncPositionY);
            public const string SyncPositionZ = nameof(_syncPositionZ);
            public const string SyncRotation = nameof(_syncRotation);
            public const string CompressRotation = nameof(_compressRotation);
            public const string SyncScaleX = nameof(_syncScaleX);
            public const string SyncScaleY = nameof(_syncScaleY);
            public const string SyncScaleZ = nameof(_syncScaleZ);
            public const string PositionThreshold = nameof(_positionThreshold);
            public const string RotationAngleThreshold = nameof(_rotationAngleThreshold);
            public const string ScaleThreshold = nameof(_scaleThreshold);
            public const string RelativePosition = nameof(_relativePosition);
            public const string RelativeRotation = nameof(_relativeRotation);
            public const string RelativeScale = nameof(_relativeScale);
        }
#endif
    }
}
