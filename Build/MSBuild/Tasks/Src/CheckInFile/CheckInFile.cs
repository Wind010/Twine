//---------------------------------------------------------------------------------------------------------
// <copyright file="CheckInFile.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Check in specified file.
// </summary>
//---------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.Build.Framework;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Build.Utilities;

namespace Twine.MSBuild.Tasks.CheckInFile
{

    using TFS.Common;

    public sealed class CheckInFile : Task
    {
        [Required]
        public string TfsUri { get; set; }

        [Required]
        public string Reason { get; set; }

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
                Log.LogMessage(MessageImportance.High, "Executing CheckInFile Task");

                // Obtain the runtime values
                
                var filePaths = FilePaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                var dictMsg = Checkin(filePaths, Reason);

                foreach (KeyValuePair<string, MessageImportance> kvp in dictMsg)
                {
                    Log.LogMessage(kvp.Value, kvp.Key);
                }
                
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.High, string.Format("Failed to checkin file:  {0}", ex));
                return false;
            }

            return true;
        }


        /// <summary>
        /// Check In the specified file(s).
        /// </summary>
        /// <param name="filePaths"><see cref="string[]"/>  - Filepaths for files to be checked in.</param>
        /// <param name="reason"><see cref="string"/>  - Reason for checking in files.</param>
        /// <returns><see cref="List{KeyValuePair{String, MessageImportance}"/></returns>
        public List<KeyValuePair<string, MessageImportance>> Checkin(string[] filePaths, string reason)
        {
            var messages = new List<KeyValuePair<string, MessageImportance>>();

            if (filePaths.Length == 0)
            {
                string msg = "No files in filePaths to checkin.";
                Log.LogMessage(MessageImportance.High, msg);

                messages.Add(new KeyValuePair<string, MessageImportance>(msg, MessageImportance.High));

                return messages;
            }

            var tfs = new Tfs(TfsUri);
            Workspace workspace = tfs.GetWorkspace(filePaths[0]);

            // Get the pending changes to the filePath
            PendingChange[] pendingChanges = workspace.GetPendingChanges(filePaths, RecursionType.Full);

            // If no pending change for the filePath was found it means the file is can be checked out
            if (pendingChanges.Length > 0)
            {
                //if (filePaths[0].Equals("allpendingchanges", StringComparison.OrdinalIgnoreCase))
                //{
                //    // Checkin pending changes
                //    workspace.CheckIn(pendingChanges, "Build Agent", "***NO_CI***", null, null,
                //                      new PolicyOverrideInfo("Auto Checkin:  " + reason, null),
                //                      CheckinOptions.SuppressEvent);
                //}
                //else
                {
                    var changes = new List<PendingChange>();

                    foreach (string filePath in filePaths)
                    {
                        foreach (PendingChange pendingChange in pendingChanges)
                        {
                            if (filePath.Equals(pendingChange.LocalItem, StringComparison.OrdinalIgnoreCase))
                            {
                                messages.Add(new KeyValuePair<string, MessageImportance>("Pending Change:  "
                                    + pendingChange.LocalOrServerItem, MessageImportance.Normal));

                                changes.Add(pendingChange);
                                break;
                            }
                        }
                    }

                    string comment = string.Format("***NO_CI*** Auto Checkin: - {0}", reason);
                    var wip = new WorkspaceCheckInParameters(pendingChanges, comment)
                    {
                        Author = Environment.UserName,
                        OverrideGatedCheckIn = true,
                        PolicyOverride = new PolicyOverrideInfo("Auto Checkin:  " + reason, null)
                    };

                    // Checkin matching files
                    if (changes.Count > 0)
                    {
                        wip = new WorkspaceCheckInParameters(changes.ToArray(), comment)
                        {
                            Author = Environment.UserName,
                            OverrideGatedCheckIn = true,
                            PolicyOverride = new PolicyOverrideInfo("Auto Checkin:  " + reason, null)
                        };

                        //workspace.CheckIn(changes.ToArray(), "svc.V1TfsBuild", "***NO_CI***", null, null,
                        //                  new PolicyOverrideInfo("Auto Checkin:  " + reason, null),
                        //                  CheckinOptions.SuppressEvent);

                        messages.Add(new KeyValuePair<string, MessageImportance>("Checked in changes.", MessageImportance.Normal));
                    }
                    else
                    {
                        messages.Add(new KeyValuePair<string, MessageImportance>("No matching filepaths found... checking in all pending changes."
                            , MessageImportance.Normal));

                        
                        // Checkin pending changes
                        //workspace.CheckIn(pendingChanges, "Build Agent", "***NO_CI***", null, null,
                        //                  new PolicyOverrideInfo("Auto Checkin:  " + reason, null),
                        //                  CheckinOptions.SuppressEvent);
                    }

                    workspace.CheckIn(wip);
                }
            }
            else
            {
                var verbosity = MessageImportance.Normal;
                if (reason.IndexOf("Versioning", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    verbosity = MessageImportance.High;
                }

                messages.Add(new KeyValuePair<string, MessageImportance>("No pending changes found.  Checkin skipped.", verbosity));
            }


            return messages;
        }



    }
}

