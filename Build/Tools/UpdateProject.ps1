#-------------------------------------------------------------------------------------------------
# <Copyright file="UpdateProject.ps1" company="Jeff Jin Hung Tong">
#     Copyright (c) Jeff Tong.  All rights reserved.
# </Copyright>
#
# <Authors>
#	  <Author Name="Jeff Tong"  />
# </Authors>
#
# <Parameters>
#	
# </Parameters>
#
# <Description>
#     Functions used to alter MSBuild projects to use Twine. 
#     Allow for search and replace of text in multiple
#     files.
# </Description>
#
# <Remarks />
#
# <Disclaimer />
#-------------------------------------------------------------------------------------------------


# <summary>
#  Search and replace text within specified file extensions.
# </summary>
# <param name="$searchRoot" type="string">The start path to search recursively.  Defaults to %BranchRoot%</param>
# <param name="$search" type="string">string to be replaced.</param>
# <param name="$replace" type="string">New string.</param>
# <param name="$ext" type="string">Project extension.  Defaults to .vbproj</param>
function SearchAndReplace
{
	Param
	(
		[Parameter(Position=0)] [string] $searchRoot=$env:BranchRoot,
		[Parameter(Position=1)] [string] $search,
		[Parameter(Position=2)] [string] $replace, 
		[Parameter(Position=3)] [string] $ext = "*.vbproj"
	)

	try
	{
		$files = get-childitem $searchRoot $ext -recurse

		foreach ($file in $files)
		{
			WriteLog "Processing '$($file.Fullname)' ..."
			
			(Get-Content $file.FullName) | 
			Foreach-Object {$_ -replace $search, $replace} | 
			Set-Content $file.FullName
			
			WriteLog "Replacing '$search' with '$replace'"
		}
	}
	catch
	{
		if ($_){ WriteErrLog "Failure in SearchAndReplace(): $_" }
		elseif ($Error[0]) { $msg = $Error[0].ToString(); WriteErrLog "Failure in AlterXml(): $msg" }
		else { WriteErrLog "Failure in SearchAndReplace()" }
	}
}

# <summary>
#  Search and replace text within specified file extensions.
# </summary>
# <param name="$searchRoot" type="string">The start path to search recursively.  Defaults to %BranchRoot%</param>
# <param name="$search" type="string">string to be replaced.</param>
# <param name="$replace" type="string">New string.</param>
# <param name="$ext" type="string">Project extension.  Defaults to .vbproj</param>
function AlterLicx
{
	Param
	(
		[Parameter(Position=0)] [string] $searchRoot=$env:BranchRoot,
		[Parameter(Position=3)] [string] $ext = "*.vbproj"
	)


	$files = get-childitem $searchRoot $ext -recurse

	foreach($path in $files)
	{	
		if ($path)
		{
			[string] $fullPath = $path.FullName
			WriteLog "Analyizing '$fullPath'"
			$tmp = $fullPath + ".tmp"
			
			[bool] $changed = $false
			
			$reader = [System.IO.File]::OpenText($fullPath)
			$tries = 0 
			while (($reader -eq $null) -and ($tries -lt 3))
		    {
		        $tries++
				Start-Sleep -Seconds 3
				WriteLog "Failed to open reader for '$fullPath'.  Attempting try $tries".
				$reader = [System.IO.File]::OpenText($fullPath)
		    }
			
			if ($reader -eq $null)
			{
				throw  "Failed to open reader for '$fullPath'.  Attempted $tries tries."
			}
			
			
			$sw = [System.IO.StreamWriter] "$tmp"
			$tries = 0
			while (($sw -eq $null) -and ($tries -lt 3))
		    {
		        $tries++
				Start-Sleep -Seconds 3
				WriteLog "Failed to open StreamWriter for '$tmp'.  Attempting try $tries".
				$sw = [System.IO.StreamWriter] "$tmp"
		    }
			
			if ($sw -eq $null)
			{
				throw  "Failed to open reader for '$fullPath'.  Attempted $tries tries."
			}
			
			try 
			{
				if ($reader)
				{
					while( ($line = $reader.ReadLine()) -ne $null)  
					{	
						foreach ($ref in $global:ProjectReferences)
						{
							#$reference = $ref.Replace("v11.2","v11.1").Replace("11.2.20112.1010", "11.1.20111.2009").Replace(", processorArchitecture=MSIL", "")
							#$newRefence = $ref.Replace(", processorArchitecture=MSIL", "")
							$newReference = $line.Replace("v11.1", "v11.2").Replace("11.1.20111.2009", "11.2.20112.1010")
							if ($line.contains("v11.1"))
							{
								$sw.WriteLine($newReference)
								$changed = $true
								WriteLog "Replacing '$line' with $newReference'"
								break
							}
						}
					}
				}
			}
			catch
			{
				if ($_){ WriteErrLog "Failure in SearchAndReplace(): $_" }
				elseif ($Error[0]) { $msg = $Error[0].ToString(); WriteErrLog "Failure in AlterXml(): $msg" }
				else { WriteErrLog "Failure in SearchAndReplace()" }
			}
			finally 
			{
			    $reader.Close()
				$reader.Dispose()
				$sw.Flush()
				$sw.Close()
				$sw.Dispose()
				[System.GC]::Collect();
			}
			
			if ($changed)
			{
				Remove-Item $fullPath -force -ErrorAction stop
				Rename-Item $tmp $fullPath -ErrorAction stop
			}
			else
			{
				Remove-Item $tmp -force -ErrorAction stop
				WriteLog "No changes made."
			}
		}
	}
}


# <summary>
#  Alter MSBuild project files.
# </summary>
# <param name="$searchRoot" type="string">The start path to search recursively.  Defaults to %BranchRoot%</param>
# <param name="$ext" type="string">Project extension.  Defaults to .vbproj</param>
function AlterProjects
{
	Param
	(	
		[Parameter(Position=0)] [string] $searchRoot=$env:BranchRoot,
		[Parameter(Position=1)] [string] $ext = "*.vbproj"
	)
  
  try
  {
	  $files = Get-Childitem $searchRoot $ext -recurse
	  
	  if ($files -ne $null)
	  {
			foreach ($file in $files)
			{
				WriteLog "Processing '$file' ..."

				$xml = [xml](gc $file.Fullname)
				
				$ext = "*" + [System.IO.Path]::GetExtension($file.Name)
				
				# Necessary for select node use for MSBuild Projects.
				$nsmgr = New-Object System.Xml.XmlNamespaceManager -ArgumentList $xml.NameTable
				$nsmgr.AddNamespace('MSBuild','http://schemas.microsoft.com/developer/msbuild/2003')


				ImportEnvironmentProps $xml $nsmgr


				# Set to Debug and x86.
				WriteLog "Updating to Debug and x86"
				$xml.Project.PropertyGroup[0].Configuration.'#text' = "Debug"
				$xml.Project.PropertyGroup[0].Platform.'#text' = "x86"

				#$xml.'#comment' = ""

				AlterToUseGenericAssemblyInfo $xml $ext

				RemoveUnnecessaryNodes $xml $nsmgr
				
				[string] $type = DetermineSettingsType $file.FullName
				AddGlobalSettings $xml $ext $type
				
				AddCommonAssemblyInfo $xml $nsmgr $ext
				
				# Update References
				UpdateReference $xml $xdNS $nsmgr "" "" "" $false "" "`$(BinPath)\Ext\Infragistics\11.2" 
				
				ImportCommonTargets $xml 
				
				# Validate the alterations.
				[string] $validationError = ValidateProjectFile $xml $nsmgr $ext $type
				if ($validationError -ne "")
				{
					throw $validationError
				}
				
				#Debugging
				$xml.Save($($file.Fullname) + '.xml')

				#$xml.Save($file)
				
				WriteLog "Saving $($file.Fullname)"
			}
		}
		else
		{
			WriteLog "No projects found to process at '$searchRoot'."
		}
		
		WriteLog "Finished Altering Projects."
	}
	catch
	{
		if ($_){ WriteErrLog "Failure in AlterProjects(): $_" }
		elseif ($Error[0]) { $msg = $Error[0].ToString(); WriteErrLog "Failure in AlterXml(): $msg" }
		else { WriteErrLog "Failure in AlterProjects()" }
	}
}


# <summary>
#  Add the import to Environment.Props
# </summary>
# <param name="$searchRoot" type="string">The start path to search recursively.  Defaults to %BranchRoot%</param>
# <param name="$ext" type="string">Project extension.  Defaults to .vbproj</param>
function ImportEnvironmentProps
{
	Param
	(	
		[Parameter(Position=0)] [xml] [ref] $xml = $(throw "xml is a required parameter"),
		[Parameter(Position=1)] [System.Xml.XmlNamespaceManager] $nsmgr = $(throw "$nsmgr is a required parameter")
	)
	
	WriteLog "Adding import of Environment.Props"

	# For avoiding xmlns="" when creating element.
	$xdNS = $xml.DocumentElement.NamespaceURI  


	$nodePropImport = $xml.SelectSingleNode("//MSBuild:Import[contains(@Project,'Environment.props')]", $nsmgr)
	if ($nodePropImport)
	{
		WriteLog "Project already modified to use global settings.  Skipping..."
		continue
	}

	$nodePropImport = $xml.CreateElement("Import", $xdNS)
	$nodePropImport.SetAttribute("Project", "`$(BranchRoot)\Build\MsBuild\Props\Environment.props")
	$output = $xml.Project.InsertBefore($nodePropImport, $xml.Project.PropertyGroup[0]) | Out-String
	WriteLog $output
}

# <summary>
#  Add the Common/Generic assembly info.
# </summary>
# <param name="$xml" type="XmlDocuement">The project file xml.</param>
# <param name="$ext" type="string">Project extension.</param>
function AlterToUseGenericAssemblyInfo
{
	Param
	(	
		[Parameter(Position=0)] [xml] [ref] $xml = $(throw "xml is a required parameter"),
		[Parameter(Position=1)] [string] $ext = $(throw "ext is a required parameter")
	)
	
	$xdNS = $xml.DocumentElement.NamespaceURI  

	if ($ext -ieq "*.wixproj")
	{
		return
	}
	
	if ($ext -ieq "*.csproj")
	{
		$UseGenericAssembly = "UseGenericAssemblyInfoCS"
	}
	elseif ($ext -ieq "*.vbproj")
	{
		$UseGenericAssembly = "UseGenericAssemblyInfoVB"
	}

	$nodeGenericAssemblyInfo = $xml.CreateElement($UseGenericAssembly, $xdNS)
	$nodeGenericAssemblyInfo.InnerText = 'true'
	$xml.Project.PropertyGroup[0].AppendChild($nodeGenericAssemblyInfo)
}


# <summary>
#  Remove local configuration settings
# </summary>
# <param name="$xml" type="XmlDocuement">The project file xml.</param>
# <param name="$type" type="string">Settings type.</param>
function RemoveLocalProjectSettings
{
	Param
	(	
		[Parameter(Position=0)] [xml] [ref] $xml = $(throw "xml is a required parameter")
	)
	
	# Remove uneeded PropertyGroups
	$nodesPropertyGroups = $xml.SelectNodes("//MSBuild:PropertyGroup", $nsmgr)
	foreach ($pg in $nodesPropertyGroups)
	{
		if ($pg.HasAttribute("Condition"))
		{
			WriteLog "Removing PropertyGroup:"
			$output = $pg.ParentNode.RemoveChild($pg) | Out-String
			WriteLog $output
		}
	}
}


# <summary>
#  Add global configuration settings
# </summary>
# <param name="$xml" type="XmlDocuement">The project file xml.</param>
# <param name="$ext" type="string">Project extension.</param>
# <param name="$type" type="string">Settings type.</param>
function AddGlobalSettings
{
	Param
	(	
		[Parameter(Position=0)] [xml] $xml = $(throw "xml is a required parameter"),
		[Parameter(Position=1)] [string] $ext = $(throw "ext is a required parameter"),
		[Parameter(Position=2)] [string] $type = $(throw "type is a required parameter")
	)
	
	# For avoiding xmlns="" when creating element.
	$xdNS = $xml.DocumentElement.NamespaceURI  

	# Import global settings file.
	$nodeSettingsImport = $xml.CreateElement("Import", $xdNS)
	if ($ext -ieq "*.wixproj")
	{
		$nodeSettingsImport.SetAttribute("Project", "`$(SettingsPath)\Twine.Wix.Settings")
	}
	else
	{
		if ($type -ieq "Test")
		{
			$nodeSettingsImport.SetAttribute("Project", "`$(SettingsPath)\Twine.Test.Settings")
		}
		else
		{
			$nodeSettingsImport.SetAttribute("Project", "`$(SettingsPath)\Twine.Core.Build.Settings")
		}
	}
	
	if ($xml.Project.PropertyGroup[0] -eq $null)
	{
		$output = $xml.Project.InsertAfter($nodeSettingsImport, $xml.Project.PropertyGroup) | Out-String 
	}
	else
	{
		$output = $xml.Project.InsertAfter($nodeSettingsImport, $xml.Project.PropertyGroup[0]) | Out-String 
	}
	WriteLog $output

	return $xml
}


# <summary>
#  Add the CommonAssemblyInfo
# </summary>
# <param name="$xml" type="XmlDocuement">The project file xml.</param>
function AddCommonAssemblyInfo
{
	Param
	(	
		[Parameter(Position=0)] [xml] $xml = $(throw "xml is a required parameter"),
		[Parameter(Position=1)] [System.Xml.XmlNamespaceManager] $nsmgr = $(throw "$nsmgr is a required parameter"),
		[Parameter(Position=2)] [string] $ext = $(throw "ext is a required parameter")
	)
	
	if ($ext -ne "*.wixproj")
	{
		if ($ext -eq "*.csproj")
		{
			$nodeAssemblyInfo = $xml.SelectSingleNode("//MSBuild:Compile[contains(@Include,'Properties\AssemblyInfo.cs')]", $nsmgr)
		}
		elseif ($ext -eq "*.vbproj")
		{
			$nodeAssemblyInfo = $xml.SelectSingleNode("//MSBuild:Compile[contains(@Include,'My Project\AssemblyInfo.vb')]", $nsmgr)
		}
		
		if ($nodeAssemblyInfo -ne $null)
		{

			# For avoiding xmlns="" when creating element.
			$xdNS = $xml.DocumentElement.NamespaceURI  
			
			$nodeCompile = $xml.CreateElement("Compile", $xdNS)
	
			if ($ext -eq "*.csproj")
			{
				$nodeCompile.SetAttribute("Include", "`$(CommonAssemblyInfoPath)\CommonAssemblyInfo.cs")	
			}
			elseif ($ext -eq "*.vbproj")
			{
				$nodeCompile.SetAttribute("Include", "`$(CommonAssemblyInfoPath)\CommonAssemblyInfo.vb")	
			}
			
			$nodeLink = $xml.CreateElement("Link", $xdNS)
			$nodeLink.InnerText = "Properties\CommonAssemblyInfo.cs"
			
			if ($ext -eq "*.vbproj")
			{
				$nodeLink.InnerText = "Properties\CommonAssemblyInfo.vb"
			}
			
			$nodeCompile.AppendChild($nodeLink)
			
			$nodeAssemblyInfo.ParentNode.InsertBefore($nodeCompile, $nodeAssemblyInfo) | Out-Null
		}
	}

	return $xml
}


# <summary>
#  Add the import to for Common.Targets
# </summary>
# <param name="$xml" type="XmlDocument">The project file xml.</param>
# <returns>String</returns>
function ImportCommonTargets
{
	Param
	(	
		[Parameter(Position=0)] [xml] $xml = $(throw "xml is a required parameter")
	)
	
	WriteLog "Adding import of Twine.Common.targets"

	# For avoiding xmlns="" when creating element.
	$xdNS = $xml.DocumentElement.NamespaceURI  
	
	# <Import Project="$(TargetsPath)\Twine.Common.targets" />
	
	# Import Custom Target after the last import.
	$nodeCustomTargetImport = $xml.CreateElement("Import", $xdNS)
	$nodeCustomTargetImport.SetAttribute("Project", "`$(TargetsPath)\Twine.Common.targets")
	$count = $xml.Project.Import.Count - 1
	$output = $xml.Project.InsertAfter($nodeCustomTargetImport, $xml.Project.Import[$count]) | Out-String
	WriteLog $output
	
	return $xml
}


# <summary>
#  Add import of settings
# </summary>
# <param name="$xml" type="XmlDocuement">The project file xml.</param>
# <returns>String</returns>
function DetermineSettingsType()
{
	Param
	(	
		[Parameter(Position=0)] [string] $filePath = $(throw "filePath is a required parameter")
	)
	
	if ($filePath -match "Test")
	{
		return "Test"
	}
	else
	{
		return "Product"
	}
	
	throw "Type not determined"
}



# <summary>
#  Remove unnecessary nodes
# </summary>
# <param name="$xml" type="XmlDocument">The project file xml.</param>
# <returns>String</returns>
function RemoveUnnecessaryNodes
{
	Param
	(	
		[Parameter(Position=0)] [xml] [ref] $xml = $(throw "xml is a required parameter"),
		[Parameter(Position=1)] [System.Xml.XmlNamespaceManager] $nsmgr = $(throw "$nsmgr is a required parameter")
	)
	
	WriteLog "Removing unneeded nodes"

				
	# Remove unnecessary nodes.
	$nodeUpgradeBackupLocation = $xml.Project.PropertyGroup[0].SelectSingleNode("//MSBuild:UpgradeBackupLocation", $nsmgr)
	if ($nodeUpgradeBackupLocation -ne $null -and $nodeUpgradeBackupLocation.'#text' -ne "")
	{
		$output = $nodeUpgradeBackupLocation.ParentNode.RemoveChild($nodeUpgradeBackupLocation) | Out-String
		WriteLog $output
	}

	$nodeFileUpgradeFlags = $xml.SelectSingleNode("//MSBuild:UpgradeBackupLocation", $nsmgr)
	if ($nodeFileUpgradeFlags -ne $null)
	{
		$output = $nodeFileUpgradeFlags.ParentNode.RemoveChild($nodeFileUpgradeFlags) | Out-String
		WriteLog $output
	}
	
	
	RemoveLocalProjectSettings $xml

}


# <summary>
#  Validate MSBuild project.
# </summary>
# <param name="$filePath" type="String">The MSBUILD project file path.</param>
# <returns>String</returns>
function ValidateProjectFile
{
	Param
	(	
		[Parameter(Position=0)] [string] $filePath = $(throw "filePath is a required parameter")
	)
	
	WriteLog "Validating file:  " + $filePath
	
	$ext = [System.IO.Path]::GetExtension($fileName)
	
	$type = DetermineSettingsType($filePath)

	
	$xml = [xml](gc $filePath)
	$xml = ValidateProjectFile $xml $ext $type
	
	return ""
}


# <summary>
#  Validate MSBuild project.
# </summary>
# <param name="$xml" type="XmlDocument">The project file xml.</param>
# <returns>String</returns>
function ValidateProjectFile
{
	Param
	(	
		[Parameter(Position=0)] [xml] $xml = $(throw "xml is a required parameter"),
		[Parameter(Position=1)] [System.Xml.XmlNamespaceManager] $nsmgr = $(throw "$nsmgr is a required parameter"),
		[Parameter(Position=2)] [string] $ext = $(throw "ext is a required parameter"),
		[Parameter(Position=3)] [string] $type = $(throw "type is a required parameter")
	)
	
	WriteLog "ValidateProjectFile:  START"
	
	$nodeImportEnvironmentProps = $xml.SelectSingleNode("//MSBuild:Import[contains(@Project,'BranchRoot)\Build\MsBuild\Props\Environment.props')]", $nsmgr)
	if ($nodeImportEnvironmentProps -eq $null -and $nodeImportEnvironmentProps.'#text' -eq "")
	{
		return "Import of Environment.Props is missing."
	}
	
	
	# Configuration/Platform
	$nodeConfiguration = $xml.SelectSingleNode("//MSBuild:Configuration", $nsmgr)
	if ($nodeConfiguration -eq $null)
	{
		return "Configuration node could not be found."
	}
	elseif ($nodeConfiguration.InnerText -ne "Debug")
	{
		return "Configuration should not be set to Debug"
	}

	$nodePlatform = $xml.SelectSingleNode("//MSBuild:Platform", $nsmgr)
	if ($nodePlatform -eq $null)
	{
		return "Platform node could not be found."
	}
	elseif ($nodePlatform.InnerText -ne "x86")
	{
		return "Platform should not be set to x86"
	}
	
	
	if ($ext -match "vbproj")
	{
		$nodeUseGenericAssemblyInfo = $xml.SelectSingleNode("//MSBuild:UseGenericAssemblyInfoVB", $nsmgr)
		if ($nodeUseGenericAssemblyInfo -eq $null)
		{
			return "UseGenericAssemblyInfoVB node could not be found."
		}
		elseif ($nodeUseGenericAssemblyInfo.InnerText -ne "true")
		{
			return "UseGenericAssemblyInfoVB is not set to 'true'."
		}
	}
	elseif ($ext -match "csproj")
	{
		$nodeUseGenericAssemblyInfo = $xml.SelectSingleNode("//MSBuild:UseGenericAssemblyInfoCS", $nsmgr)
		if ($nodeUseGenericAssemblyInfo -eq $null)
		{
			return "UseGenericAssemblyInfoCS node could not be found."
		}
		elseif ($nodeUseGenericAssemblyInfo.InnerText -ne "true")
		{
			return "UseGenericAssemblyInfoCS is not set to 'true'."
		}
	}

	
#	$nodeUpgradeBackupLocation = $xml.Project.PropertyGroup[0].SelectSingleNode("//MSBuild:UpgradeBackupLocation", $nsmgr)
#	if ($nodeUpgradeBackupLocation -eq $null -and $nodeUpgradeBackupLocation.'#text' -eq "")
#	{
#		return "UpgradeBackupLocation node exists."
#	}
#
#	$nodeFileUpgradeFlags = $xml.SelectSingleNode("//MSBuild:FileUpgradeFlags", $nsmgr)
#	if ($nodeFileUpgradeFlags -eq $null -and $nodeFileUpgradeFlags.'#text' -eq "")
#	{
#		return "FileUpgradeFlags node exists."
#	}



	if ($ext -match "wixproj")
	{
		$nodeSettingsImport = $xml.SelectSingleNode("//MSBuild:Import[contains(@Project,'SettingsPath)\Twine.Wix.Settings')]", $nsmgr)
		if ($nodeGloblaSettings -eq $null)
		{
			return "Import of Twine.Wix.Settings is missing."
		}
	}
	else
	{
		if ($type -match "Test")
		{
			$nodeSettingsImport = $xml.SelectSingleNode("//MSBuild:Import[contains(@Project,'SettingsPath)\Twine.Test.Settings')]", $nsmgr)
			if ($nodeSettingsImport -eq $null)
			{
				return "Import of Twine.Test.Settings is missing."
			}
		}
		else
		{
			$nodeSettingsImport = $xml.SelectSingleNode("//MSBuild:Import[contains(@Project,'SettingsPath)\Twine.Core.Build.Settings')]", $nsmgr)
			if ($nodeSettingsImport -eq $null)
			{
				return "Import of Twine.Core.Build.Settings is missing."
			}
		}
	}
	
	
	if ($ext -match "vbproj")
	{
		$nodeAssemblyInfo = $xml.SelectSingleNode("//MSBuild:Compile[contains(@Include,'My Project\AssemblyInfo.vb')]", $nsmgr)
		if ($nodeAssemblyInfo -eq $null)
		{
			return "Include of AssemblyInfo.vb is missing"
		}
	}
	elseif($ext -match "csproj")
	{
		$nodeAssemblyInfo = $xml.SelectSingleNode("//MSBuild:Compile[contains(@Include,'Properties\AssemblyInfo.cs')]", $nsmgr)
		if ($nodeAssemblyInfo -eq $null)
		{
			return "Include of AssemblyInfo.cs is missing"
		}
	}
	

	
	
	if ($ext -match "vbproj")
	{
		$nodeCommonAssemblyInfo = $xml.SelectSingleNode("//MSBuild:Compile[contains(@Include,'CommonAssemblyInfoPath)\CommonAssemblyInfo.vb')]", $nsmgr)
		if ($nodeCommonAssemblyInfo -eq $null)
		{
			return "CommonAssemblyInfo is missing."
		}
	}
	elseif($ext -match "csproj")
	{
		$nodeCommonAssemblyInfo = $xml.SelectSingleNode("//MSBuild:Compile[contains(@Include,'CommonAssemblyInfoPath)\CommonAssemblyInfo.cs')]", $nsmgr)
		if ($nodeCommonAssemblyInfo -eq $null)
		{
			return "CommonAssemblyInfo is missing."
		}
	}
	
	
	$nodeCommonTargetsImport = $xml.SelectSingleNode("//MSBuild:Import[contains(@Project,'TargetsPath)\Twine.Common.targets')]", $nsmgr)
	if ($nodeCommonTargetsImport -eq $null)
	{
		return "Import of Twine.Common.targets is missing."
	}
	
	return ""
}


# <summary>
#  Update specified project reference.
# </summary>
function UpdateReference
{
	Param
	(	
		[Parameter(Position=0)] [xml] $xml,
		[Parameter(Position=1)] $xdNS,
		[Parameter(Position=2)] $nsmgr,
		[Parameter(Position=3)] [string] $referenceInclude,
		[Parameter(Position=4)] [string] $newReferenceInclude,
		[Parameter(Position=5)] [string] $newReferencePath,
		[Parameter(Position=6)] [bool] $specificVersion,
		[Parameter(Position=7)] [string] $hintPath,
		[Parameter(Position=8)] [string] $referencePath
	)
	
	
	foreach ($ref in $global:ProjectReferences)
	{
		$referenceName = $ref.Split(",")[0].Replace("v11.2","v11.1").Replace("11.2.20112.1010", "11.1.20111.2009")
		$referenceDll = $ref.Split(",")[0] + ".dll"
		
		$nodeReference = $xml.SelectSingleNode("//MSBuild:Reference[contains(@Include,'$($referenceName)')]", $nsmgr)
		if ($nodeReference)
		{
			$nodeReference.Include = $ref
			#$nodeNewReference = $xml.CreateElement("Reference", $xdNS)
			#$nodeNewReference.SetAttribute("Include", "$ref")
			$nodeSpecificVersion = $xml.CreateElement("SpecificVersion", $xdNS)
			$nodeSpecificVersion.InnerText = $specificVersion.ToString()
			$nodeHintPath = $xml.CreateElement("HintPath", $xdNS)
			$nodeHintPath.InnerText = "$($referencePath)\$referenceDll"
			
			$nodeReference.AppendChild($nodeSpecificVersion)
			$nodeReference.AppendChild($nodeHintPath)
			
			WriteLog "Replacing '$($referenceName)' with '$($ref)'"
		}
	}

}


function ReferencesToReplace()
{
	[string[]] $global:ProjectReferences = @(
		"Infragistics2.Documents.Core.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Documents.Excel.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Documents.IO.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Documents.Reports.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Documents.Word.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Math.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Shared.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.SyntaxParsing.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.AppStylistSupport.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.Misc.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.SupportDialogs.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraChart.v11.2.Design, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraGauge.v11.2.Design, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinCalcManager.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinCalcManager.v11.2.FormulaBuilder, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinChart.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinDataSource.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinDock.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinEditors.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinExplorerBar.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinFormattedText.WordWriter.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinGanttView.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinGauge.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinGrid.DocumentExport.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinGrid.ExcelExport.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinGrid.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinGrid.WordWriter.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinInkProvider.Ink17.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinInkProvider.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinListBar.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinListView.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinMaskedEdit.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinPrintPreviewDialog.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinSchedule.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinSpellChecker.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinStatusBar.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinTabbedMdi.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinTabControl.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinToolbars.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.UltraWinTree.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.v11.2.Design, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics2.Win.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Documents.Core.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Documents.Excel.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Documents.Excel.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Documents.IO.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Documents.IO.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Documents.Word.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Documents.Word.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Win.UltraWinFormattedText.WordWriter.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Win.UltraWinFormattedText.WordWriter.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Win.UltraWinGrid.ExcelExport.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Win.UltraWinGrid.ExcelExport.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Win.UltraWinGrid.WordWriter.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics3.Win.UltraWinGrid.WordWriter.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Documents.Core.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Documents.Excel.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Documents.IO.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Documents.Reports.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Documents.Word.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Math.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Shared.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.SyntaxParsing.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.AppStylistSupport.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.Misc.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.SupportDialogs.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinCalcManager.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinCalcManager.v11.2.FormulaBuilder, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinChart.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinDataSource.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinDock.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinEditors.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinExplorerBar.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinFormattedText.WordWriter.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinGanttView.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinGauge.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinGrid.DocumentExport.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinGrid.ExcelExport.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinGrid.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinGrid.WordWriter.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinInkProvider.Ink17.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinListBar.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinListView.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinMaskedEdit.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinPrintPreviewDialog.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinSchedule.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinSpellChecker.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinStatusBar.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinTabbedMdi.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinTabControl.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinToolbars.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.UltraWinTree.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
		"Infragistics4.Win.v11.2, Version=11.2.20112.1010, Culture=neutral, PublicKeyToken=7dd5c3163f2cd0cb, processorArchitecture=MSIL"
	)
}


# <summary>
#  Get the current directory the script is executing from.
# </summary>
function Get-ScriptDirectory
{
	$Invocation = (Get-Variable MyInvocation -Scope 1).Value;
	$commandPath = Get-ChildItem $Invocation.ScriptName
	return $commandPath.DirectoryName
}


function Main
{
  #SearchAndReplace
  ReferencesToReplace
  [string] $script:MainScriptDir = Get-ScriptDirectory
  Import-Module "$MainScriptDir\Logger.psm1" #-verbose
  InitializeLog "Diagnostic" $true "$MainScriptDir\UpdateProjects.log"
  AlterProjects "$env:BranchRoot\Main" 
  AlterProjects "$env:BranchRoot\Main" "*.csproj"
  
  # Alter licx files.
  AlterLicx "$env:BranchRoot" "*.licx"

}

cls
Main 

