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
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class RowTransformationTests : IDisposable
    {
        public SqlConnectionManager Connection => Config.SqlConnectionManager("DataFlow");
        public RowTransformationTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public void Dispose()
        {
        }

        public class MySimpleRow
        {
            public int Col1 { get; set; }
            public string Col2 { get; set; }
        }

        [Fact]
        public void ConvertIntoObject()
        {
            //Arrange
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("DestinationRowTransformation");
            CSVSource source = new CSVSource("res/RowTransformation/TwoColumns.csv");

            //Act
            RowTransformation<string[], MySimpleRow> trans = new RowTransformation<string[], MySimpleRow>(
                csvdata =>
                {
                    return new MySimpleRow()
                    {
                        Col1 = int.Parse(csvdata[0]),
                        Col2 = csvdata[1]
                    };
                });
            DBDestination<MySimpleRow> dest = new DBDestination<MySimpleRow>(Connection, "DestinationRowTransformation");
            source.LinkTo(trans);
            trans.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            dest2Columns.AssertTestData();
        }

        [Fact]
        public void InitAction()
        {
            //Arrange
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("DestinationRowTransformation");
            CSVSource<MySimpleRow> source = new CSVSource<MySimpleRow>("res/RowTransformation/TwoColumnsIdMinus1.csv");

            //Act
            int IdOffset = 0;
            RowTransformation<MySimpleRow, MySimpleRow> trans = new RowTransformation<MySimpleRow, MySimpleRow>(
                "RowTransformation testing init Action",
                row =>
                {
                    row.Col1 += IdOffset;
                    return row;
                },
                () => IdOffset += 1
            );
            DBDestination<MySimpleRow> dest = new DBDestination<MySimpleRow>(Connection, "DestinationRowTransformation");
            source.LinkTo(trans);
            trans.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            dest2Columns.AssertTestData();
        }
    }
}
