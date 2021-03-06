using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.SqlFormatters;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Profiles a single SQL execution.
    /// </summary>
    public class SqlTiming
    {
        /// <summary>
        /// Holds the maximum size that will be stored for byte[] parameters
        /// </summary>
        private const int MaxByteParameterSize = 512;
        private readonly MiniProfiler _profiler;
        private readonly CustomTiming _customTiming;

        /// <summary>
        /// Initialises a new instance of the <see cref="SqlTiming"/> class. 
        /// Creates a new <c>SqlTiming</c> to profile 'command'.
        /// </summary>
        public SqlTiming(IDbCommand command, SqlExecuteType type, MiniProfiler profiler)
        {
            if (profiler == null)
                throw new ArgumentNullException(nameof(profiler));

            _profiler = profiler;

            var commandText = AddSpacesToParameters(command.CommandText);
            var parameters = GetCommandParameters(command);

            if (MiniProfiler.Settings.SqlFormatter != null)
                commandText = MiniProfiler.Settings.SqlFormatter.GetFormattedSql(commandText, parameters, command);

            _customTiming = profiler.CustomTiming("sql", commandText, type.ToString());

            if (_customTiming == null)
                throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets or sets the offset from main <c>MiniProfiler</c> start that this custom command began.
        /// </summary>
        public decimal StartMilliseconds { get { return _customTiming.StartMilliseconds; } }

        /// <summary>
        /// Returns a snippet of the SQL command and the duration.
        /// </summary>
        public override string ToString()
        {
            return _customTiming.CommandString.Truncate(30) + " (" + _customTiming.DurationMilliseconds + " ms)";
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object other)
        {
            var timing = other as SqlTiming;

            return timing != null && _customTiming.Id.Equals(timing._customTiming.Id);
        }

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        public override int GetHashCode()
        {
            return _customTiming.Id.GetHashCode();
        }

        /// <summary>
        /// Called when command execution is finished to determine this <c>SqlTiming's</c> duration.
        /// </summary>
        public void ExecutionComplete(bool isReader)
        {
            if (isReader)
            {
                _customTiming.FirstFetchCompleted();
            }
            else
            {
                _customTiming.Stop();
            }
        }

        /// <summary>
        /// Called when database reader is closed, ending profiling for 
        /// <see cref="StackExchange.Profiling.Data.SqlExecuteType.Reader"/> <c>SqlTimings</c>.
        /// </summary>
        public void ReaderFetchComplete()
        {
            _customTiming.Stop();
        }

        /// <summary>
        /// Returns the value of <paramref name="parameter"/> suitable for storage/display.
        /// </summary>
        private static string GetValue(IDataParameter parameter)
        {
            var rawValue = parameter.Value;

            if (rawValue == null || rawValue == DBNull.Value)
            {
                return null;
            }

            // This assumes that all SQL variants use the same parameter format, it works for T-SQL
            if (parameter.DbType == DbType.Binary)
            {
                var bytes = rawValue as byte[];

                if (bytes != null && bytes.Length <= MaxByteParameterSize)
                {
                    return "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                }

                // Parameter is too long, so blank it instead
                return null;

            }

            var param = parameter as SqlParameter;

            if (parameter.DbType == DbType.Object && param != null && param.SqlDbType == SqlDbType.Structured)
            {
                var r = param.Value as DataTable;

                if (r != null)
                {
                    var result = new List<object>();

                    foreach (DataRow x in r.Rows)
                    {
                        for (var i = 0; i < x.ItemArray.Length; i++)
                        {
                            result.Add(new { Name = x.Table.Columns.Count > i ? x.Table.Columns[i].ColumnName : "Unk", Value = x.ItemArray[i] });
                        }
                    }

                    return JsonConvert.SerializeObject(result);
                }
            }

            if (rawValue is DateTime)
            {
                return ((DateTime)rawValue).ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            }

            // we want the integral value of an enum, not its string representation
            var rawType = rawValue.GetType();

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (rawType.IsEnum)
            {
                // use ChangeType, as we can't cast - http://msdn.microsoft.com/en-us/library/exx3b86w(v=vs.80).aspx
                return Convert.ChangeType(rawValue, Enum.GetUnderlyingType(rawType)).ToString();
            }


            return rawValue.ToString();
        }

        private static int GetParameterSize(IDbDataParameter parameter)
        {
            var value = parameter.Value as INullable;

            if (value == null)
                return parameter.Size;

            var nullable = value;

            return nullable.IsNull ? 0 : parameter.Size;
        }

        /// <summary>
        /// To help with display, put some space around crowded commas.
        /// </summary>
        private static string AddSpacesToParameters(string commandString)
        {
            return Regex.Replace(commandString, @",([^\s])", ", $1");
        }

        /// <summary>
        /// Returns better parameter information for <paramref name="command"/>.  Returns null if no parameters are present.
        /// </summary>
        public static List<SqlTimingParameter> GetCommandParameters(IDbCommand command)
        {
            if (command.Parameters == null || command.Parameters.Count == 0) return null;

            var result = new List<SqlTimingParameter>();

            foreach (DbParameter parameter in command.Parameters)
            {
                if (parameter.ParameterName.HasValue())
                {
                    result.Add(new SqlTimingParameter
                    {
                        Name = parameter.ParameterName.Trim(),
                        Value = GetValue(parameter),
                        DbType = parameter.DbType.ToString(),
                        Size = GetParameterSize(parameter),
                        Direction = parameter.Direction.ToString(),
                        IsNullable = parameter.IsNullable
                    });
                }
            }

            return result;
        }
    }
}