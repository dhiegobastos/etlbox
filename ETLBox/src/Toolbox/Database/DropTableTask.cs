﻿using ALE.ETLBox.ConnectionManager;

namespace ALE.ETLBox.ControlFlow
{
    /// <summary>
    /// Drops a table. Use DropIfExists to drop a table only if it exists.
    /// </summary>
    public class DropTableTask : DropTask<IfTableOrViewExistsTask>, ITask
    {
        internal override string GetSql()
        {
            return $@"DROP TABLE {ON.QuotatedFullName}";
        }

        /* Some constructors */
        public DropTableTask()
        {
        }

        public DropTableTask(string tableName) : this()
        {
            ObjectName = tableName;
        }


        /* Static methods for convenience */
        public static void Drop(string tableName)
            => new DropTableTask(tableName).Drop();
        public static void Drop(IConnectionManager connectionManager, string tableName)
            => new DropTableTask(tableName) { ConnectionManager = connectionManager }.Drop();
        public static void DropIfExists(string tableName)
            => new DropTableTask(tableName).DropIfExists();
        public static void DropIfExists(IConnectionManager connectionManager, string tableName)
            => new DropTableTask(tableName) { ConnectionManager = connectionManager }.DropIfExists();

    }


}
