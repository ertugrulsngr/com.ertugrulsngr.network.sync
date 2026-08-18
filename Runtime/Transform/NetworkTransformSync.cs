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

        [Tooltip("Lerp rendered position toward the interpolated sample.")]
        public bool SmoothPosition = true;
        [Min(0.001f)]
        [Tooltip("Seconds to close most of the gap to the interpolated position target.")]
        public float PositionSmoothTime = 0.1f;

        [Tooltip("Slerp rendered rotation toward the interpolated sample.")]
        public bool SmoothRotation = true;
        [Min(0.001f)]
        [Tooltip("Seconds to close most of the gap to the interpolated rotation target.")]
        public float RotationSmoothTime = 0.1f;

        [Tooltip("Lerp rendered scale toward the interpolated sample.")]
        public bool SmoothScale = true;
        [Min(0.001f)]
        [Tooltip("Seconds to close most of the gap to the interpolated scale target.")]
        public float ScaleSmoothTime = 0.1f;

        // TODO: Known issue — when the Anchor is another NetworkTransformSync, world pose
        // is built from live transforms. Apply and send on the same update stage have no
        // guaranteed order, so a child can read the parent before that parent has been
        // updated this frame. Anchors that are not NetworkTransformSync (moved in Update
        // or LateUpdate) are fine. This project currently only uses that kind of Anchor.
        public INetworkAnchor Anchor { get; set; }

        /// <summary>When true, authority binds <see cref="Anchor"/> from the network parent.</summary>
        public bool AutoAnchorFromParent = true;

        private NetworkTransformState? _lastSetState;

        /// <summary>When true, the next send is marked teleported, then this is cleared.</summary>
        public bool Teleported { get; set; }

        protected override void OnDespawning()
        {
            Anchor = null;
            _lastSetState = null;
            Teleported = false;
            base.OnDespawning();
        }

        /// <summary>On authority, binds the anchor to the new parent (or clears it when unparented).</summary>
        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            base.OnNetworkObjectParentChanged(parentNetworkObject);

            if (!AutoAnchorFromParent || !IsLocalAuthority) return;

            Anchor = parentNetworkObject != null
                ? parentNetworkObject.GetComponent<INetworkAnchor>()
                : null;
        }

        /// <summary>Clamps and monotonically restamps client tick before reliable relay.</summary>
        protected override bool ServerValidatePayload(ref NetworkTransformPayload payload, ulong senderClientId)
        {
            int serverTick = AuthoritativeTick;
            int tick = payload.Tick;
            int maxAllowedTick = serverTick + MaxClientTickAhead;
            int lastRelayedTick = LastSyncedState?.Tick ?? -1;

            if (tick < serverTick) tick = serverTick;
            
            // This is only true if states are reliable and ordered. If not check must be removed.
            if (tick <= lastRelayedTick) tick = lastRelayedTick + 1;
            if (tick > maxAllowedTick) tick = maxAllowedTick;
                

            payload.Tick = tick;
            return true;
        }

        public override NetworkTransformState GetState()
        {
            transform.GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation);
            Vector3 worldScale = transform.lossyScale;

            NetworkTransformState state = new NetworkTransformState
            {
                Tick = AuthoritativeTick,
                Teleported = Teleported
            };

            if (Anchor == null || Anchor.NetworkBehaviour == null)
            {
                state.Anchor = null;
                state.Position = worldPosition;
                state.Rotation = worldRotation;
                state.Scale = worldScale;
                state.IsWorldPosition = true;
                state.IsWorldRotation = true;
                state.IsWorldScale = true;
                return state;
            }

            Anchor.GetPositionAndRotation(out Vector3 anchorPosition, out Quaternion anchorRotation);
            Vector3 anchorScale = Anchor.GetWorldScale();

            state.Anchor = Anchor;
            state.IsWorldPosition = !RelativePosition;
            state.IsWorldRotation = !RelativeRotation;
            state.IsWorldScale = !RelativeScale;

            if (RelativePosition)
            {
                state.Position = AnchoredTransformUtility.GetLocalPosition(
                    worldPosition, anchorPosition, anchorRotation, anchorScale);
            }
            else
            {
                state.Position = worldPosition;
            }
            state.Rotation = RelativeRotation ? AnchoredTransformUtility.GetLocalRotation(worldRotation, anchorRotation) : worldRotation;
            state.Scale = RelativeScale ? AnchoredTransformUtility.GetLocalScale(worldScale, anchorScale) : worldScale;
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

            if (current.IsWorldPosition)
            {
                payload.Flags |= NetworkTransformPayloadFlags.IsWorldPosition;
            }

            if (current.IsWorldRotation)
            {
                payload.Flags |= NetworkTransformPayloadFlags.IsWorldRotation;
            }

            if (current.IsWorldScale)
            {
                payload.Flags |= NetworkTransformPayloadFlags.IsWorldScale;
            }

            return payload;
        }

        protected override bool ShouldSendPayload(in NetworkTransformPayload payload)
        {
            return payload.HasData;
        }

        protected override NetworkTransformState DecodePayload(in NetworkTransformPayload payload)
        {
            NetworkTransformState state = LastSyncedState ?? GetState();

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

            state.IsWorldPosition = payload.IsWorldPosition;
            state.IsWorldRotation = payload.IsWorldRotation;
            state.IsWorldScale = payload.IsWorldScale;

            return state;
        }

        protected override void ProcessInterpolatedState(ref NetworkTransformState state)
        {
            if (state.Teleported || !_lastSetState.HasValue) return;

            float deltaTime = Time.deltaTime;
            float positionT = SmoothPosition ? Mathf.Clamp01(deltaTime / PositionSmoothTime) : 1f;
            float rotationT = SmoothRotation ? Mathf.Clamp01(deltaTime / RotationSmoothTime) : 1f;
            float scaleT = SmoothScale ? Mathf.Clamp01(deltaTime / ScaleSmoothTime) : 1f;

            // Re-express the last rendered pose in the target's space so smoothing always lerps in
            // one consistent space: filters the network offset without lagging the anchor's motion.
            NetworkTransformState from = ExpressInSpaceOf(_lastSetState.Value, state);
            state = InterpolateState(from, state, positionT, rotationT, scaleT);
        }

        public override void SetState(in NetworkTransformState state)
        {
            state.GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            transform.localScale = state.GetWorldScale();
            _lastSetState = state;
        }

        protected override NetworkTransformState Interpolate(in NetworkTransformState from, in NetworkTransformState to, float t)
        {
            if (to.Teleported) return from;

            NetworkTransformState result = InterpolateState(from, to, t, t, t);
            // Carry Teleported from older sample so second-pass smoothing snaps across the jump.
            result.Teleported = from.Teleported;
            return result;
        }

        #region INetworkAnchor Implementation

        NetworkBehaviour INetworkAnchor.NetworkBehaviour => this;

        void INetworkAnchor.GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation)
        {
            transform.GetPositionAndRotation(out worldPosition, out worldRotation);
        }

        Vector3 INetworkAnchor.GetWorldScale()
        {
            return transform.lossyScale;
        }

        #endregion

        /// <summary>
        /// Lerps between two states, deciding the coordinate space per channel. A channel is lerped in
        /// anchor-local space when both states share the same anchor and both store that channel locally
        /// (so anchor motion is preserved); otherwise that channel is resolved to world space first.
        /// </summary>
        private static NetworkTransformState InterpolateState(
            in NetworkTransformState from,
            in NetworkTransformState to,
            float positionT,
            float rotationT,
            float scaleT)
        {
            bool sameAnchor = from.Anchor?.NetworkBehaviour == to.Anchor?.NetworkBehaviour;
            bool useLocalPosition = sameAnchor && !from.IsWorldPosition && !to.IsWorldPosition;
            bool useLocalRotation = sameAnchor && !from.IsWorldRotation && !to.IsWorldRotation;
            bool useLocalScale = sameAnchor && !from.IsWorldScale && !to.IsWorldScale;

            from.GetPositionAndRotation(out Vector3 fromWorldPosition, out Quaternion fromWorldRotation);
            to.GetPositionAndRotation(out Vector3 toWorldPosition, out Quaternion toWorldRotation);

            Vector3 position = useLocalPosition
                ? Vector3.Lerp(from.Position, to.Position, positionT)
                : Vector3.Lerp(fromWorldPosition, toWorldPosition, positionT);

            Quaternion rotation = useLocalRotation
                ? Quaternion.Slerp(from.Rotation, to.Rotation, rotationT)
                : Quaternion.Slerp(fromWorldRotation, toWorldRotation, rotationT);

            Vector3 scale = useLocalScale
                ? Vector3.Lerp(from.Scale, to.Scale, scaleT)
                : Vector3.Lerp(from.GetWorldScale(), to.GetWorldScale(), scaleT);

            return new NetworkTransformState
            {
                Tick = to.Tick,
                Anchor = to.Anchor,
                Position = position,
                Rotation = rotation,
                Scale = scale,
                IsWorldPosition = !useLocalPosition,
                IsWorldRotation = !useLocalRotation,
                IsWorldScale = !useLocalScale
            };
        }

        /// <summary>
        /// Returns <paramref name="source"/> re-expressed in <paramref name="target"/>'s coordinate space:
        /// the same world pose, described with the target's anchor and world/local flags.
        /// </summary>
        private static NetworkTransformState ExpressInSpaceOf(
            in NetworkTransformState source,
            in NetworkTransformState target)
        {
            source.GetPositionAndRotation(out Vector3 worldPosition, out Quaternion worldRotation);
            Vector3 worldScale = source.GetWorldScale();

            NetworkTransformState result = source;
            result.Anchor = target.Anchor;
            result.IsWorldPosition = target.IsWorldPosition;
            result.IsWorldRotation = target.IsWorldRotation;
            result.IsWorldScale = target.IsWorldScale;

            INetworkAnchor anchor = target.Anchor;
            if (anchor?.NetworkBehaviour == null)
            {
                result.Position = worldPosition;
                result.Rotation = worldRotation;
                result.Scale = worldScale;
                return result;
            }

            anchor.GetPositionAndRotation(out Vector3 anchorPosition, out Quaternion anchorRotation);
            Vector3 anchorScale = anchor.GetWorldScale();

            result.Position = target.IsWorldPosition
                ? worldPosition
                : AnchoredTransformUtility.GetLocalPosition(
                    worldPosition, anchorPosition, anchorRotation, anchorScale);

            result.Rotation = target.IsWorldRotation
                ? worldRotation
                : AnchoredTransformUtility.GetLocalRotation(worldRotation, anchorRotation);

            result.Scale = target.IsWorldScale
                ? worldScale
                : AnchoredTransformUtility.GetLocalScale(worldScale, anchorScale);

            return result;
        }
    }
}
