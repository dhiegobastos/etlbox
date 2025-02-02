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
    public class LookupTests : IDisposable
    {
        public static IEnumerable<object[]> Connections => Config.AllSqlConnections("DataFlow");
        public SqlConnectionManager Connection => Config.SqlConnectionManager("DataFlow");
        public LookupTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public void Dispose()
        {
        }

        public class MyLookupRow
        {
            [ColumnMap("Col1")]
            public long Key { get; set; }
            [ColumnMap("Col3")]
            public long? LookupValue1 { get; set; }
            [ColumnMap("Col4")]
            public decimal LookupValue2 { get; set; }
        }

        public class MyInputDataRow
        {
            public long Col1 { get; set; }
            public string Col2 { get; set; }
        }

        public class MyOutputDataRow
        {
            public long Col1 { get; set; }
            public string Col2 { get; set; }
            public long? Col3 { get; set; }
            public decimal Col4 { get; set; }
        }

        [Theory, MemberData(nameof(Connections))]
        public void SimpleLookupFromDB(IConnectionManager connection)
        {
            //Arrange
            TwoColumnsTableFixture source2Columns = new TwoColumnsTableFixture(connection,"Source");
            source2Columns.InsertTestData();
            FourColumnsTableFixture dest4Columns = new FourColumnsTableFixture(connection,"Destination");
            FourColumnsTableFixture lookup4Columns = new FourColumnsTableFixture(connection,"Lookup");
            lookup4Columns.InsertTestData();

            DBSource<MyInputDataRow> source = new DBSource<MyInputDataRow>(connection, "Source");
            DBSource<MyLookupRow> lookupSource = new DBSource<MyLookupRow>(connection, "Lookup");

            //Act
            List<MyLookupRow> LookupTableData = new List<MyLookupRow>();
            Lookup<MyInputDataRow, MyOutputDataRow, MyLookupRow> lookup = new Lookup<MyInputDataRow, MyOutputDataRow, MyLookupRow>(
                row =>
                {
                    MyOutputDataRow output = new MyOutputDataRow()
                    {
                        Col1 = row.Col1,
                        Col2 = row.Col2,
                        Col3 = LookupTableData.Where(ld => ld.Key == row.Col1).Select(ld => ld.LookupValue1).FirstOrDefault(),
                        Col4 = LookupTableData.Where(ld => ld.Key == row.Col1).Select(ld => ld.LookupValue2).FirstOrDefault(),
                    };
                    return output;
                }
                , lookupSource
                , LookupTableData
            );
            DBDestination<MyOutputDataRow> dest = new DBDestination<MyOutputDataRow>(connection, "Destination");
            source.LinkTo(lookup);
            lookup.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            dest4Columns.AssertTestData();
        }
    }
}
