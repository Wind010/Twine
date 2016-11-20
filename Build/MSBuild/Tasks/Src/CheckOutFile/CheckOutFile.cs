//---------------------------------------------------------------------------------------------------------
// <copyright file="CheckOutFile.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Check out specified file.
// </summary>
//---------------------------------------------------------------------------------------------------------

using System;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Build.Utilities;

namespace Twine.MSBuild.Tasks.CheckOutFile
{

    using TFS.Common;
    
    public sealed class CheckOutFile : Task
    {
        [Required]
        public string TfsUri { get; set; }

        /// <summary>
        /// FilePaths(s) of file to be checked in. Semi-Colon(;) separated paths.  
        /// Also supports all pending changes by specifying 'AllPendingChanges'.
        /// </summary>
        [Required]
        public string FilePaths { get; set; }


        public override bool Execute()
        {
            try
            {
                Workspace workspace = null;

                // Get the pending changes to the filePaths
                var fps = FilePaths.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string filePath in fps)
                {
                    Log.LogMessage(MessageImportance.High, string.Format("Checking out {0} for edit.", filePath));

                    if (workspace == null)
                    {
                        var tfs = new Tfs(TfsUri);
                        workspace = tfs.GetWorkspace(filePath);
                    }

                    if (Path.GetExtension(filePath) != ".wxi")
                    {
                        CheckOut(workspace, filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.High, string.Format("Failed to checkout file:  {0}", ex));
                return false;
            }

            return true;
        }



        /// <summary>
        /// Check out the specified file(s).
        /// </summary>
        /// <param name="workspace">Workspace</param>
        /// <param name="filePath">string - Filepath(s) for files to be checked out.</param>
        public void CheckOut(Workspace workspace, string filePath)
        {
            if (workspace == null)
            {
                throw new NullReferenceException("Could not get workspace.");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new NullReferenceException("Filepath must be specified.");
            }

            // No need to sync latest since the build should have done this already.

            PendingChange[] pendingChanges = workspace.GetPendingChanges(filePath, RecursionType.Full);

            // If no pending change for the filePath was found it means the file is can be checked out
            if (pendingChanges.Length == 0)
            {
                // Check out the file
                workspace.PendEdit(filePath, RecursionType.Full);
            }
        }
        


    }
}

