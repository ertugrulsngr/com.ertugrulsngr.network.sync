using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Core
{
    /// <summary>
    /// Network state sync that buffers received samples and interpolates them on remote peers.
    /// </summary>
    public abstract class InterpolatedNetworkStateSync<TState, TPayload> : BufferedNetworkStateSync<TState, TPayload>
        where TState : struct, ITickStamped
        where TPayload : struct, INetworkSerializable
    {
        [Tooltip("Network update stage used to sample and apply interpolated state.")]
        public NetworkUpdateStage InterpolationStage = NetworkUpdateStage.PostScriptLateUpdate;

        /// <summary>Fractional tick used to sample the interpolation buffer.</summary>
        protected virtual double InterpolationTick => NetworkSyncManager.Instance.TimeService.InterpolationTime.TickWithPartial;

        /// <summary>Interpolates from one state toward another by factor t (0..1).</summary>
        protected abstract TState Interpolate(in TState from, in TState to, float t);

        protected override void OnSynchronizationComplete()
        {
            base.OnSynchronizationComplete();

            if (!IsLocalAuthority && LastSyncedState.HasValue)
            {
                SetState(LastSyncedState.Value);
            }
        }

        protected override int GetRequiredNetworkUpdateStageMask()
        {
            return base.GetRequiredNetworkUpdateStageMask() | GetNetworkUpdateStageBit(InterpolationStage);
        }

        public override void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            base.NetworkUpdate(updateStage);
            if (updateStage == InterpolationStage)
            {
                InterpolateAndApply();
            }
        }

        /// <summary>Applies a state.</summary>
        public abstract override void SetState(in TState state);

        public void InterpolateAndApply()
        {
            if (!IsSpawned || IsLocalAuthority) return;

            if (TryGetInterpolatedState(out TState state))
            {
                ProcessInterpolatedState(ref state);
                SetState(state);
            }
        }

        /// <summary>Optional post-interpolate processing before <see cref="SetState"/></summary>
        protected virtual void ProcessInterpolatedState(ref TState state)
        {
        }

        /// <summary>Gets the interpolated state at <see cref="InterpolationTick"/>, or false if none is available.</summary>
        private bool TryGetInterpolatedState(out TState state)
        {
            if (!NetworkStateBuffer.TrySample(
                    InterpolationTick,
                    out TState from,
                    out TState to,
                    out float alpha))
            {
                state = default;
                return false;
            }

            state = Interpolate(from, to, alpha);
            return true;
        }

    }
}
