using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class DBDestinationSpecialCharacterTests : IDisposable
    {
        public static IEnumerable<object[]> OdbcConnections => Config.AllOdbcConnections("DataFlow");
        public static IEnumerable<object[]> SqlConnections => Config.AllSqlConnections("DataFlow");

        public DBDestinationSpecialCharacterTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public void Dispose()
        {
        }

        private void InsertTestData(IConnectionManager connection, string tableName)
        {
            var TN = new TableNameDescriptor(tableName, connection);
            SqlTask.ExecuteNonQuery(connection, "Insert demo data"
                , $@"INSERT INTO {TN.QuotatedFullName} VALUES(1,'\0 \"" \b \n \r \t \Z \\ \% \_ ')");
            SqlTask.ExecuteNonQuery(connection, "Insert demo data"
                , $@"INSERT INTO {TN.QuotatedFullName} VALUES(2,' '' """" ')");
            SqlTask.ExecuteNonQuery(connection, "Insert demo data"
                 , $@"INSERT INTO {TN.QuotatedFullName} VALUES(3,' !""�$%&/())='' ')");
            SqlTask.ExecuteNonQuery(connection, "Insert demo data"
                , $@"INSERT INTO {TN.QuotatedFullName} VALUES(4,NULL)");
        }

        [Theory, MemberData(nameof(OdbcConnections)),
            MemberData(nameof(SqlConnections))]
        public void ColumnMapping(IConnectionManager connection)
        {
            //Arrange
            TwoColumnsTableFixture s2c = new TwoColumnsTableFixture(connection, "SpecialCharacterSource");
            InsertTestData(connection, "SpecialCharacterSource");

            TwoColumnsTableFixture d2c = new TwoColumnsTableFixture(connection, "SpecialCharacterDestination");

            //Act
            DBSource source = new DBSource()
            {
                ConnectionManager = connection,
                SourceTableDefinition = s2c.TableDefinition
            };
            DBDestination dest = new DBDestination()
            {
                ConnectionManager = connection,
                DestinationTableDefinition = d2c.TableDefinition
            };
            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            Assert.Equal(4, RowCountTask.Count(connection, "SpecialCharacterDestination"));
        }
    }
}
