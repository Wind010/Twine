//---------------------------------------------------------------------------------------------------------
// <copyright file="RetryExec.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Run specified file with arguments, retries and time to sleep between retries.
// </summary>
//---------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Twine.MSBuild.Tasks.RetryExec
{

    public class RetryExec : Task
    {

        #region Properties

        /// <summary>
        /// Path of file to run.
        /// </summary>
        [Required]
        public string FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Arguments to run the specified file with.
        /// </summary>
        [Required]
        public string Arguments
        {
            get;
            set;
        }

        /// <summary>
        /// Number of times to retry
        /// </summary>
        [Required]
        public short Retries
        {
            get;
            set;
        }

        /// <summary>
        /// Time to sleep before retrying again in seconds.
        /// </summary>
        [Required]
        public int TimeToSleep
        {
            get;
            set;
        }

        #endregion Properties



        /// <summary>
        /// Runs passed in file with retry and time to wait specified.
        /// </summary>
        /// <returns>bool</returns>
        public override bool Execute()
        {
            bool value = false;
            try
            {
                value = ExecuteFile(FilePath, Arguments, Retries, TimeToSleep);
                Log.LogMessage(MessageImportance.Normal, "Finished executing " + FilePath);
            }
            catch (Exception ex)
            {
                Log.LogError(string.Format("Error:  {0}", ex));
                return false;
            }

            return value;
        }


        /// <summary>
        /// Runs passed in file with retry and time to wait specified.
        /// </summary>
        private bool ExecuteFile(string filePath, string arguments, short retries, int timeToSleep)
        {
            short originalRetry = retries;

            Log.LogMessage(MessageImportance.Normal,
                           string.Format("Running the following command:{0} {1}", filePath, arguments));

            Log.LogMessage(MessageImportance.Low, string.Format("FilePath={0}", filePath));
            Log.LogMessage(MessageImportance.Low, string.Format("Args={0}", arguments));
            Log.LogMessage(MessageImportance.Low, string.Format("Retries={0}", retries));
            Log.LogMessage(MessageImportance.Low, string.Format("Timetosleep={0}", timeToSleep));

            try
            {
                // Throw if the file path is null or empty
                if (String.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("FilePath is not specified", filePath);
                }

                filePath = filePath.Replace("\"", "");

                if (!File.Exists(filePath))
                {
                    throw new ArgumentException("FilePath does not exist.", filePath);
                }

                if (retries < 0)
                {
                    throw new ArgumentException("Retries must be larger than or equal to zero.", filePath);
                }

                if (timeToSleep < 0)
                {
                    throw new ArgumentException("TimeToWait must be larger zero.", filePath);
                }

                do
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                            {
                                FileName = filePath,
                                Arguments = arguments,
                                RedirectStandardError = true,
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };


                        Process proc = Process.Start(psi);
                        //proc.BeginOutputReadLine();
                        //proc.BeginErrorReadLine();
                        string stdOut = proc.StandardOutput.ReadToEnd();
                        string stdErr = proc.StandardError.ReadToEnd();
                        proc.WaitForExit();

                        Log.LogMessage(MessageImportance.Normal, string.Format("StandardOut: {0}", stdOut));

                        int exitCode = proc.ExitCode;
                        if (exitCode != 0)
                        {
                            Log.LogWarning(string.Format("StandardError: {0}", stdErr));

                            psi = null;
                            proc = null;

                            throw new Exception(string.Format("ExitCode: {0} - Retrying", exitCode));
                        }

                        Log.LogMessage(MessageImportance.High, string.Format("Exited successfully with ExitCode: {0}", exitCode));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning(string.Format("Exception:  {0}", ex));

                        if (retries <= 0) throw;  
                        
                        Thread.Sleep(timeToSleep * 1000);  // Miliseconds to seconds.
                        Log.LogMessage(MessageImportance.High, "Retrying...");
                    }
                } while (--retries > 0);

            }
            catch (ArgumentException ex)
            {
                Log.LogError("Error: " + ex);
            }
            catch(Exception)
            {
                Log.LogError(string.Format("Failed to execute command.  Maximum number of retries reached {0}",
                                            originalRetry));
            }

            return false;
        }


    }
}
