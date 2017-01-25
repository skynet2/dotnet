using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web;
using System.Web.Routing;

namespace StackExchange.Profiling
{
    /// <summary>
    /// HttpContext based profiler provider.  This is the default provider to use in a web context.
    /// The current profiler is associated with a HttpContext.Current ensuring that profilers are 
    /// specific to a individual HttpRequest.
    /// </summary>
    public class WebRequestProfilerProvider : BaseProfilerProvider
    {
        /// <summary>
        /// 
        /// </summary>
        public static ConcurrentDictionary<string, object> LocalCache = new ConcurrentDictionary<string, object>();
        private string Id { get; set; }
        /// <summary>
        /// Starts a new MiniProfiler and associates it with the current <see cref="HttpContext.Current"/>.
        /// </summary>
        public override MiniProfiler Start(string sessionName = null)
        {
            if (string.IsNullOrEmpty(sessionName))
                sessionName = Guid.NewGuid().ToString();

            Id = sessionName;

            var result = new MiniProfiler(Id);

            Current = result;

            SetProfilerActive(result);

            // don't really want to pass in the context to MiniProfler's constructor or access it statically in there, either

            return result;
        }

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public override void Stop(bool discardResults)
        {
            var current = Current;

            if (current == null)
                return;

            // stop our timings - when this is false, we've already called .Stop before on this session
            if (!StopProfiler(current))
                return;

            if (discardResults)
            {
                Current = null;
                return;
            }

            // save the profiler
            SaveProfiler(current);
        }

        /// <summary>
        /// Returns the current profiler
        /// </summary>
        public override MiniProfiler GetCurrentProfiler()
        {
            return Current;
        }

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start(string)"/>ed.
        /// </summary>
        private MiniProfiler Current
        {
            get
            {
                object obj;
                LocalCache.TryGetValue(Id, out obj);

                return obj as MiniProfiler;
            }
            set
            {
                LocalCache.TryAdd(Id, value);
            }
        }
    }
}
