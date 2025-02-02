﻿using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace ALE.ETLBox.Helper
{
    public class DatabaseHelper
    {
        private static void DropAndCreate(IConnectionManager connManagerMaster, string dbName)
        {
            new DropDatabaseTask(dbName)
            {
                DisableLogging = true,
                ConnectionManager = connManagerMaster
            }.DropIfExists();

            new CreateDatabaseTask(dbName)
            {
                DisableLogging = true,
                ConnectionManager = connManagerMaster
            }.Execute();
        }

        public static void RecreateSqlDatabase(string section)
        {
            var connManagerMaster = new SqlConnectionManager(
                            Config.SqlConnection.ConnectionString(section).GetMasterConnection()
                            );
            var dbName = Config.SqlConnection.ConnectionString(section).DBName;

            DropAndCreate(connManagerMaster, dbName);
        }

        public static void RecreateMySqlDatabase(string section)
        {
            var connManagerMaster = new MySqlConnectionManager(
                            Config.MySqlConnection.ConnectionString(section).GetMasterConnection()
                            );
            var dbName = Config.MySqlConnection.ConnectionString(section).DBName;
            DropAndCreate(connManagerMaster, dbName);
        }

        public static void RecreatePostgresDatabase(string section)
        {
            var connManagerMaster = new PostgresConnectionManager(
                            Config.PostgresConnection.ConnectionString(section).GetMasterConnection()
                            );
            var dbName = Config.PostgresConnection.ConnectionString(section).DBName;

            DropAndCreate(connManagerMaster, dbName);
        }
    }
}
