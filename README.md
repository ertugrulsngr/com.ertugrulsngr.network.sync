# Network Sync

Built on top of [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/), this package provides synchronization components and network services for networked gameplay.

## Quick start

1. Add **Network Sync Manager** to the same GameObject as your **Network Manager**.
2. Add **Network Transform Sync** to a networked GameObject (with a `NetworkObject`).
3. Start a host/server/client session as usual — the transform is synchronized to remotes with interpolation and optional lerp smoothing.

