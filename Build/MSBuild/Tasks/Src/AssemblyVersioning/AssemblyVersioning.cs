//---------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyVersioning.cs" holder="Jeff Jin Hung Tong">
//     Copyright (c) Jeff Tong.  All rights reserved.
// </copyright>
// <summary>
//      Increment Assemblyinfo.vb/cs.  Used in conjunction with CheckOutFile and CheckInFile.
// </summary>
//---------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Twine.MSBuild.Tasks.AssemblyVersioning
{

    public class AssemblyVersioning : Task
    {
        #region Properties

        /// <summary>
        /// Specify the path of the file to replace occurences of the regular 
        /// expression with the replacement text
        /// </summary>
        [Required]
        public string FilePaths
        {
            get; set;
        }

        /// <summary>
        /// Specify the path of the file that contains the version number to increment
        /// </summary>
        [Required]
        public string VersionFilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Specify the path of the file that contains the version number to increment
        /// </summary>
        [Required]
        public string IsNewVersion
        {
            get;
            set;
        }


        #endregion Properties


        #region Methods


        /// <summary>
        /// Updates the assembly version number (AssemblyVersion and/or AssemblyFileVersion) in the passed file - this 
        /// is expected to be an AssemblyInfo file containing the string "1.0.0.0".
        /// </summary>
        /// <exception cref="ArgumentException">If the file passed in the specified FilePaths does not contain "1.0.0.0", 
        /// an ArgumentException is thrown.</exception>
        public override bool Execute()
        {
            try
            {
                // Throw if the file path is null or empty
                if (String.IsNullOrEmpty(VersionFilePath))
                {
                    throw new ArgumentException("Specify a path to the version.txt file", "VersionFilePath");
                }

                if (String.IsNullOrEmpty(FilePaths))
                {
                    throw new ArgumentException("Specify a path to replace text in", "FilePath");
                }

                Version version = IncrementVersion(VersionFilePath, Boolean.Parse(IsNewVersion));
                if (version == new Version())
                {
                    Log.LogError("Failed to increment version file!", null);
                    return false;
                }

                string[] filePaths = FilePaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string filePath in filePaths)
                {
                    // Throw if the file path is null or empty
                    if (String.IsNullOrEmpty(filePath))
                    {
                        throw new ArgumentException("Filepath variable was empty!", filePath);
                    }

                    // Throw if the specified file path doesn't exist
                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException("File not found", filePath);
                    }

                    if (Path.GetExtension(filePath) == ".wxi")
                    {
                        IncrementWixProjects(filePath, version);
                    }
                    else if (Path.GetExtension(filePath) == ".sql")
                    {
                        IncrementSqlRunMeFiles(filePath, version);
                    }
                    else if (Path.GetExtension(filePath) == ".sqlproj")
                    {
                        IncrementSqlProjectFiles(filePath, version);
                    }
                    else
                    {
                        IncrementVersionAssemblyInfo(filePath, version);
                    }
                }

                Log.LogMessage(MessageImportance.High, "Finished incrementing assemblies.");
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.High, string.Format("Failed to increment version:  {0}"
                                                                     , ex));
                return false;
            }

            return true;
        }


 
        #endregion Methods


        #region Helper Methods


        public Version IncrementVersion(string filePath, bool isNewVersion)
        {
            string text;

            Encoding encoding;
            using (var reader = new StreamReader(filePath))
            {
                text = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
            }

            Log.LogMessage(MessageImportance.High, "IncrementVersion: file '" + filePath + "' being updated.");

            Boolean fileChanged = false;
            Version currentVersion =  null;
            Version newVersion = null;
            
            try
            {
                currentVersion = new Version(text);
                if (isNewVersion)
                { 
                    newVersion = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build + 1, currentVersion.Revision);
                }
                else
                {
                    newVersion = currentVersion;
                }
            }
            catch(Exception ex)
            {
                Log.LogError("IncrementVersion: Error getting / setting version info:" + ex.Message, null);
                return null;
            }
            
            Log.LogMessage(MessageImportance.High, "IncrementVersion: MatchedValue = " + currentVersion.ToString());
            Log.LogMessage(MessageImportance.High, "IncrementVersion: New Version  = " + newVersion.ToString());
            
            // Ensure that the file is writeable
            FileAttributes fileAttributes = File.GetAttributes(filePath);
            File.SetAttributes(filePath, fileAttributes & ~FileAttributes.ReadOnly);

            string newText = text.Replace(currentVersion.ToString(), newVersion.ToString());
            File.WriteAllText(filePath, newText, encoding);

            // Restore the file's original attributes
            File.SetAttributes(filePath, fileAttributes);
            Log.LogMessage(MessageImportance.High, "IncrementVersion: file '" + filePath + "' saved.");

            return newVersion;

        }


        /// <summary>
        /// Search specified file for version information matching the given attributes.
        /// </summary>
        /// <param name="filePath">string - Filepath to AssemblyInfo.cs/vb</param>
        /// <param name="attributes">IEnumerable of String - The AssemblyVersion tags to update.</param>
        public Version IncrementVersionAssemblyInfo(string filePath, Version newVersion)
        {
            string text;

            Encoding encoding;
            using (var reader = new StreamReader(filePath))
            {
                text = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
            }

            Log.LogMessage(MessageImportance.High, "IncrementVersionAssemblyInfo: file '" + filePath + "' being updated.");

            Boolean fileChanged = false;
            
            // Get file attributes
            FileAttributes fileAttributes = File.GetAttributes(filePath);

            var attributes = new List<string>();

            attributes.Add("AssemblyVersion");
            attributes.Add("AssemblyFileVersion");
            attributes.Add("AssemblyInformationalVersion");
   

            foreach (string attribute in attributes)
            {
                var regex = new Regex(attribute + @"\(""\d+\.\d+\.\d+\.\d+""\)");
                var match = regex.Match(text);

                if (match.Success)
                {
                    fileChanged = true;
                    
                    Log.LogMessage(MessageImportance.High, "IncrementVersionAssemblyInfo: MatchedValue = " + match.Value);
                    Log.LogMessage(MessageImportance.High, "IncrementVersionAssemblyInfo: New Version  = " + newVersion);

                    // Ensure that the file is writeable
                    File.SetAttributes(filePath, fileAttributes & ~FileAttributes.ReadOnly);

                    text = regex.Replace(text,  attribute + "(\"" + newVersion + "\")");
                    
                }
            }

            if (fileChanged)
            {
                File.WriteAllText(filePath, text, encoding);

                // Restore the file's original attributes
                File.SetAttributes(filePath, fileAttributes);
                Log.LogMessage(MessageImportance.High, "IncrementVersionAssemblyInfo: file '" + filePath + "' saved.");
            }
           
            return newVersion;
        }

        /// <summary>
        /// Increment the build version in the specified .wxi file.  The variable is defined as "BuildVersion".
        /// </summary>
        /// <param name="filePath">string - file path to the .wxi containing build version.</param>
        /// <param name="version">Version - The new version to replace the old.</param>
        public void IncrementWixProjects(string filePath, Version version)
        {
            string text;
            Encoding encoding;
            using (var reader = new StreamReader(filePath))
            {
                text = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
            }

            Log.LogMessage(MessageImportance.High, "IncrementWixProjects: file '" + filePath + "' being updated.");

            const string attribute = "BuildVersion";

            var regex = new Regex(attribute + @"\=""\d+\.\d+\.\d+\.\d+\""");
            var match = regex.Match(text);

            if (match.Success)
            {
                Log.LogMessage(MessageImportance.High, "IncrementWixProjects: MatchedValue = " + match.Value);
                Log.LogMessage(MessageImportance.High, "IncrementWixProjects: New Version  = " + version);
                // Ensure that the file is writeable
                FileAttributes fileAttributes = File.GetAttributes(filePath);
                File.SetAttributes(filePath, fileAttributes & ~FileAttributes.ReadOnly);

                text = regex.Replace(text, attribute + "=\"" + version + "\"");
                File.WriteAllText(filePath, text, encoding);

                // Restore the file's original attributes
                File.SetAttributes(filePath, fileAttributes);
                Log.LogMessage(MessageImportance.High, "IncrementWixProjects: file '" + filePath +"' saved.");
            }
        }


        /// <summary>
        /// Increment the build version in the SQL runme files.".
        /// </summary>
        /// <param name="filePath">string - file path to the .sql containing build version.</param>
        /// <param name="version">Version - The new version to replace the old.</param>
        public void IncrementSqlRunMeFiles(string filePath, Version version)
        {
            bool changesMade = false;
            string text;
            Encoding encoding;


            using (var reader = new StreamReader(filePath))
            {
                text = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
            }

            Log.LogMessage(MessageImportance.High, "IncrementSqlRunMeFiles: file '" + filePath + "' being updated.");

            string[] attributes = new string[4] { @"DECLARE.+VersionMajor.*=.*\'", 
                                                  @"DECLARE.+VersionMinor.*=.*\'", 
                                                  @"DECLARE.+VersionBuild.*=.*\'", 
                                                 @"DECLARE.+VersionHotFix.*=.*\'"};

            foreach (string attrib in attributes)
            {
                var pattern = attrib + @"\d+\'";
                Log.LogMessage(MessageImportance.High, "IncrementSqlRunMe: Finding regex: " + pattern);
                
                //var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    Log.LogMessage(MessageImportance.High, "IncrementSqlRunMe: MatchedValue = " + match.Value);

                    changesMade = true;
                    var replaceVal = string.Empty;
                    if (attrib.Contains("VersionMajor"))
                    {
                        replaceVal = String.Format("DECLARE @VersionMajor AS VARCHAR({0}) = '{1}'", version.Major.ToString().Length, version.Major.ToString());
                    }
                    else if (attrib.Contains("VersionMinor"))
                    {
                        replaceVal = String.Format("DECLARE @VersionMinor AS VARCHAR({0}) = '{1}'", version.Minor.ToString("##0").Length, version.Minor.ToString("##0"));
                    }
                    else if (attrib.Contains("VersionBuild"))
                    {
                        replaceVal = String.Format("DECLARE @VersionBuild AS VARCHAR({0}) = '{1}'", version.Build.ToString("###0").Length, version.Build.ToString("###0"));
                    }
                    else if (attrib.Contains("VersionHotFix"))
                    {
                        replaceVal = String.Format("DECLARE @VersionHotFix AS VARCHAR({0}) = '{1}'", version.Revision.ToString("##0").Length, version.Revision.ToString("##0"));
                    }

                    Log.LogMessage(MessageImportance.High, "IncrementSqlRunMe: New Version  = " + replaceVal);
                    text = Regex.Replace(text, pattern, replaceVal, RegexOptions.IgnoreCase);

                }
                else
                {
                    throw new Exception("Unable to find regex pattern in file (this is an error) : " + pattern);
                }
            }

            if (changesMade)
            {
                // Ensure that the file is writeable
                FileAttributes fileAttributes = File.GetAttributes(filePath);
                File.SetAttributes(filePath, fileAttributes & ~FileAttributes.ReadOnly);

                File.WriteAllText(filePath, text, encoding);
                
                // Restore the file's original attributes
                File.SetAttributes(filePath, fileAttributes);
                Log.LogMessage(MessageImportance.High, "IncrementSqlRunMeFiles: file '" + filePath + "' saved.");
            }

          
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="version"></param>
        public void IncrementSqlProjectFiles(string filePath, Version version)
        {
            string text;
            Encoding encoding;
            using (var reader = new StreamReader(filePath))
            {
                text = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
            }

            Log.LogMessage(MessageImportance.High, "SQL Project file '" + filePath + "' being updated.");

            XmlDocument xml = new XmlDocument();

            try
            {
                xml.Load(filePath);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
                nsmgr.AddNamespace("ns1", "http://schemas.microsoft.com/developer/msbuild/2003");
                XmlNode node = xml.SelectSingleNode("//ns1:DacVersion",nsmgr);

                if (node == null)
                {
                    Log.LogMessage(MessageImportance.High, "IncrementSqlProjectFiles: Could not find <DacVersion> node");
                    return;
                }

                Log.LogMessage(MessageImportance.High, "IncrementSqlProjectFiles: MatchedValue = " + node.InnerText);
                Log.LogMessage(MessageImportance.High, "IncrementSqlProjectFiles: New Version  = " + version.ToString());
                node.InnerText = version.ToString();

                // Ensure that the file is writeable
                FileAttributes fileAttributes = File.GetAttributes(filePath);
                File.SetAttributes(filePath, fileAttributes & ~FileAttributes.ReadOnly);
                    
                xml.Save(filePath);

                // Restore the file's original attributes
                File.SetAttributes(filePath, fileAttributes);
                Log.LogMessage(MessageImportance.High, "IncrementSqlProjectFiles: file '" + filePath + "' saved.");

                

            }
            catch (System.Exception ex)
            {
                Log.LogMessage(MessageImportance.High, "IncrementSqlProjectFiles: Exception Occured: " + ex.Message );
                throw;
            }
           
        }

        #endregion Helper Methods


    }

}
