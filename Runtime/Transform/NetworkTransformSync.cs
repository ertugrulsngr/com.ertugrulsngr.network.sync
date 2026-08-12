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

        public bool SyncPositionX = true;
        public bool SyncPositionY = true;
        public bool SyncPositionZ = true;
        public bool SyncRotation = true;
        public bool CompressRotation = true;
        public bool SyncScaleX = true;
        public bool SyncScaleY = true;
        public bool SyncScaleZ = true;

        public float PositionThreshold = 0.001f;
        [Range(0f, 180f)] public float RotationAngleThreshold = 0.01f;
        public float ScaleThreshold = 0.01f;
        public bool RelativePosition = true;
        public bool RelativeRotation = true;
        public bool RelativeScale = true;

        [Tooltip("Lerp rendered position toward the interpolated sample (NGO LegacyLerp style).")]
        public bool PositionLerpSmoothing = true;
        [Min(0.001f)]
        [Tooltip("Seconds to close most of the gap to the interpolated position target.")]
        public float PositionMaxInterpolationTime = 0.1f;

        [Tooltip("Slerp rendered rotation toward the interpolated sample.")]
        public bool RotationLerpSmoothing = true;
        [Min(0.001f)]
        [Tooltip("Seconds to close most of the gap to the interpolated rotation target.")]
        public float RotationMaxInterpolationTime = 0.1f;

        [Tooltip("Lerp rendered scale toward the interpolated sample.")]
        public bool ScaleLerpSmoothing = true;
        [Min(0.001f)]
        [Tooltip("Seconds to close most of the gap to the interpolated scale target.")]
        public float ScaleMaxInterpolationTime = 0.1f;

        public INetworkAnchor Anchor { get; set; }

        /// <summary>When true, the next send is marked teleported, then this is cleared.</summary>
        public bool Teleported { get; set; }

        /// <inheritdoc />
        public NetworkBehaviour NetworkBehaviour => this;

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
            int lastRelayedTick = LastSyncedState?.Tick ?? -1;

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

            if (Anchor == null || Anchor.NetworkBehaviour == null)
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
            state.WorldPosition = !RelativePosition;
            state.WorldRotation = !RelativeRotation;
            state.WorldScale = !RelativeScale;

            if (RelativePosition)
            {
                Vector3 positionScale = RelativeScale ? anchorScale : Vector3.one;
                state.Position = AnchoredTransformUtility.GetLocalPosition(worldPosition, anchorPosition, anchorRotation, positionScale);
            }
            else
            {
                state.Position = worldPosition;
            }

            if (RelativeRotation)
            {
                state.Rotation = AnchoredTransformUtility.GetLocalRotation(worldRotation, anchorRotation);
            }
            else
            {
                state.Rotation = worldRotation;
            }

            if (RelativeScale)
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

            if (SyncPositionX &&
                (includeAll ||
                 Mathf.Abs(current.Position.x - LastSyncedState.Value.Position.x) >= PositionThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasPositionX;
            }

            if (SyncPositionY &&
                (includeAll ||
                 Mathf.Abs(current.Position.y - LastSyncedState.Value.Position.y) >= PositionThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasPositionY;
            }

            if (SyncPositionZ &&
                (includeAll ||
                 Mathf.Abs(current.Position.z - LastSyncedState.Value.Position.z) >= PositionThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasPositionZ;
            }

            if (SyncRotation &&
                (includeAll ||
                 Quaternion.Angle(current.Rotation, LastSyncedState.Value.Rotation) >= RotationAngleThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasRotation;
                if (CompressRotation)
                {
                    payload.Flags |= NetworkTransformPayloadFlags.CompressRotation;
                }
            }

            if (SyncScaleX &&
                (includeAll ||
                 Mathf.Abs(current.Scale.x - LastSyncedState.Value.Scale.x) >= ScaleThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasScaleX;
            }

            if (SyncScaleY &&
                (includeAll ||
                 Mathf.Abs(current.Scale.y - LastSyncedState.Value.Scale.y) >= ScaleThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasScaleY;
            }

            if (SyncScaleZ &&
                (includeAll ||
                 Mathf.Abs(current.Scale.z - LastSyncedState.Value.Scale.z) >= ScaleThreshold))
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasScaleZ;
            }

            NetworkBehaviour currentAnchorBehaviour = current.Anchor?.NetworkBehaviour;
            NetworkBehaviour lastAnchorBehaviour = includeAll
                ? null
                : LastSyncedState.Value.Anchor?.NetworkBehaviour;

            if (includeAll || currentAnchorBehaviour != lastAnchorBehaviour)
            {
                payload.Flags |= NetworkTransformPayloadFlags.HasAnchor;
                if (currentAnchorBehaviour != null)
                {
                    payload.AnchorReference = currentAnchorBehaviour;
                }
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
            NetworkTransformState state = LastSyncedState.HasValue
                ? LastSyncedState.Value
                : GetState();

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

            if (payload.HasAnchor)
            {
                state.Anchor = payload.AnchorReference.TryGet(out NetworkBehaviour anchorBehaviour)
                    ? anchorBehaviour as INetworkAnchor
                    : null;
            }

            state.WorldPosition = payload.WorldPosition;
            state.WorldRotation = payload.WorldRotation;
            state.WorldScale = payload.WorldScale;

            return state;
        }

        protected override void ProcessInterpolatedState(ref NetworkTransformState state)
        {
            if (state.Teleported) return;

            state.GetPositionAndRotation(out Vector3 targetPosition, out Quaternion targetRotation);
            Vector3 targetScale = state.GetWorldScale();

            Vector3 position = targetPosition;
            Quaternion rotation = targetRotation;
            Vector3 scale = targetScale;

            if (PositionLerpSmoothing)
            {
                float t = Mathf.Clamp01(Time.deltaTime / PositionMaxInterpolationTime);
                position = Vector3.Lerp(transform.position, targetPosition, t);
            }

            if (RotationLerpSmoothing)
            {
                float t = Mathf.Clamp01(Time.deltaTime / RotationMaxInterpolationTime);
                rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            }

            if (ScaleLerpSmoothing)
            {
                float t = Mathf.Clamp01(Time.deltaTime / ScaleMaxInterpolationTime);
                scale = Vector3.Lerp(transform.localScale, targetScale, t);
            }

            state.Anchor = null;
            state.Position = position;
            state.Rotation = rotation;
            state.Scale = scale;
            state.WorldPosition = true;
            state.WorldRotation = true;
            state.WorldScale = true;
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

            // Carry Teleported from older sample so second-pass smoothing snaps across the jump.
            bool teleported = from.Teleported;

            if (from.Anchor?.NetworkBehaviour == to.Anchor?.NetworkBehaviour)
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
                    WorldScale = to.WorldScale,
                    Teleported = teleported
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
                WorldScale = true,
                Teleported = teleported
            };
        }
    }
}
