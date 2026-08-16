using System;
using System.Collections.Generic;
using NetworkSync.Core;

namespace NetworkSync.Utility
{
    /// <summary>
    /// Fixed-capacity buffer of tick-stamped network states, sorted by tick.
    /// The same tick overwrites; when full, the oldest state is dropped.
    /// </summary>
    public sealed class NetworkStateBuffer<T> where T : struct, ITickStamped
    {
        private readonly List<T> _items;
        private readonly int _capacity;

        public NetworkStateBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
            _items = new List<T>(capacity);
        }

        /// <summary>Number of stored states.</summary>
        public int Count => _items.Count;

        /// <summary>Maximum number of states.</summary>
        public int Capacity => _capacity;

        /// <summary>State at the given index in tick order.</summary>
        public T this[int index] => _items[index];

        /// <summary>
        /// Inserts or overwrites a state by tick.
        /// Returns false if the buffer is full and the state is older than all stored states.
        /// </summary>
        public bool TryAdd(in T state)
        {
            int index = BinarySearch(state.Tick);

            if (index >= 0)
            {
                _items[index] = state;
                return true;
            }

            index = ~index;

            if (_items.Count == _capacity)
            {
                if (index == 0) return false;

                _items.RemoveAt(0);
                index--;
            }

            _items.Insert(index, state);
            return true;
        }

        /// <summary>Gets the state at an exact tick.</summary>
        public bool TryGet(int tick, out T state)
        {
            int index = BinarySearch(tick);

            if (index >= 0)
            {
                state = _items[index];
                return true;
            }

            state = default;
            return false;
        }

        /// <summary>
        /// Samples the buffer at <paramref name="tick"/>.
        /// Returns the surrounding states and an alpha in 0..1.
        /// Clamps to the first or last state when outside the buffered range.
        /// </summary>
        public bool TrySample(
            double tick,
            out T from,
            out T to,
            out float alpha)
        {
            from = default;
            to = default;
            alpha = 0f;

            if (_items.Count == 0) return false;

            int index = BinarySearch((int)Math.Floor(tick));

            if (index >= 0)
            {
                from = _items[index];
                if (index + 1 < _items.Count)
                {
                    to = _items[index + 1];
                    alpha = GetSampleAlpha(tick, from.Tick, to.Tick);
                }
                else
                {
                    to = from;
                }

                return true;
            }

            int insertAt = ~index;

            if (insertAt == 0)
            {
                from = to = _items[0];
                return true;
            }

            from = _items[insertAt - 1];
            if (insertAt < _items.Count)
            {
                to = _items[insertAt];
                alpha = GetSampleAlpha(tick, from.Tick, to.Tick);
            }
            else
            {
                to = from;
            }

            return true;
        }

        /// <summary>Removes all states.</summary>
        public void Clear() => _items.Clear();

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

        private static float GetSampleAlpha(double tick, int fromTick, int toTick)
        {
            int span = toTick - fromTick;
            return span > 0 ? (float)((tick - fromTick) / span) : 0f;
        }
    }
}
