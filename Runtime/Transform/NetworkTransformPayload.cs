using System;
using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Transform
{
    [Flags]
    public enum NetworkTransformPayloadFlags : ushort
    {
        None = 0,

        IsWorldPosition = 1 << 0,
        IsWorldRotation = 1 << 1,
        IsWorldScale = 1 << 2,

        HasAnchor = 1 << 3,

        HasPositionX = 1 << 4,
        HasPositionY = 1 << 5,
        HasPositionZ = 1 << 6,

        HasRotation = 1 << 7,
        CompressRotation = 1 << 8,

        HasScaleX = 1 << 9,
        HasScaleY = 1 << 10,
        HasScaleZ = 1 << 11,

        Teleported = 1 << 12,

        IsWorldAll = IsWorldPosition | IsWorldRotation | IsWorldScale,
        HasPosition = HasPositionX | HasPositionY | HasPositionZ,
        HasScale = HasScaleX | HasScaleY | HasScaleZ
    }

    public struct NetworkTransformPayload : INetworkSerializable
    {
        public int Tick;
        public NetworkTransformPayloadFlags Flags;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public NetworkBehaviourReference AnchorReference;

        public bool IsWorldPosition => (Flags & NetworkTransformPayloadFlags.IsWorldPosition) != 0;
        public bool IsWorldRotation => (Flags & NetworkTransformPayloadFlags.IsWorldRotation) != 0;
        public bool IsWorldScale => (Flags & NetworkTransformPayloadFlags.IsWorldScale) != 0;
        public bool HasAnchor => (Flags & NetworkTransformPayloadFlags.HasAnchor) != 0;
        public bool HasPositionX => (Flags & NetworkTransformPayloadFlags.HasPositionX) != 0;
        public bool HasPositionY => (Flags & NetworkTransformPayloadFlags.HasPositionY) != 0;
        public bool HasPositionZ => (Flags & NetworkTransformPayloadFlags.HasPositionZ) != 0;
        public bool HasRotation => (Flags & NetworkTransformPayloadFlags.HasRotation) != 0;
        public bool CompressRotation => (Flags & NetworkTransformPayloadFlags.CompressRotation) != 0;
        public bool HasScaleX => (Flags & NetworkTransformPayloadFlags.HasScaleX) != 0;
        public bool HasScaleY => (Flags & NetworkTransformPayloadFlags.HasScaleY) != 0;
        public bool HasScaleZ => (Flags & NetworkTransformPayloadFlags.HasScaleZ) != 0;
        public bool Teleported => (Flags & NetworkTransformPayloadFlags.Teleported) != 0;
        public bool HasPosition => (Flags & NetworkTransformPayloadFlags.HasPosition) == NetworkTransformPayloadFlags.HasPosition;
        public bool HasScale => (Flags & NetworkTransformPayloadFlags.HasScale) == NetworkTransformPayloadFlags.HasScale;

        public bool HasData => (Flags & (
            NetworkTransformPayloadFlags.HasPosition |
            NetworkTransformPayloadFlags.HasRotation |
            NetworkTransformPayloadFlags.HasScale |
            NetworkTransformPayloadFlags.HasAnchor |
            NetworkTransformPayloadFlags.Teleported)) != 0;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();
                BytePacker.WriteValueBitPacked(writer, Tick);
                BytePacker.WriteValueBitPacked(writer, (ushort)Flags);
            }
            else
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                ByteUnpacker.ReadValueBitPacked(reader, out Tick);
                ByteUnpacker.ReadValueBitPacked(reader, out ushort flags);
                Flags = (NetworkTransformPayloadFlags)flags;
            }

            if (HasPositionX) serializer.SerializeValue(ref Position.x);
            if (HasPositionY) serializer.SerializeValue(ref Position.y);
            if (HasPositionZ) serializer.SerializeValue(ref Position.z);

            if (HasRotation)
            {
                if (CompressRotation)
                {
                    if (serializer.IsWriter)
                    {
                        uint compressed = QuaternionCompressor.CompressQuaternion(ref Rotation);
                        serializer.SerializeValue(ref compressed);
                    }
                    else
                    {
                        uint compressed = 0;
                        serializer.SerializeValue(ref compressed);
                        QuaternionCompressor.DecompressQuaternion(ref Rotation, compressed);
                    }
                }
                else
                {
                    serializer.SerializeValue(ref Rotation);
                }
            }

            if (HasScaleX) serializer.SerializeValue(ref Scale.x);
            if (HasScaleY) serializer.SerializeValue(ref Scale.y);
            if (HasScaleZ) serializer.SerializeValue(ref Scale.z);

            if (HasAnchor) serializer.SerializeValue(ref AnchorReference);
        }
    }
}
