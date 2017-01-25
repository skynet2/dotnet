using System;
using System.Runtime.Serialization;

namespace StackExchange.Profiling
{
    /// <summary>
    /// An individual suppression block that deactivates profiling temporarily
    /// </summary>
    [DataContract]
    public class Suppression : IDisposable
    {
        private readonly bool _wasSuppressed;

        /// <summary>
        /// Initialises a new instance of the <see cref="Suppression"/> class. 
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public Suppression(bool wasSuppressed)
        {
            _wasSuppressed = wasSuppressed;
        }

        /// <summary>
        /// Gets a reference to the containing profiler, allowing this Suppression to affect profiler activity.
        /// </summary>
        internal MiniProfiler Profiler { get; }

        void IDisposable.Dispose()
        {
            if(Profiler != null && _wasSuppressed)
            {
                Profiler.IsActive = true;
            }
        }
    }
}