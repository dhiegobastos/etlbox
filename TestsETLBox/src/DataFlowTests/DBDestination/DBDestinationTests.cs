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
    public class DBDestinationTests : IDisposable
    {
        public static IEnumerable<object[]> Connections => Config.AllSqlConnections("DataFlow");
        public DBDestinationTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public void Dispose()
        {
        }


        public class MyExtendedRow
        {
            [ColumnMap("Col1")]
            public int Id { get; set; }
            [ColumnMap("Col3")]
            public long? Value { get; set; }
            [ColumnMap("Col4")]
            public decimal Percentage { get; set; }
            [ColumnMap("Col2")]
            public string Text { get; set; }
        }

        [Theory, MemberData(nameof(Connections))]
        public void ColumnMapping(IConnectionManager connection)
        {
            //Arrange
            FourColumnsTableFixture source4Columns = new FourColumnsTableFixture(connection,"Source");
            source4Columns.InsertTestData();
            FourColumnsTableFixture dest4Columns = new FourColumnsTableFixture(connection, "Destination", identityColumnIndex: 2);

            DBSource source = new DBSource(connection, "Source");
            RowTransformation<string[], MyExtendedRow> trans = new RowTransformation<string[], MyExtendedRow>(
                row => new MyExtendedRow()
                {
                    Id = int.Parse(row[0]),
                    Text = row[1],
                    Value = row[2] != null ? (long?)long.Parse(row[2]) : null,
                    Percentage = decimal.Parse(row[3])
                });

            //Act
            DBDestination<MyExtendedRow> dest = new DBDestination<MyExtendedRow>(connection, "Destination");
            source.LinkTo(trans);
            trans.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            dest4Columns.AssertTestData();

        }
    }
}
