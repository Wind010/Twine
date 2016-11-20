//---------------------------------------------------------------------------------------------------------
// <copyright file="Tfs.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Common class used with MSBuild custom tasks.
// </summary>
//---------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Twine.MSBuild.Tasks.TFS.Common
{

    public class Tfs
    {
        /// <summary>
        /// Constant string for TfsUri;
        /// </summary>
        private const string TFSURI = "TfsUri";

        public readonly string TfsUri = string.Empty;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tfsUri"><see cref="string"/> The TfsUri property passed in from the target.</param>
        public Tfs(string tfsUri)
        { 
            if (string.IsNullOrWhiteSpace(tfsUri))
            {
                throw new ArgumentNullException(TFSURI);
            }

            TfsUri = tfsUri;
        }


        /// <summary>
        /// Get TFS Workspace for given filepath.
        /// </summary>
        /// <param name="filePath">string</param>
        /// <returns><see cref="Workspace"/> for more information.</returns>
        public Workspace GetWorkspace(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("Filepath must be specified.");
            }

            var tpc = new TfsTeamProjectCollection(new Uri(TfsUri));

            // Get a reference to Version Control. 
            var versionControl = (VersionControlServer)tpc.GetService(typeof(VersionControlServer));

            Workspace workspace = versionControl.GetWorkspace(filePath);

            return workspace;
        }


        /// <summary>
        /// Get list of local or server files path for pending edits in TFS.
        /// </summary>
        /// <param name="branchRoot"><see cref="string" - The local path within workspace to search.</param>
        /// <param name="localItem"><see cref="bool"/>  - If true will return local path, otherwise the server path is returned.</param>
        /// <returns><see cref="List{string}"/></returns>
        public List<string> GetPendingEdits(string branchRoot, bool localItem)
        {
            if (string.IsNullOrWhiteSpace(branchRoot))
            {
                throw new ArgumentNullException("BranchRoot must be specified.");
            }

            var filePath = new List<string>();

            Workspace workspace = GetWorkspace(branchRoot);

            // Get the pending changes to the filePath
            PendingChange[] pendingChanges = workspace.GetPendingChanges(branchRoot, RecursionType.Full);

            foreach(var files in pendingChanges)
            {
                if (localItem)
                {
                    filePath.Add(files.LocalItem);
                }
                else
                {
                    filePath.Add(files.ServerItem);
                }
            }

            return filePath;
        }


    }
}
