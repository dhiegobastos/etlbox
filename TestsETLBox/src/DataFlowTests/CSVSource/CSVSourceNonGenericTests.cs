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
    public class CSVSourceNonGenericTests : IDisposable
    {
        public SqlConnectionManager Connection => Config.SqlConnectionManager("DataFlow");
        public CSVSourceNonGenericTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public void Dispose()
        {
        }

        [Fact]
        public void SimpleCSVIntoDatabase()
        {
            //Arrange
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("CSVDestination2Columns");
            DBDestination dest = new DBDestination(Connection, "CSVDestination2Columns");

            //Act
            CSVSource source = new CSVSource("res/CSVSource/TwoColumns.csv");
            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            dest2Columns.AssertTestData();
        }

        [Fact]
        public void MoreColumnsInCSV()
        {
            //Arrange
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("CSVDestination2Columns");
            DBDestination dest = new DBDestination(Connection, "CSVDestination2Columns");

            //Act
            CSVSource source = new CSVSource("res/CSVSource/ThreeColumns.csv");
            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            dest2Columns.AssertTestData();
        }

        [Fact]
        public void MoreColumnsInDatabase()
        {
            //Arrange
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("CSVDestination2Columns");
            DBDestination dest = new DBDestination(Connection, "CSVDestination2Columns");

            //Act
            CSVSource source = new CSVSource("res/CSVSource/OneColumn.csv");
            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            Assert.Equal(3, RowCountTask.Count(Connection, "CSVDestination2Columns", "Col1 IN (1,2,3)"));
        }
    }
}
