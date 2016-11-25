//---------------------------------------------------------------------------------------------------------
// <copyright file="TwineTask.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Base class that for Twine MSBuild Tasks.
// </summary>
//---------------------------------------------------------------------------------------------------------


using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Twine.MSBuild.Tasks
{
    public class TwineTask : Task
    {
        public override bool Execute()
        {
            return false;
        }


        /// <summary>
        /// A safe wrapper around the Task.Log for unit tests.
        /// </summary>
        /// <param name="messageImportance"><see cref="MessageImportance"/> </param>
        /// <param name="message"><see cref="string"/> </param>
        protected void Logger(MessageImportance messageImportance, string message)
        {
            if (this.BuildEngine != null)
            {
                Log.LogMessage(messageImportance, message);
            }
        }

    }
}
