using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Core
{
    /// <summary>
    /// Network state sync that buffers received samples and interpolates on remote peers.
    /// Authority peers send on the network tick; remote peers sample on the interpolation update stage.
    /// </summary>
    public abstract class InterpolatedNetworkStateSync<TState, TPayload> : BufferedNetworkStateSync<TState, TPayload>, INetworkUpdateSystem
        where TState : struct, ITickStamped
        where TPayload : struct, INetworkSerializable
    {
        private NetworkUpdateStage _interpolationStage = NetworkUpdateStage.PostScriptLateUpdate;

        /// <summary>The update stage used for interpolation.</summary>
        public NetworkUpdateStage InterpolationStage
        {
            get => _interpolationStage;
            set
            {
                this.UnregisterNetworkUpdate(_interpolationStage);
                _interpolationStage = value;

                if (IsSpawned && isActiveAndEnabled && value != NetworkUpdateStage.Unset)
                {
                    this.RegisterNetworkUpdate(_interpolationStage);
                }
            }
        }

        /// <summary>Fractional tick used to sample the interpolation buffer.</summary>
        protected virtual double InterpolationTick => NetworkSyncManager.Instance.TimeService.InterpolationTime.TickWithPartial;

        /// <summary>Interpolates from one state toward another by factor t (0..1).</summary>
        protected abstract TState Interpolate(in TState from, in TState to, float t);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (IsSpawned)
            {
                RegisterInterpolationCallback();
            }
        }

        protected override void OnDisable()
        {
            UnregisterInterpolationCallback();
            base.OnDisable();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            RegisterInterpolationCallback();
        }

        protected override void OnSynchronizationComplete()
        {
            base.OnSynchronizationComplete();

            if (!IsLocalAuthority && LastSyncedState.HasValue)
            {
                SetState(LastSyncedState.Value);
            }
        }

        protected override void OnDespawning()
        {
            UnregisterInterpolationCallback();
            base.OnDespawning();
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            InterpolateAndApply();
        }

        private void RegisterInterpolationCallback()
        {
            UnregisterInterpolationCallback();

            if (_interpolationStage != NetworkUpdateStage.Unset)
            {
                this.RegisterNetworkUpdate(_interpolationStage);
            }
        }

        private void UnregisterInterpolationCallback()
        {
            this.UnregisterNetworkUpdate(_interpolationStage);
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
