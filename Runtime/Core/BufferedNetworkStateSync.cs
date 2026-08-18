using NetworkSync.Utility;
using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Core
{
    /// <summary>
    /// Network state sync that stores received tick-stamped states in a buffer.
    /// Authority sends at a tick interval. Subclasses decide how the buffer is consumed.
    /// </summary>
    public abstract class BufferedNetworkStateSync<TState, TPayload> : NetworkStateSync<TState, TPayload>, INetworkUpdateSystem
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

        [Tooltip("Network update stage used to send authoritative state.")]
        public NetworkUpdateStage SendStage = NetworkUpdateStage.PostScriptLateUpdate;

        private int _forcedStateTick = int.MinValue;
        private int _lastSentTick = int.MinValue;
        private int _registeredNetworkUpdateStageMask;

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
            RefreshNetworkUpdateStages();
        }

        protected virtual void OnDisable()
        {
            ClearNetworkUpdateStages();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            RefreshNetworkUpdateStages();
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
            ClearNetworkUpdateStages();
            NetworkStateBuffer.Clear();
            _forcedStateTick = int.MinValue;
            _lastSentTick = int.MinValue;
            base.OnDespawning();
        }

        protected virtual void OnValidate()
        {
            if (!Application.isPlaying) return;
            RefreshNetworkUpdateStages();
        }

        public virtual void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (updateStage == SendStage)
            {
                OnSendStage();
            }
        }

        protected virtual void OnSendStage()
        {
            if (!IsSpawned || !IsLocalAuthority) return;

            int tick = NetworkSyncManager.Instance.TimeService.ServerTime.Tick;
            if (!ShouldSendOnTick(tick)) return;

            _lastSentTick = tick;
            SendState();
        }

        /// <summary>True when enough ticks have elapsed since the last send.</summary>
        protected virtual bool ShouldSendOnTick(int tick)
        {
            if (_lastSentTick == int.MinValue) return true;

            return tick - _lastSentTick >= Mathf.Max(1, TicksPerSend);
        }

        /// <summary>Mask bit for a network update stage.</summary>
        protected static int GetNetworkUpdateStageBit(NetworkUpdateStage stage)
        {
            return stage == NetworkUpdateStage.Unset ? 0 : 1 << (int)stage;
        }

        /// <summary>Stages this component needs update callbacks on.</summary>
        protected virtual int GetRequiredNetworkUpdateStageMask()
        {
            return GetNetworkUpdateStageBit(SendStage);
        }

        /// <summary>Registers the stages currently requested and drops the rest.</summary>
        public void RefreshNetworkUpdateStages()
        {
            ApplyNetworkUpdateStageMask(IsSpawned && isActiveAndEnabled ? GetRequiredNetworkUpdateStageMask() : 0);
        }

        /// <summary>Drops every registered stage.</summary>
        public void ClearNetworkUpdateStages()
        {
            ApplyNetworkUpdateStageMask(0);
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

        private void ApplyNetworkUpdateStageMask(int desired)
        {
            int toRegister = desired & ~_registeredNetworkUpdateStageMask;
            int toUnregister = _registeredNetworkUpdateStageMask & ~desired;

            if (toRegister == 0 && toUnregister == 0) return;

            for (int stage = 1; stage <= (int)NetworkUpdateStage.PostScriptLateUpdate; stage++)
            {
                int bit = 1 << stage;
                if ((toRegister & bit) != 0)
                {
                    this.RegisterNetworkUpdate((NetworkUpdateStage)stage);
                }
                else if ((toUnregister & bit) != 0)
                {
                    this.UnregisterNetworkUpdate((NetworkUpdateStage)stage);
                }
            }

            _registeredNetworkUpdateStageMask = desired;
        }
    }
}
