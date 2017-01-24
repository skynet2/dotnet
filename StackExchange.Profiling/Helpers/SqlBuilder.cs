using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Profiling.Helpers.Dapper
{
    /// <summary>
    /// Help to build out sql
    /// </summary>
    public class SqlBuilder
    {
        private readonly Dictionary<string, Clauses> _data = new Dictionary<string, Clauses>();
        private int _seq;

        private class Clause
        {
            public string Sql { get; set; }
            public object Parameters { get; set; }
        }

        private class Clauses : List<Clause>
        {
            private readonly string _joiner;
            private readonly string _prefix;
            private readonly string _postfix;

            public Clauses(string joiner, string prefix = "", string postfix = "")
            {
                _joiner = joiner;
                _prefix = prefix;
                _postfix = postfix;
            }

            public string ResolveClauses(DynamicParameters p)
            {
                foreach (var item in this)
                {
                    p.AddDynamicParams(item.Parameters);
                }
                return _prefix + string.Join(_joiner, this.Select(c => c.Sql)) + _postfix;
            }
        }

        /// <summary>
        /// Template helper class for SqlBuilder
        /// </summary>
        public class Template
        {
            private readonly string _sql;
            private readonly SqlBuilder _builder;
            private readonly object _initParams;
            private int _dataSeq = -1; // Unresolved

            /// <summary>
            /// Template constructor
            /// </summary>
            /// <param name="builder"></param>
            /// <param name="sql"></param>
            /// <param name="parameters"></param>
            public Template(SqlBuilder builder, string sql, dynamic parameters)
            {
                _initParams = parameters;
                _sql = sql;
                _builder = builder;
            }

            private static readonly System.Text.RegularExpressions.Regex Regex =
                new System.Text.RegularExpressions.Regex(@"\/\*\*.+\*\*\/", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline);

            private void ResolveSql()
            {
                if (_dataSeq == _builder._seq)
                    return;

                var p = new DynamicParameters(_initParams);

                _rawSql = _sql;

                foreach (var pair in _builder._data)
                {
                    _rawSql = _rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(p));
                }
                _parameters = p;

                // replace all that is left with empty
                _rawSql = Regex.Replace(_rawSql, "");

                _dataSeq = _builder._seq;
            }

            private string _rawSql;
            private object _parameters;

            /// <summary>
            /// Raw Sql returns by the <see cref="SqlBuilder"/>
            /// </summary>
            public string RawSql { get { ResolveSql(); return _rawSql; } }

            /// <summary>
            /// Parameters being used
            /// </summary>
            public object Parameters { get { ResolveSql(); return _parameters; } }
        }

        /// <summary>
        /// Add a template to the SqlBuilder
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Template AddTemplate(string sql, dynamic parameters = null)
        {
            return new Template(this, sql, parameters);
        }

        private void AddClause(string name, string sql, object parameters, string joiner, string prefix = "", string postfix = "")
        {
            Clauses clauses;

            if (!_data.TryGetValue(name, out clauses))
            {
                clauses = new Clauses(joiner, prefix, postfix);
                _data[name] = clauses;
            }

            clauses.Add(new Clause { Sql = sql, Parameters = parameters });
            _seq++;
        }


        /// <summary>
        /// Add a Left Join
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder LeftJoin(string sql, dynamic parameters = null)
        {
            AddClause("leftjoin", sql, parameters, joiner: "\nLEFT JOIN ", prefix: "\nLEFT JOIN ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// Add a Where Clause
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder Where(string sql, dynamic parameters = null)
        {
            AddClause("where", sql, parameters, " AND ", prefix: "WHERE ", postfix: "\n");
            return this;
        }
        
        /// <summary>
        /// Add an OrderBy Clause
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder OrderBy(string sql, dynamic parameters = null)
        {
            AddClause("orderby", sql, parameters, " , ", prefix: "ORDER BY ", postfix: "\n");
            return this;
        }
    }
}
