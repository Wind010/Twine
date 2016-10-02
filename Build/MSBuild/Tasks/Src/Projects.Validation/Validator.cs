//---------------------------------------------------------------------------------------------------------
// <copyright file="Validator.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//     Program which allows for alteration and validation of MSBuild projects
//     that are specific to the Visual One build environment.    Expected to be
//     called from the Twine.ProjectValidation.targets only if 
//     '$(ValidateProjects)' == 'true'.
// </summary>
//---------------------------------------------------------------------------------------------------------


using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Construction;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Twine.MSBuild.Tasks.ProjectValidation
{
    using Twine.Build.MSBuild.Projects.Configuration;
    using Twine.Common.Logging.Adapters;  // Replace later.

    using TFS.Common;

    public sealed class Validator : Task
    {
        // Required property indicated by Required attribute

        /// <summary>
        /// The TFS Uri.
        /// </summary>
        [Required]
        public string TfsUri { get; set; }

        /// <summary>
        /// The BranchRoot\BranchLeaf path.
        /// </summary>
        [Required]
        public string BranchPath { get; set; }

        /// <summary>
        /// The local path of the project getting compiled.
        /// </summary>
        [Required]
        public string ProjectFilePath { get; set; }

        /// <summary>
        /// This method is called automatically when the task is run.
        /// </summary>
        /// <returns>Boolean to indicate if the task was successful.</returns>
        public override bool Execute()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TfsUri))
                {
                    throw new ArgumentNullException("TfsUri");
                }

                if (string.IsNullOrWhiteSpace(BranchPath))
                {
                    throw new ArgumentNullException("BranchPath");
                }

                if (string.IsNullOrWhiteSpace(ProjectFilePath))
                {
                    throw new ArgumentNullException("ProjectFilePath");
                }

                
                Log.LogMessage("TfsUri=" + TfsUri);
                Log.LogMessage("BranchPath=" + BranchPath);
                Log.LogMessage("ProjectFilePath=" + ProjectFilePath);


                // Get pending edits in TFS.  If changes exist then it's a gated build.
                // If the ProjectFilePath matches a file open for edit then process it.
                // If no files are open for edit, then it's a daily build and we just process ProjectFilePath.

                var log = new LoggingDelegates(OnInfo, OnWarn, OnError);
                string ext = Path.GetExtension(ProjectFilePath);

                var tfs = new Tfs(TfsUri);
                List<string> files = tfs.GetPendingEdits(BranchPath, true);

                if (files.Count == 0)
                {
                    Log.LogMessage("This is a daily build since no files are open for edit.");

                    // Daily Build:
                    var utility = new ProjectUtility(ProjectFilePath, log);
                    utility.ValidateProject(true);
                    return true;
                }

                if (files.Contains(ProjectFilePath, StringComparer.CurrentCultureIgnoreCase))
                {
                    Log.LogMessage(string.Format("This is a gated build since {0} are open for edit.", files.Count));

                    // Gated Build:
                    var utility = new ProjectUtility(ProjectFilePath, log);
                    utility.ValidateProject(true);
                }
                else
                {
                    string msg = string.Format("This is a gated build, but this project ({0}) is not part of the shelveset.", 
                        ProjectFilePath);

                    Log.LogMessage(msg, LoggerVerbosity.Diagnostic);
                }

                return true;
            }
            catch(Exception ex)
            {
                Log.LogErrorFromException(ex);
            }

            return false;
        }


        private void OnInfo(string message)
        {
            Log.LogMessage(message);
        }

        private void OnWarn(string message)
        {
            Log.LogWarning(message);
        }

        private void OnError(string message)
        {
            Log.LogError(message);
        }
    }


}

