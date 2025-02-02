using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace ALE.ETLBoxTests.ControlFlowTests
{
    [Collection("ControlFlow")]
    public class CreateViewTaskTests
    {
       public static IEnumerable<object[]> Connections => Config.AllSqlConnections("ControlFlow");

        public CreateViewTaskTests(ControlFlowDatabaseFixture dbFixture)
        { }

        [Theory, MemberData(nameof(Connections))]
        public void CreateView(IConnectionManager connection)
        {
            //Arrange
            //Act
            CreateViewTask.CreateOrAlter(connection, "View1", "SELECT 1 AS Test");
            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "View1"));
        }

        [Theory, MemberData(nameof(Connections))]
        public void AlterView(IConnectionManager connection)
        {
            //Arrange
            CreateViewTask.CreateOrAlter(connection, "View2", "SELECT 1 AS Test");
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "View2"));

            //Act
            CreateViewTask.CreateOrAlter(connection, "View2", "SELECT 5 AS Test");

            //Assert
            Assert.True(IfTableOrViewExistsTask.IsExisting(connection, "View2"));
        }
    }
}
