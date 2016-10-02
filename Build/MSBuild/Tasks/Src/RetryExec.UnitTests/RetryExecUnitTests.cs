//---------------------------------------------------------------------------------------------------------
// <copyright file="RetryExecUnitTests.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Run specified file with arguments, retries and time to sleep between retries.
// </summary>
//---------------------------------------------------------------------------------------------------------

using System;
using System.Collections;

namespace Twine.MSBuild.Tasks.RetryExec.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Microsoft.Build.Framework;


    [TestClass]
    public class RetryExecUnitTests
    {
        private RetryExec _retryExec;
        private const string FilePath = @"TestRetryExec.exe";

        [TestInitialize()]
        public void Initialize()
        {
            var customBuildEngine = new CustomBuildEngine();
            _retryExec = new RetryExec
                {
                    BuildEngine = customBuildEngine,
                    FilePath = FilePath,
                    Arguments = "Pass",
                    Retries = 1,
                    TimeToSleep = 1
                };
        }

        [TestMethod]
        public void TestRetryExec_Fail()
        {
            _retryExec.Arguments = "Fail";
            Assert.IsFalse(_retryExec.Execute());
        }

        [TestMethod]
        public void TestRetryExec_Pass()
        {
            _retryExec.Arguments = "Pass";
            Assert.IsTrue(_retryExec.Execute());
        }

        [TestMethod]
        public void TestRetryExec_Exeption_Fail()
        {
            _retryExec.Arguments = "Exception";
            Assert.IsFalse(_retryExec.Execute());
        }

        [TestCleanup()]
        public void Cleanup()
        {
           
        }

    }


    /// <summary>
    /// Mock/Fake BuildEngine class for testing.
    /// </summary>
    public class CustomBuildEngine : IBuildEngine
    {
        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties,
                                     IDictionary targetOutputs)
        {
            throw new NotImplementedException();
        }

        public bool ContinueOnError { get; private set; }
        public int LineNumberOfTaskNode { get; private set; }
        public int ColumnNumberOfTaskNode { get; private set; }
        public string ProjectFileOfTaskNode { get; private set; }
    }
}
