//---------------------------------------------------------------------------------------------------------
// <copyright file="CheckOutFileTests.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Requires an existing TFS Workspace mapping.  All are mostly integration tests.
// </summary>
//---------------------------------------------------------------------------------------------------------

using System;
using System.Configuration;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Twine.MSBuild.Tasks.CheckOutFile.Tests
{
    using TFS.Common;

    [TestClass]
    public class CheckOutFileTests
    {
        private CheckOutFile _checkoutFile;

        [TestInitialize]
        public void TestInitialize()
        {
            _checkoutFile = new CheckOutFile();
            _checkoutFile.TfsUri = ConfigurationManager.AppSettings["TfsUri"];
            _checkoutFile.FilePaths = ConfigurationManager.AppSettings["FilePaths"];
        }


        #region TryGetWorkspace Tests

        [TestMethod]
        public void TryGetWorkspace_Success()
        {
            // Assemble
            string filePaths = _checkoutFile.FilePaths;

            // Act
            Workspace workspace = _checkoutFile.TryGetWorkspace(filePaths);

            // Assert
            Assert.IsNotNull(workspace);
        }



        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TryGetWorkspace_EmptyFilePaths_ArgumentNullException()
        {
            // Assemble
            string filePaths = string.Empty;

            // Act
            Workspace workspace = _checkoutFile.TryGetWorkspace(filePaths);

            // Assert
            Assert.IsNull(workspace);
        }

        #endregion TryGetWorkspace Tests


        [TestMethod]
        public void Execute_Success()
        {
            string tfsUri = _checkoutFile.TfsUri;
            string filePaths = _checkoutFile.FilePaths;

            try
            {
                // Assemble
                Workspace workspace = _checkoutFile.TryGetWorkspace(filePaths);

                // Act
                _checkoutFile.Execute();

                // Assert
                Assert.IsNotNull(workspace);
            }
            finally
            {
                // Undo any checkouts.
                var tfs = new Tfs(tfsUri);
                tfs.UndoPendingChanges(filePaths);
            }
        }

        

    }


}
