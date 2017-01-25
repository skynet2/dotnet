using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NLog;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// 
    /// </summary>
    public class NlogStorage : IStorage
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger(); 
        /// <inheritdoc />
        public void Save(MiniProfiler profiler) //TODO
        {
            _logger.Log(new LogEventInfo()
            {
                Level = LogLevel.Info,
                Message = JsonConvert.SerializeObject(profiler)
            });
        }
    }
}
