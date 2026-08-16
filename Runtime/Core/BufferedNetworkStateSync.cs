using NetworkSync.Utility;
using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Core
{
    /// <summary>
    /// Network state sync that stores received tick-stamped states in a buffer.
    /// Authority peers send on the network tick. Subclasses decide how buffered states are consumed.
    /// </summary>
    public abstract class BufferedNetworkStateSync<TState, TPayload> : NetworkStateSync<TState, TPayload>
        where TState : struct, ITickStamped
        where TPayload : struct, INetworkSerializable
    {
        /// <summary>Maximum states stored in the network state buffer.</summary>
        [Min(1)]
        [Tooltip("Maximum states stored in the network state buffer.")]
        public int BufferCapacity = 32;

        [Min(0)]
        [Tooltip("Network ticks between sends. 0 = every tick.")]
        public int TicksPerSend;

        private int _forcedStateTick = int.MinValue;
        private int _ticksSinceSend;

        /// <summary>Buffer of received tick-stamped states.</summary>
        protected NetworkStateBuffer<TState> NetworkStateBuffer { get; private set; }

        /// <summary>Integer tick used to stamp outgoing authoritative state.</summary>
        protected virtual int AuthoritativeTick => NetworkSyncManager.Instance.TimeService.ServerReceiveTime.Tick;

        protected virtual void Awake()
        {
            NetworkStateBuffer = new NetworkStateBuffer<TState>(BufferCapacity);
        }

        protected virtual void OnEnable()
        {
            if (IsSpawned)
            {
                RegisterBufferCallbacks();
            }
        }

        protected virtual void OnDisable()
        {
            UnregisterBufferCallbacks();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            RegisterBufferCallbacks();
        }

        protected override void OnSynchronizationComplete()
        {
            base.OnSynchronizationComplete();

            if (!IsLocalAuthority && LastSyncedState.HasValue)
            {
                NetworkStateBuffer.TryAdd(LastSyncedState.Value);
            }
        }

        protected override void OnDespawning()
        {
            UnregisterBufferCallbacks();
            NetworkStateBuffer.Clear();
            _forcedStateTick = int.MinValue;
            _ticksSinceSend = 0;
            base.OnDespawning();
        }

        /// <summary>Called each network tick. Default sends from authority on the configured interval.</summary>
        protected virtual void OnNetworkTick()
        {
            if (!IsSpawned || !IsLocalAuthority) return;

            if (TicksPerSend <= 0)
            {
                SendState();
                return;
            }

            _ticksSinceSend++;
            if (_ticksSinceSend < TicksPerSend) return;

            _ticksSinceSend = 0;
            SendState();
        }

        /// <summary>Forces a state now and ignores buffered states until a newer tick arrives.</summary>
        public void ForceStateUntilNewer(in TState state)
        {
            _forcedStateTick = state.Tick;
            NetworkStateBuffer.Clear();
            NetworkStateBuffer.TryAdd(state);
            SetState(state);
        }

        protected override void OnStateReceived(in TState state)
        {
            if (state.Tick <= _forcedStateTick) return;

            NetworkStateBuffer.TryAdd(state);
        }

        private void RegisterBufferCallbacks()
        {
            UnregisterBufferCallbacks();
            NetworkSyncManager.Instance.TimeService.Tick += OnNetworkTick;
        }

        private void UnregisterBufferCallbacks()
        {
            if (NetworkSyncManager.Instance != null && NetworkSyncManager.Instance.TimeService != null)
            {
                NetworkSyncManager.Instance.TimeService.Tick -= OnNetworkTick;
            }
        }
    }
}
