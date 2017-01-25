using System;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Contains helper methods that ease working with null <see cref="MiniProfiler"/>s.
    /// </summary>
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <returns>the profile step</returns>
        public static IDisposable Step(this MiniProfiler profiler, string name)
        {
            return profiler?.StepImpl(name);
        }

        /// <summary>
        /// Returns a new <see cref="CustomTiming"/> that will automatically set its <see cref="Profiling.CustomTiming.StartMilliseconds"/>
        /// and <see cref="Profiling.CustomTiming.DurationMilliseconds"/>
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="category">The category under which this timing will be recorded.</param>
        /// <param name="commandString">The command string that will be recorded along with this timing, for display in the MiniProfiler results.</param>
        /// <param name="executeType">Execute Type to be associated with the Custom Timing. Example: Get, Set, Insert, Delete</param>
        /// <remarks>
        /// Should be used like the <see cref="Step(MiniProfiler, string)"/> extension method
        /// </remarks>
        public static CustomTiming CustomTiming(this MiniProfiler profiler, string category, string commandString, string executeType = null)
        {
            return CustomTimingIf(profiler, category, commandString, 0, executeType: executeType);
        }

        /// <summary>
        /// Returns a new <see cref="CustomTiming"/> that will automatically set its <see cref="Profiling.CustomTiming.StartMilliseconds"/>
        /// and <see cref="Profiling.CustomTiming.DurationMilliseconds"/>. Will only save the new <see cref="Timing"/> if the total elapsed time
        /// takes more than <paramef name="minSaveMs" />.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="category">The category under which this timing will be recorded.</param>
        /// <param name="commandString">The command string that will be recorded along with this timing, for display in the MiniProfiler results.</param>
        /// <param name="executeType">Execute Type to be associated with the Custom Timing. Example: Get, Set, Insert, Delete</param>
        /// <param name="minSaveMs">The minimum amount of time that needs to elapse in order for this result to be recorded.</param>
        /// <remarks>
        /// Should be used like the <see cref="Step(MiniProfiler, string)"/> extension method 
        /// </remarks>
        public static CustomTiming CustomTimingIf(this MiniProfiler profiler, string category, string commandString, decimal minSaveMs, string executeType = null)
        {
            if (profiler?.Head == null || !profiler.IsActive) return null;

            var result = new CustomTiming(profiler, commandString, minSaveMs)
            {
                ExecuteType = executeType,
                Category = category
            };

            // THREADING: revisit
            profiler.Head.AddCustomTiming(category, result);

            return result;
        }
    }
}