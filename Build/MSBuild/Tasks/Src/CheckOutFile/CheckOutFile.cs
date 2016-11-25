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
    
    public sealed class CheckOutFile : TwineTask
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
                var fps = FilePaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string filePath in fps)
                {

                    // Getting workspace can fail intermittently.  We retry here.
                    workspace = Retry.Do<Workspace>(() => TryGetWorkspace(filePath), TimeSpan.FromSeconds(5), 5);

                    if (Path.GetExtension(filePath) != ".wxi")
                    {
                        CheckOut(workspace, filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger(MessageImportance.High, string.Format("Failed to checkout file:  {0}", ex));

                return false;
            }

            return true;
        }


        /// <summary>
        /// Check out the specified file(s).
        /// </summary>
        /// <param name="workspace"><see cref="Workspace"/></param>
        /// <param name="filePath">string - Filepath(s) for files to be checked out.</param>
        public void CheckOut(Workspace workspace, string filePath)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException("Could not get workspace.");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("Filepath must be specified.");
            }

            // No need to sync latest since the build should have done this already.

            Logger(MessageImportance.High, string.Format("Checking out '{0}' for edit.", filePath));

            PendingChange[] pendingChanges = workspace.GetPendingChanges(filePath, RecursionType.Full);

            // If no pending change for the filePath was found it means the file is can be checked out
            if (pendingChanges.Length == 0)
            {
                // Check out the file
                workspace.PendEdit(filePath, RecursionType.Full);
            }
        }


        /// <summary>
        /// Wrapper to obtain workspace.
        /// </summary>
        /// <param name="filePath"><see cref="string"/></param>
        /// <returns><see cref="Workspace"/></returns>
        public Workspace TryGetWorkspace(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("Filepath must be specified.");
            }

            Logger(MessageImportance.High, string.Format("Attempting to get workspace for '{0}'.", filePath));

            var tfs = new Tfs(TfsUri);
            Workspace workspace = tfs.GetWorkspace(filePath);

            if (workspace == null)
            {
                throw new NullReferenceException("Could not get workspace.");
            }

            return workspace;
        }


    }
}

