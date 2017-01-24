using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Script.Serialization;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling
{
    /// <summary>
    /// A single MiniProfiler can be used to represent any number of steps/levels in a call-graph, via Step()
    /// </summary>
    /// <remarks>Totally baller.</remarks>
    [DataContract]
    public partial class MiniProfiler
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="MiniProfiler"/> class. 
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public MiniProfiler()
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MiniProfiler"/> class.  Creates and starts a new MiniProfiler 
        /// for the root <paramref name="url"/>.
        /// </summary>
        public MiniProfiler(string url)
        {
            Id = Guid.NewGuid();
#pragma warning disable 612,618
            Level = ProfileLevel.Info;
#pragma warning restore 612,618
            SqlProfiler = new SqlProfiler(this);
            MachineName = Environment.MachineName;
            Started = DateTime.UtcNow;

            // stopwatch must start before any child Timings are instantiated
            _sw = Settings.StopwatchProvider();
            Root = new Timing(this, null, url);
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MiniProfiler"/> class.  Creates and starts a new MiniProfiler 
        /// for the root <paramref name="url"/>, filtering <see cref="Timing"/> steps to <paramref name="level"/>.
        /// </summary>
        [Obsolete("Please use the MiniProfiler(string url) constructor instead of this one. ProfileLevel is going away")]
        public MiniProfiler(string url, ProfileLevel level = ProfileLevel.Info) : this(url)
        {
#pragma warning disable 612,618
            Level = level;
#pragma warning restore 612,618
        }

        /// <summary>
        /// Starts when this profiler is instantiated. Each <see cref="Timing"/> step will use this Stopwatch's current ticks as
        /// their starting time.
        /// </summary>
        private readonly IStopwatch _sw;

        /// <summary>
        /// The root.
        /// </summary>
        private Timing _root;

        /// <summary>
        /// Gets or sets the profiler id.
        /// Identifies this Profiler so it may be stored/cached.
        /// </summary>
        [DataMember(Order = 1)]
        public Guid Id { get; }

        /// <summary>
        /// Gets or sets a display name for this profiling session.
        /// </summary>
        [DataMember(Order = 2)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets when this profiler was instantiated, in UTC time.
        /// </summary>
        [DataMember(Order = 3)]
        public DateTime Started { get; set; }

        /// <summary>
        /// Gets the milliseconds, to one decimal place, that this MiniProfiler ran.
        /// </summary>
        [DataMember(Order = 4)]
        public decimal DurationMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets where this profiler was run.
        /// </summary>
        [DataMember(Order = 5)]
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the root timing.
        /// The first <see cref="Timing"/> that is created and started when this profiler is instantiated.
        /// All other <see cref="Timing"/>s will be children of this one.
        /// </summary>
        [DataMember(Order = 7)]
        public Timing Root
        {
            get
            {
                return _root;
            }
            set
            {
                _root = value;
                RootTimingId = value.Id;

                // TODO: remove this shit

                // when being deserialized, we need to go through and set all child timings' parents
                if (!_root.HasChildren)
                    return;

                var timings = new Stack<Timing>();

                timings.Push(_root);

                while (timings.Count > 0)
                {
                    var timing = timings.Pop();

                    if (!timing.HasChildren)
                        continue;

                    var children = timing.Children;

                    for (var i = children.Count - 1; i >= 0; i--)
                    {
                        children[i].ParentTiming = timing;
                        timings.Push(children[i]); // FLORIDA!  TODO: refactor this and other stack creation methods into one 
                    }
                }
            }
        }

        /// <summary>
        /// Id of Root Timing. Used for Sql Storage purposes.
        /// </summary>
        [ScriptIgnore]
        public Guid? RootTimingId { get; set; }

        /// <summary>
        /// Gets or sets timings collected from the client
        /// </summary>
        [DataMember(Order = 8)]
        public ClientTimings ClientTimings { get; set; }

        /// <summary>
        /// RedirectCount in ClientTimings. Used for sql storage.
        /// </summary>
        [ScriptIgnore]
        public int? ClientTimingsRedirectCount { get; set; }

        /// <summary>
        /// Gets or sets whether or not filtering is allowed of <see cref="Timing"/> steps based on what <see cref="ProfileLevel"/> 
        /// the steps are created with.
        /// </summary>
        [Obsolete("If you don't want this removed, speak up at https://github.com/MiniProfiler/dotnet")]
        [ScriptIgnore]
        public ProfileLevel Level { get; set; }

        /// <summary>
        /// Gets or sets points to the currently executing Timing. 
        /// </summary>
        [ScriptIgnore]
        public Timing Head { get; set; }

        /// <summary>
        /// Gets the ticks since this MiniProfiler was started.
        /// </summary>
        internal long ElapsedTicks => _sw.ElapsedTicks;

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start(string)"/>ed.
        /// </summary>
        public static MiniProfiler Current => Settings.ProfilerProvider.GetCurrentProfiler();

        /// <summary>
        /// A <see cref="IStorage"/> strategy to use for the current profiler. 
        /// If null, then the <see cref="IStorage"/> set in <see cref="MiniProfiler.Settings.Storage"/> will be used.
        /// </summary>
        /// <remarks>Used to set custom storage for an individual request</remarks>
        public IStorage Storage { get; set; }

        /// <summary>
        /// Starts a new MiniProfiler based on the current <see cref="IProfilerProvider"/>. This new profiler can be accessed by
        /// <see cref="MiniProfiler.Current"/>.
        /// </summary>
        public static MiniProfiler Start()
        {
            return Start(null);
        }

        /// <summary>
        /// Starts a new MiniProfiler based on the current <see cref="IProfilerProvider"/>. This new profiler can be accessed by
        /// <see cref="MiniProfiler.Current"/>.
        /// </summary>
        /// <param name="sessionName">
        /// Allows explicit naming of the new profiling session; when null, an appropriate default will be used, e.g. for
        /// a web request, the url will be used for the overall session name.
        /// </param>
        public static MiniProfiler Start(string sessionName)
        {
            return Settings.ProfilerProvider.Start(sessionName);
        }

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public static void Stop(bool discardResults = false)
        {
            Settings.ProfilerProvider.Stop(discardResults);
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal. Use this method when you
        /// do not wish to include the StackExchange.Profiling namespace for the <see cref="MiniProfilerExtensions.Step(MiniProfiler,string)"/> extension method.
        /// </summary>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <returns>the static step.</returns>
        public static IDisposable StepStatic(string name)
        {
            return Current.Step(name);
        }

        /// <summary>
        /// Renders the current <see cref="MiniProfiler"/> to JSON.
        /// </summary>
        public static string ToJson()
        {
            return ToJson(Current);
        }

        /// <summary>
        /// Renders the parameter <see cref="MiniProfiler"/> to JSON.
        /// </summary>
        public static string ToJson(MiniProfiler profiler)
        {
            return profiler == null ? null : GetJsonSerializer().Serialize(profiler);
        }

        private static JavaScriptSerializer GetJsonSerializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = Settings.MaxJsonResponseSize };
        }

        /// <summary>
        /// Returns the <see cref="Root"/>'s <see cref="Timing.Name"/> and <see cref="DurationMilliseconds"/> this profiler recorded.
        /// </summary>
        /// <returns>a string containing the recording information</returns>
        public override string ToString()
        {
            return Root != null ? Root.Name + " (" + DurationMilliseconds + " ms)" : "";
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object other)
        {
            return other is MiniProfiler && Id.Equals(((MiniProfiler)other).Id);
        }

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Walks the <see cref="Timing"/> hierarchy contained in this profiler, starting with <see cref="Root"/>, and returns each Timing found.
        /// </summary>
        public IEnumerable<Timing> GetTimingHierarchy()
        {
            var timings = new Stack<Timing>();

            timings.Push(_root);

            while (timings.Count > 0)
            {
                var timing = timings.Pop();

                yield return timing;

                if (!timing.HasChildren)
                    continue;
                var children = timing.Children;

                for (int i = children.Count - 1; i >= 0; i--)
                    timings.Push(children[i]);
            }
        }

        /// <summary>
        /// Create a DEEP clone of this MiniProfiler.
        /// </summary>
        public MiniProfiler Clone()
        {
            var serializer = new DataContractSerializer(typeof(MiniProfiler), null, int.MaxValue, false, true, null);

            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                ms.Position = 0;
                return (MiniProfiler)serializer.ReadObject(ms);
            }
        }

        internal IDisposable StepImpl(string name, decimal? minSaveMs = null, bool? includeChildrenWithMinSave = false)
        {
            return new Timing(this, Head, name, minSaveMs, includeChildrenWithMinSave);
        }

        internal bool StopImpl()
        {
            if (!_sw.IsRunning)
                return false;

            _sw.Stop();

            DurationMilliseconds = GetRoundedMilliseconds(ElapsedTicks);

            foreach (var timing in GetTimingHierarchy())
                timing.Stop();

            return true;
        }

        /// <summary>
        /// Returns milliseconds based on Stopwatch's Frequency, rounded to one decimal place.
        /// </summary>
        internal decimal GetRoundedMilliseconds(long ticks)
        {
            long z = 10000 * ticks;

            decimal timesTen = (int)(z / _sw.Frequency);

            return timesTen / 10;
        }

        /// <summary>
        /// Returns how many milliseconds have elapsed since <paramref name="startTicks"/> was recorded.
        /// </summary>
        internal decimal GetDurationMilliseconds(long startTicks)
        {
            return GetRoundedMilliseconds(ElapsedTicks - startTicks);
        }
    }
}