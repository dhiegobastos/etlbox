﻿using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ALE.ETLBox.ControlFlow
{
    /// <summary>
    /// Creates a table. If the tables exists, this task won't change the table.
    /// </summary>
    /// <example>
    /// <code>
    /// CreateTableTask.Create("demo.table1", new List&lt;TableColumn&gt;() {
    /// new TableColumn(name:"key", dataType:"int", allowNulls:false, isPrimaryKey:true, isIdentity:true),
    ///     new TableColumn(name:"value", dataType:"nvarchar(100)", allowNulls:true)
    /// });
    /// </code>
    /// </example>
    public class CreateTableTask : GenericTask, ITask
    {
        /* ITask Interface */
        public override string TaskName => $"Create table {TableName}";
        public override void Execute()
        {
            bool tableExists = new IfTableOrViewExistsTask(TableName) { ConnectionManager = this.ConnectionManager, DisableLogging = true }.Exists();
            if (tableExists && ThrowErrorIfTableExists) throw new ETLBoxException($"Table {TableName} already exists!");
            if (!tableExists)
                new SqlTask(this, Sql).ExecuteNonQuery();
        }

        /* Public properties */
        public void Create() => Execute();
        public string TableName { get; set; }
        public TableNameDescriptor TN => new TableNameDescriptor(TableName, ConnectionType);
        public IList<ITableColumn> Columns { get; set; }
        public bool ThrowErrorIfTableExists { get; set; }

        public string Sql
        {
            get
            {
                return
$@"CREATE TABLE {TN.QuotatedFullName} (
{ColumnsDefinitionSql}
)
";
            }
        }

        public CreateTableTask()
        {

        }
        public CreateTableTask(string tableName, IList<ITableColumn> columns) : this()
        {
            this.TableName = tableName;
            this.Columns = columns;
        }

        public CreateTableTask(TableDefinition tableDefinition) : this()
        {
            this.TableName = tableDefinition.Name;
            this.Columns = tableDefinition.Columns.Cast<ITableColumn>().ToList();
        }

        public static void Create(string tableName, IList<ITableColumn> columns) => new CreateTableTask(tableName, columns).Execute();
        public static void Create(string tableName, List<TableColumn> columns) => new CreateTableTask(tableName, columns.Cast<ITableColumn>().ToList()).Execute();
        public static void Create(TableDefinition tableDefinition) => new CreateTableTask(tableDefinition).Execute();
        public static void Create(IConnectionManager connectionManager, string tableName, IList<ITableColumn> columns) => new CreateTableTask(tableName, columns) { ConnectionManager = connectionManager }.Execute();
        public static void Create(IConnectionManager connectionManager, string tableName, List<TableColumn> columns) => new CreateTableTask(tableName, columns.Cast<ITableColumn>().ToList()) { ConnectionManager = connectionManager }.Execute();
        public static void Create(IConnectionManager connectionManager, TableDefinition tableDefinition) => new CreateTableTask(tableDefinition) { ConnectionManager = connectionManager }.Execute();

        string ColumnsDefinitionSql => String.Join("  , " + Environment.NewLine, Columns?.Select(col => CreateTableDefinition(col)));

        string CreateTableDefinition(ITableColumn col)
        {
            string dataType = string.Empty;
            dataType = CreateDataTypeSql(col);
            string identitySql = CreateIdentitySql(col);
            string collationSql = !String.IsNullOrWhiteSpace(col.Collation)
                                    ? $"COLLATE {col.Collation}"
                                    : string.Empty;
            string nullSql = CreateNotNullSql(col);
            string primarySql = CreatePrimaryKeyConstraint(col);
            string defaultSql = CreateDefaultSql(col);
            string computedColumnSql = CreateComputedColumnSql(col);
            return
$@"{QB}{col.Name}{QE} {dataType} {nullSql} {identitySql} {collationSql} {primarySql} {defaultSql} {computedColumnSql}";
        }


        private string CreateDataTypeSql(ITableColumn col)
        {
            if (ConnectionType == ConnectionManagerType.SqlServer && col.HasComputedColumn)
                return string.Empty;
            else if (ConnectionType == ConnectionManagerType.Postgres && col.IsIdentity)
                return string.Empty;
            else
                return DataTypeConverter.TryGetDBSpecificType(col.DataType, this.ConnectionType);
        }

        private string CreateIdentitySql(ITableColumn col)
        {
            if (ConnectionType == ConnectionManagerType.SQLite) return string.Empty;
            else
            {
                if (col.IsIdentity)
                {
                    if (ConnectionType == ConnectionManagerType.MySql)
                        return "AUTO_INCREMENT";
                    else if(ConnectionType == ConnectionManagerType.Postgres)
                        return "SERIAL";
                    return $"IDENTITY({col.IdentitySeed ?? 1},{col.IdentityIncrement ?? 1})";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private string CreateNotNullSql(ITableColumn col)
        {
            string nullSql = string.Empty;
            if (ConnectionType == ConnectionManagerType.Postgres &&
                col.IsIdentity) return string.Empty;
            if (String.IsNullOrWhiteSpace(col.ComputedColumn))
                nullSql = col.AllowNulls
                            ? "NULL"
                            : "NOT NULL";
            return nullSql;
        }

        private string CreatePrimaryKeyConstraint(ITableColumn col)
        {
            if (col.IsPrimaryKey)
            {
                string pkConst = $" CONSTRAINT {QB}pk_{TN.Table}_{col.Name}{QE} PRIMARY KEY ";
                if (ConnectionType != ConnectionManagerType.SQLite)
                    pkConst = $"," + pkConst + $"({QB}{ col.Name}{QE}) ";
                return pkConst;
            }
            else
                return String.Empty;
        }

        private string CreateDefaultSql(ITableColumn col)
        {
            string defaultSql = string.Empty;
            if (!col.IsPrimaryKey)
                defaultSql = col.DefaultValue != null ? $" DEFAULT {SetQuotesIfString(col.DefaultValue)}" : string.Empty;
            return defaultSql;
        }

        private string CreateComputedColumnSql(ITableColumn col)
        {
            if (col.HasComputedColumn && ConnectionType == ConnectionManagerType.SQLite)
                throw new ETLBoxNotSupportedException("SQLite does not support computed columns");
            if (col.HasComputedColumn)
                return $"AS {col.ComputedColumn}";
            else
                return string.Empty;
        }

        string SetQuotesIfString(string value)
        {
            if (!Regex.IsMatch(value, @"^\d+(\.\d+|)$"))//@" ^ (\d|\.)+$"))
                return $"'{value}'";
            else
                return value;

        }
    }
}
