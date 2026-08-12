namespace NetworkSync.Core
{
    /// <summary>A value stamped with a simulation tick.</summary>
    public interface ITickStamped
    {
        /// <summary>Simulation tick of this value.</summary>
        int Tick { get; }
    }
}
