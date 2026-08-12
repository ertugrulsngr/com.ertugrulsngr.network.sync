using System;
using System.Collections.Generic;

namespace NetworkSync.Core
{
    /// <summary>
    /// Fixed-capacity buffer of tick-stamped samples, sorted by tick.
    /// Same tick overwrites; when full, the oldest sample is dropped.
    /// </summary>
    public sealed class InterpolationBuffer<T> where T : struct, ITickStamped
    {
        private readonly List<T> _items;
        private readonly int _capacity;

        public InterpolationBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
            _items = new List<T>(capacity);
        }

        /// <summary>Number of stored samples.</summary>
        public int Count => _items.Count;

        /// <summary>Maximum number of samples.</summary>
        public int Capacity => _capacity;

        /// <summary>Sample at the given index in tick order.</summary>
        public T this[int index] => _items[index];

        /// <summary>
        /// Inserts or overwrites a sample by tick.
        /// Returns false if the buffer is full and the sample is older than all stored samples.
        /// </summary>
        public bool TryAdd(in T sample)
        {
            // Find by tick: >= 0 means found, otherwise ~index is the insert position.
            int index = BinarySearch(sample.Tick);

            // Same tick already stored — overwrite it.
            if (index >= 0)
            {
                _items[index] = sample;
                return true;
            }

            // BinarySearch miss: flip bits to get where this tick should be inserted.
            index = ~index;

            // Buffer is full — make room or reject.
            if (_items.Count == _capacity)
            {
                // New sample is older than everything — discard it.
                if (index == 0) return false;

                // Drop the oldest sample, then shift the insert index down.
                _items.RemoveAt(0);
                index--;
            }

            // Insert in tick order.
            _items.Insert(index, sample);
            return true;
        }

        /// <summary>Gets the sample at an exact tick.</summary>
        public bool TryGet(int tick, out T sample)
        {
            int index = BinarySearch(tick);

            if (index >= 0)
            {
                sample = _items[index];
                return true;
            }

            sample = default;
            return false;
        }

        /// <summary>
        /// Gets the samples around <paramref name="interpolationTick"/> and the blend factor (0..1).
        /// Clamps to the first or last sample when outside the buffered range.
        /// </summary>
        public bool TryGetInterpolationPair(
            double interpolationTick,
            out T older,
            out T newer,
            out float blendFactor)
        {
            older = default;
            newer = default;
            blendFactor = 0f;

            if (_items.Count == 0) return false;

            // Search by floor(interpolationTick) to locate the lower sample / insert point.
            int index = BinarySearch((int)Math.Floor(interpolationTick));

            if (index >= 0)
            {
                // Exact floor tick exists — blend toward the next sample if any.
                older = _items[index];
                if (index + 1 < _items.Count)
                {
                    newer = _items[index + 1];
                    blendFactor = GetBlendFactor(interpolationTick, older.Tick, newer.Tick);
                }
                else
                {
                    newer = older;
                }

                return true;
            }

            int insertAt = ~index;

            // Before the first sample — clamp.
            if (insertAt == 0)
            {
                older = newer = _items[0];
                return true;
            }

            // Between (insertAt - 1) and insertAt, or after the last sample.
            older = _items[insertAt - 1];
            if (insertAt < _items.Count)
            {
                newer = _items[insertAt];
                blendFactor = GetBlendFactor(interpolationTick, older.Tick, newer.Tick);
            }
            else
            {
                newer = older;
            }

            return true;
        }

        /// <summary>Removes all samples.</summary>
        public void Clear() => _items.Clear();

        /// <summary>
        /// Finds the index of <paramref name="tick"/> in tick order.
        /// Returns the index when present; otherwise returns the bitwise complement of where it would be inserted.
        /// </summary>
        private int BinarySearch(int tick)
        {
            int lo = 0;
            int hi = _items.Count - 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int midTick = _items[mid].Tick;

                if (midTick == tick) return mid;

                if (midTick < tick)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return ~lo;
        }

        // How far interpolationTick sits between older and newer (0 = at older, 1 = at newer).
        private static float GetBlendFactor(double interpolationTick, int olderTick, int newerTick)
        {
            int span = newerTick - olderTick;
            return span > 0 ? (float)((interpolationTick - olderTick) / span) : 0f;
        }
    }
}
