using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Core
{
    /// <summary>
    /// Network state sync that buffers received samples and interpolates on remote peers.
    /// Authority peers send on the network tick; remote peers sample on the interpolation update stage.
    /// </summary>
    public abstract class InterpolatedNetworkStateSync<TState, TPayload> : NetworkStateSync<TState, TPayload>, INetworkUpdateSystem
        where TState : struct, ITickStamped
        where TPayload : struct, INetworkSerializable
    {
        /// <summary>Maximum samples stored in the interpolation buffer.</summary>
        protected int BufferCapacity = 32;

        [SerializeField, Min(0)]
        [Tooltip("Network ticks between sends. 0 = every tick.")]
        private int _ticksPerSend;

        private int _forcedStateTick = int.MinValue;
        private int _ticksSinceSend;
        private NetworkUpdateStage _interpolationStage = NetworkUpdateStage.Update;

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

        /// <summary>Buffer of received tick-stamped states.</summary>
        protected InterpolationBuffer<TState> Buffer { get; private set; }

        /// <summary>Integer tick used to stamp outgoing authoritative state.</summary>
        protected virtual int SendTick => NetworkSyncManager.Instance.TimeService.ServerTime.Tick;

        /// <summary>Fractional tick used to sample the interpolation buffer.</summary>
        protected virtual double InterpolationTick => NetworkSyncManager.Instance.TimeService.InterpolationTime.TickWithPartial;

        /// <summary>Interpolates from one state toward another by factor t (0..1).</summary>
        protected abstract TState Interpolate(in TState from, in TState to, float t);

        protected virtual void Awake()
        {
            Buffer = new InterpolationBuffer<TState>(BufferCapacity);
        }

        private void OnEnable()
        {
            if (IsSpawned)
            {
                RegisterNetworkCallbacks();
            }
        }

        private void OnDisable()
        {
            UnregisterNetworkCallbacks();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsLocalAuthority && LastSyncedState.HasValue)
            {
                Buffer.TryAdd(LastSyncedState.Value);
                SetState(LastSyncedState.Value);
            }

            RegisterNetworkCallbacks();
        }

        public override void OnNetworkDespawn()
        {
            UnregisterNetworkCallbacks();
            base.OnNetworkDespawn();
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            SampleAndApply();
        }

        /// <summary>Called each network tick. Default sends from authority on the configured interval.</summary>
        protected virtual void OnNetworkTick()
        {
            if (!IsSpawned || !IsLocalAuthority) return;

            if (_ticksPerSend <= 0)
            {
                SendState();
                return;
            }

            _ticksSinceSend++;
            if (_ticksSinceSend < _ticksPerSend) return;

            _ticksSinceSend = 0;
            SendState();
        }

        private void RegisterNetworkCallbacks()
        {
            UnregisterNetworkCallbacks();

            if (_interpolationStage != NetworkUpdateStage.Unset)
            {
                this.RegisterNetworkUpdate(_interpolationStage);
            }

            NetworkSyncManager.Instance.TimeService.Tick += OnNetworkTick;
        }

        private void UnregisterNetworkCallbacks()
        {
            this.UnregisterNetworkUpdate(_interpolationStage);

            if (NetworkSyncManager.Instance == null || NetworkSyncManager.Instance.TimeService == null) return;
            NetworkSyncManager.Instance.TimeService.Tick -= OnNetworkTick;
        }

        /// <summary>Applies a state.</summary>
        protected abstract override void SetState(in TState state);

        /// <summary>Forces a state now and ignores buffered states until a newer tick arrives.</summary>
        public void ForceStateUntilNewer(in TState state)
        {
            _forcedStateTick = state.Tick;
            Buffer.Clear();
            Buffer.TryAdd(state);
            SetState(state);
        }

        protected override void OnStateReceived(in TState state)
        {
            if (state.Tick <= _forcedStateTick) return;

            Buffer.TryAdd(state);
        }

        private void SampleAndApply()
        {
            if (!IsSpawned || IsLocalAuthority) return;

            if (TrySample(out TState state))
            {
                SetState(state);
            }
        }

        /// <summary>Samples the buffer at <see cref="InterpolationTick"/> and blends the pair.</summary>
        protected bool TrySample(out TState state)
        {
            if (!Buffer.TryGetInterpolationPair(
                    InterpolationTick,
                    out TState older,
                    out TState newer,
                    out float blendFactor))
            {
                state = default;
                return false;
            }

            state = Interpolate(older, newer, blendFactor);
            return true;
        }
    }
}
