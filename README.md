<div align="center">

# Network Sync

[![Network Sync demo](https://img.youtube.com/vi/VCNgMjD5I8Y/maxresdefault.jpg)](https://youtu.be/VCNgMjD5I8Y)

</div>

---

**Network Sync** is a Unity package built on top of [Netcode for GameObjects](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.13/manual/index.html). It provides synchronization components and shared network services for networked gameplay.

The code is written with customization in mind, so behavior can be changed without rewriting the pipeline. Use the ready-made `NetworkTransformSync` for movement, or subclass the generic base classes to synchronize any state type through the same tick-stamped, interpolated pipeline.

It also includes a shared timing and latency system based on the RFC 6298 RTT estimation model, using smoothed RTT and RTT variance like TCP, which keeps remote motion stable as network conditions change.

---

## Features

- **State sync base (`NetworkStateSync`).** The foundation: an encode → send → decode → receive pipeline over RPCs, with authority-based sending and automatic late-join synchronization to the last known state.
- **Interpolated layer (`InterpolatedNetworkStateSync`).** Builds on the base by stamping and sending state on the network tick, and buffering received samples so remotes interpolate between them each frame for smooth motion under latency.
- **Transform sync (`NetworkTransformSync`).** Builds on the interpolated layer for position, rotation, and scale: per-axis control, independent change thresholds, optional quaternion compression, per-channel lerp/slerp smoothing, and a teleport flag to snap when needed. It can sync in world space or relative to an anchor, with or without parenting: an anchor is any networked object implementing `INetworkAnchor`, such as a moving platform, vehicle, or elevator, so relative motion works without Unity transform parenting and stays correct while the anchor itself moves.
- **Timing and latency services.** RFC 6298 smoothed RTT, RTT variance, half-RTT, and tick conversions feed a three-timeline clock for server, send, and interpolation, shared by every sync behaviour.

---

## Quick start

1. Add a **`NetworkManager`** and a **`NetworkSyncManager`** to your scene.
2. Add **`NetworkTransformSync`** to any networked GameObject that has a `NetworkObject`.
3. Start a network session.

The transform is then synchronized to remote peers with tick-stamped interpolation and optional smoothing.

To make an object move relative to another networked object, set its `Anchor`:

```csharp
// Follow an anchor without Unity parenting.
transformSync.Anchor = anchorCandidate.GetComponent<INetworkAnchor>();
```

Or leave `AutoAnchorFromParent` enabled and the authority binds the anchor automatically whenever the network parent changes.

---

## Customization

`NetworkTransformSync` is a thin layer over a generic pipeline you can extend. To synchronize your own state, subclass one of the base classes and override the methods you need.

Common override points:

| Concern | Hooks |
|---------|-------|
| **Authority** | `IsAuthority`, `GetState`, `ShouldSendPayload`, `AuthoritativeTick` |
| **Wire format** | `EncodeState`, `DecodePayload`, `ServerValidatePayload` |
| **Apply path** | `OnStateReceived`, `SetState`, `ProcessInterpolatedState` |
| **Interpolation** | `Interpolate`, `InterpolationTick`, `OnNetworkTick` |

For configuration without code, `NetworkTransformSync` exposes per-axis sync toggles, change thresholds, rotation compression, send rate (`TicksPerSend`), buffer capacity, anchoring, and per-channel smoothing in the inspector.

---

## Architecture

```text
NetworkSyncManager                     scene entry point
├── Latency service                    RFC 6298 smoothed RTT / variance
└── Time service                       ServerTime · ServerReceiveTime · InterpolationTime
        │
NetworkStateSync<TState, TPayload>     encode → send → decode → receive + late-join
    └── InterpolatedNetworkStateSync   tick send + interpolation buffer + remote sampling
            └── NetworkTransformSync   position / rotation / scale · anchors · smoothing
```

- **Latency service:** tracks per-client RTT from the transport and maintains smoothed RTT, variance, and half-RTT. Tunable via `LatencySettings`.
- **Time service:** exposes three timelines updated every `PreUpdate`. **ServerTime** is the shared clock, **ServerReceiveTime** is `ServerTime` plus smoothed RTT used to stamp outgoing state, and **InterpolationTime** is `ServerTime` plus `InterpolationDelayTicks` used to sample the buffer slightly in the past for smoother remote motion. Tunable via `TimingSettings`.
- **Sync stack:** authority stamps and sends on the network tick; remotes buffer tick-stamped samples and interpolate each frame, then optionally smooth toward the target.
