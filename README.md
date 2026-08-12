# Network Sync

Built on top of [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/), this package provides synchronization components and shared network services for networked gameplay.

## Quick start

1. Add **Network Sync Manager** to the same GameObject as your **Network Manager**.
2. Add **Network Transform Sync** to a networked GameObject (with a `NetworkObject`).
3. Start a host/server/client session as usual — the transform is synchronized to remotes with interpolation and optional lerp smoothing.

Place only one `NetworkSyncManager` in the scene.

---

## Architecture

```text
NetworkSyncManager
├── Latency service
└── Time service
        │
NetworkStateSync            (encode / send / decode / receive)
    └── InterpolatedNetworkStateSync   (tick send + buffer + remote interpolate)
            └── NetworkTransformSync   (transform deltas, anchors, lerp smoothing)
```

### Latency service

Tracks per-client round-trip time from the transport and maintains smoothed latency metrics.

Smoothing follows **RFC 6298–style** RTT estimation (the common internet standard approach used for TCP-style RTT / RTTVAR):

- Latest RTT from the transport
- Smoothed RTT
- RTT variance
- Half-RTT and tick conversions for timing

Tunable via `LatencySettings` on `NetworkSyncManager` (sample interval, smoothing factors).

### Time service

Exposes **three** related timelines, updated each PreUpdate from restored server time and local latency:

| Time | What it is | How it is built |
|------|------------|-----------------|
| **ServerTime** | Current server timeline (unbuffered on clients) | NGO `ServerTime`, plus NGO’s default client safety buffer (`0.05s`) on clients so the clock matches real server time more closely |
| **ServerReceiveTime** | Estimated server tick when a payload sent *now* would arrive | `ServerTime` + smoothed RTT (in ticks) |
| **InterpolationTime** | Time used to sample the interpolation buffer on remotes | `ServerTime` + `InterpolationDelayTicks` (default **−2**, so remotes render slightly in the past) |

Authority sync behaviours stamp outgoing state with **ServerReceiveTime** (`SendTick`). Remotes sample with **InterpolationTime**.

The service also raises a **Tick** event as the server timeline advances; interpolated sync uses that to send on the authority.

### Sync stack

#### `NetworkStateSync<TState, TPayload>`

Base synchronization pipeline:

1. Authority: `GetState` → `EncodeState` → optional `ShouldSendPayload` → RPC
2. Remote / relay: decode → `OnStateReceived` → (by default) `SetState`
3. Late join via `OnSynchronize`

Override virtuals to change authority rules, encoding, decoding, apply behaviour, or send gating.

#### `InterpolatedNetworkStateSync<TState, TPayload>`

Adds tick-stamped buffering and remote interpolation on top of the base:

- Authority sends on `TimeService.Tick` (respecting `TicksPerSend`)
- Remotes push received states into an `InterpolationBuffer`
- Each frame: sample at `InterpolationTick` → `Interpolate` → optional `ProcessInterpolatedState` → `SetState`

Key hooks: `SendTick`, `InterpolationTick`, `Interpolate`, `ProcessInterpolatedState`, `OnNetworkTick`.

#### `NetworkTransformSync`

Transform sync built on the interpolated layer:

- Position / rotation / scale with per-axis sync and thresholds (delta-style payloads)
- Optional compressed rotation, anchors, teleport flag
- Remote second-pass lerp smoothing toward the interpolated sample

Customize by subclassing and overriding the same virtual methods (for example `GetState`, `EncodeState`, `Interpolate`, `ProcessInterpolatedState`).

---

## Extensibility

The stack is designed so behaviour is easy to change without rewriting the pipeline:

- Authority: `IsAuthority`, `GetState`, `ShouldSendPayload`, `SendTick`
- Wire format: `EncodeState`, `DecodePayload`, `ServerValidatePayload`
- Apply path: `OnStateReceived`, `SetState`, `ProcessInterpolatedState`
- Interpolation: `Interpolate`, `InterpolationTick`, `OnNetworkTick`

Start from `NetworkTransformSync` for transforms, or from `InterpolatedNetworkStateSync` / `NetworkStateSync` for custom state types.
