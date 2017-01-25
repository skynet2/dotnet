using System;
using System.Collections.Generic;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// How lists should be sorted.
    /// </summary>
    public enum ListResultsOrder
    { 
        /// <summary>
        /// Ascending Order
        /// </summary>
        Ascending,
        
        /// <summary>
        /// Descending Order
        /// </summary>
        Descending
    }

    /*
     * Maybe ... to cut down on deserialization 
    public class ProfileSummary
    {

        DateTime Started { get; set; }
        int TotalDurationMilliseconds { get; set; }
        int SqlDurationMilliseconds { get; set; }
    }
    */
    
    /// <summary>
    /// Provides saving and loading <see cref="MiniProfiler"/>s to a storage medium.
    /// </summary>
    public interface IStorage
    {   
        /// <summary>
        /// Stores <paramref name="profiler"/> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The results of a profiling session.</param>
        /// <remarks>
        /// </remarks>
        void Save(MiniProfiler profiler);
    }
}
