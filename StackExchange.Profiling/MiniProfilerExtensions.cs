﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Contains helper methods that ease working with null <see cref="MiniProfiler"/>s.
    /// </summary>
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Wraps <paramref name="selector"/> in a <see cref="Step(MiniProfiler, string)"/> call and executes it, returning its result.
        /// </summary>
        /// <typeparam name="T">the type of result.</typeparam>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="selector">Method to execute and profile.</param>
        /// <param name="name">The <see cref="Timing"/> step name used to label the profiler results.</param>
        /// <returns>the profiled result.</returns>
        public static T Inline<T>(this MiniProfiler profiler, Func<T> selector, string name)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            if (profiler == null) return selector();
            using (profiler.StepImpl(name))
            {
                return selector();
            }
        }

        /// <summary>
        /// Wraps <paramref name="selector"/> in a <see cref="Step(MiniProfiler, string)"/> call and executes it, returning its result.
        /// </summary>
        /// <typeparam name="T">the type of result.</typeparam>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="selector">Method to execute and profile.</param>
        /// <param name="name">The <see cref="Timing"/> step name used to label the profiler results.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start(string)"/> is called.</param>
        /// <returns>the profiled result.</returns>
        [Obsolete("Please use the Inline(Func<T> selector, string name) overload instead of this one. ProfileLevel is going away.")]
        public static T Inline<T>(this MiniProfiler profiler, Func<T> selector, string name, ProfileLevel level)
        {
            return profiler.Inline(selector, name);
        }
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
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal.
        /// Will only save the <see cref="Timing"/> if total time taken exceeds <paramref name="minSaveMs" />.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <param name="minSaveMs">The minimum amount of time that needs to elapse in order for this result to be recorded.</param>
        /// <param name="includeChildren">Should the amount of time spent in child timings be included when comparing total time
        /// profiled with <paramref name="minSaveMs"/>? If true, will include children. If false will ignore children.</param>
        /// <returns></returns>
        /// <remarks>If <paramref name="includeChildren"/> is set to true and a child is removed due to its use of StepIf, then the 
        /// time spent in that time will also not count for the current StepIf calculation.</remarks>
        public static IDisposable StepIf(this MiniProfiler profiler, string name, decimal minSaveMs, bool includeChildren = false)
        {
            return profiler?.StepImpl(name, minSaveMs, includeChildren);
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