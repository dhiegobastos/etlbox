using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ALE.ETLBoxTests.ControlFlowTests
{
    [Collection("ControlFlow")]
    public class CreateTableTaskTests
    {
        public SqlConnectionManager SqlConnection => Config.SqlConnection.ConnectionManager("ControlFlow");
        public static IEnumerable<object[]> Connections => Config.AllSqlConnections("ControlFlow");

        public CreateTableTaskTests(ControlFlowDatabaseFixture dbFixture)
        { }

        [Theory, MemberData(nameof(Connections))]
        public void CreateTable(IConnectionManager connection)
        {
            //Arrange
            List<TableColumn> columns = new List<TableColumn>() { new TableColumn("value", "INT") };
            //Act
            CreateTableTask.Create(connection, "CreateTable1", columns);
            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "CreateTable1"));
        }

        [Theory, MemberData(nameof(Connections))]
        public void ReCreateTable(IConnectionManager connection)
        {
            //Arrange
            List<TableColumn> columns = new List<TableColumn>() { new TableColumn("value", "INT") };
            CreateTableTask.Create(connection, "CreateTable2", columns);
            //Act
            CreateTableTask.Create(connection, "CreateTable2", columns);
            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "CreateTable2"));
        }

        [Theory, MemberData(nameof(Connections))]
        public void CreateTableWithNullable(IConnectionManager connection)
        {
            //Arrange
            List<TableColumn> columns = new List<TableColumn>() {
                new TableColumn("value", "INT"),
                new TableColumn("value2", "DATE", true)
            };
            //Act
            CreateTableTask.Create(connection, "CreateTable3", columns);
            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "CreateTable3"));
            var td = TableDefinition.GetDefinitionFromTableName("CreateTable3", connection);
            Assert.Contains(td.Columns, col => col.AllowNulls);
        }

        [Theory, MemberData(nameof(Connections))]
        public void CreateTableWithPrimaryKey(IConnectionManager connection)
        {
            //Arrange
            List<TableColumn> columns = new List<TableColumn>() {
                new TableColumn("Id", "INT",allowNulls:false,isPrimaryKey:true),
                new TableColumn("value2", "DATE", allowNulls:true)
            };

            //Act
            CreateTableTask.Create(connection, "CreateTable4", columns);

            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "CreateTable4"));
            var td = TableDefinition.GetDefinitionFromTableName("CreateTable4", connection);
            Assert.Contains(td.Columns, col => col.IsPrimaryKey);

        }

        [Theory, MemberData(nameof(Connections))]
        public void ThrowingException(IConnectionManager connection)
        {
            //Arrange
            List<TableColumn> columns = new List<TableColumn>() {
                new TableColumn("value1", "INT",allowNulls:false),
                new TableColumn("value2", "DATE", allowNulls:true)
            };
            CreateTableTask.Create(connection, "CreateTable5", columns);
            //Act

            //Assert
            Assert.Throws<ETLBoxException>(() =>
            {
                new CreateTableTask("CreateTable5", columns.Cast<ITableColumn>().ToList())
                {
                    ConnectionManager = connection,
                    ThrowErrorIfTableExists = true
                }
                .Execute();
            });
        }

        [Theory, MemberData(nameof(Connections))]
        public void CreateTableWithIdentity(IConnectionManager connection)
        {
            //Arrange
            List<TableColumn> columns = new List<TableColumn>() {
                new TableColumn("value1", "INT",allowNulls:false, isPrimaryKey:true, isIdentity:true)
            };

            //Act
            CreateTableTask.Create(connection, "CreateTable6", columns);

            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "CreateTable6"));
            if (connection.GetType() != typeof(SQLiteConnectionManager))
            {
                var td = TableDefinition.GetDefinitionFromTableName("CreateTable6", connection);
                Assert.Contains(td.Columns, col => col.IsIdentity);
            }
        }

        [Fact]
        public void CreateTableWithIdentityIncrement()
        {
            //Arrange
            List<TableColumn> columns = new List<TableColumn>() {
                new TableColumn("value1", "INT",allowNulls:false)
                {
                    IsIdentity =true,
                    IdentityIncrement = 1000,
                    IdentitySeed = 50 }
            };

            //Act
            CreateTableTask.Create(SqlConnection, "CreateTable7", columns);

            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(SqlConnection, "CreateTable7"));
            var td = TableDefinition.GetDefinitionFromTableName("CreateTable7", SqlConnection);
            Assert.Contains(td.Columns, col => col.IsIdentity && col.IdentityIncrement == 1000 && col.IdentitySeed == 50);
        }

        [Theory, MemberData(nameof(Connections))]
        public void CreateTableWithDefault(IConnectionManager connection)
        {
            //Arrange
            List<TableColumn> columns = new List<TableColumn>() {
                new TableColumn("value1", "INT",allowNulls:false) { DefaultValue = "0" },
                new TableColumn("value2", "NVARCHAR(10)",allowNulls:false) { DefaultValue = "Test" },
                new TableColumn("value3", "DECIMAL(10,2)",allowNulls:false) { DefaultValue = "3.12" }
            };
            //Act
            CreateTableTask.Create(connection, "CreateTable8", columns);
            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "CreateTable8"));
            var td = TableDefinition.GetDefinitionFromTableName("CreateTable8", connection);
            Assert.Contains(td.Columns, col => col.DefaultValue == "0");
            Assert.Contains(td.Columns, col => col.DefaultValue == "Test" || col.DefaultValue == "'Test'" );
            Assert.Contains(td.Columns, col => col.DefaultValue == "3.12");
        }


        [Theory, MemberData(nameof(Connections))]
        public void CreateTableWithComputedColumn(IConnectionManager connection)
        {
            if (connection.GetType() != typeof(SQLiteConnectionManager) &&
                connection.GetType() != typeof(PostgresConnectionManager))
            {
                //Arrange
                List<TableColumn> columns = new List<TableColumn>() {
                    new TableColumn("value1", "INT",allowNulls:false) ,
                    new TableColumn("value2", "INT",allowNulls:false) ,
                    new TableColumn("compValue", "BIGINT",allowNulls:true) { ComputedColumn = "(value1 * value2)" }
                };

                //Act
                CreateTableTask.Create(connection, "CreateTable9", columns);

                //Assert
                Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "CreateTable9"));
                var td = TableDefinition.GetDefinitionFromTableName("CreateTable9", connection);
                if (connection.GetType() == typeof(SqlConnectionManager))
                    Assert.Contains(td.Columns, col => col.ComputedColumn == "[value1]*[value2]");
                else if  (connection.GetType() == typeof(MySqlConnectionManager))
                      Assert.Contains(td.Columns, col => col.ComputedColumn == "(`value1` * `value2`)");
            }
        }
    }
}
