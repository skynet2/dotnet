using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Times collected from the client
    /// </summary>
    [DataContract]
    public class ClientTimings
    {
        /// <summary>
        /// Gets or sets the list of client side timings
        /// </summary>
        [DataMember(Order = 2)]
        public List<ClientTiming> Timings { get; set; }

        /// <summary>
        /// Gets or sets the redirect count.
        /// </summary>
        [DataMember(Order = 1)]
        public int RedirectCount { get; set; }

        /// <summary>
        /// A client timing probe
        /// </summary>
        [DataContract]
        public class ClientTiming
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            [DataMember(Order = 1)]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the start.
            /// </summary>
            [DataMember(Order = 2)]
            public decimal Start { get; set; }

            /// <summary>
            /// Gets or sets the duration.
            /// </summary>
            [DataMember(Order = 3)]
            public decimal Duration { get; set; }

            /// <summary>
            /// Unique Identifier used for sql storage. 
            /// </summary>
            /// <remarks>Not set unless storing in Sql</remarks>
            [ScriptIgnore]
            public Guid Id { get; set; }

            /// <summary>
            /// Used for sql storage
            /// </summary>
            /// <remarks>Not set unless storing in Sql</remarks>
            [ScriptIgnore]
            public Guid MiniProfilerId { get; set; }
        }
    }
}
