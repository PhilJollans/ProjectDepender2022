' VB Script Document
option explicit

Const ForReading = 1
Const BaseOutputDirectory = "C:\ProjectDependerBuilds"

'-------------------------------------------------------------------------------
'Global variables
'-------------------------------------------------------------------------------

'Utility objects
Dim fso
Dim TextStream
Dim FileText
Dim re
Dim WshShell

'Values to patch into the files 
Dim Build_Version
Dim Assembly_Version
Dim Vsix_Version
Dim RootDir

Dim OutputDirectory
Dim ExitCode

'-------------------------------------------------------------------------------
'Function: PatchAssemblyFileVersion
'-------------------------------------------------------------------------------
Private Sub PatchAssemblyFileVersion ( ConfigFile, AssemblyVersion )

  Set TextStream  = fso.OpenTextFile ( ConfigFile, ForReading )
  FileText = TextStream.ReadAll
  TextStream.Close
  Set TextStream = Nothing
         
  re.Pattern = "AssemblyFileVersion\s*\(\s*""[^""]*""\s*\)"
  FileText = re.Replace ( FileText, "AssemblyFileVersion(""" & AssemblyVersion & """)" )

  re.Pattern = "AssemblyVersion\s*\(\s*""[^""]*""\s*\)"
  FileText = re.Replace ( FileText, "AssemblyVersion(""" & AssemblyVersion & """)" )

  set TextStream = fso.CreateTextFile ( ConfigFile, True, False )
  TextStream.Write FileText
  TextStream.Close
  Set TextStream = Nothing

End Sub

'-------------------------------------------------------------------------------
'Function: PatchVsixManifest
'-------------------------------------------------------------------------------
Private Sub PatchVsixManifest ( ManifestFile, AssemblyVersion )
                            
  Set TextStream  = fso.OpenTextFile ( ManifestFile, ForReading )
  FileText = TextStream.ReadAll
  TextStream.Close
  Set TextStream = Nothing
         
  're.Pattern = "<Version>.*</Version>" 
  'FileText = re.Replace ( FileText, "<Version>" & AssemblyVersion & "</Version>" )

  'This assumes the exact format <Identity Version="6.1" ...
  '(i.e. Version is the first attribute 
  re.Pattern = "Identity Version=""[^""]*""" 
  FileText = re.Replace ( FileText, "Identity Version=""" & AssemblyVersion & """" )

  set TextStream = fso.CreateTextFile ( ManifestFile, True, False )
  TextStream.Write FileText
  TextStream.Close
  Set TextStream = Nothing

End Sub

'-------------------------------------------------------------------------------
'Function: PatchPackageDefinition
'-------------------------------------------------------------------------------
Private Sub PatchPackageDefinition ( PackageSourceFile, AssemblyVersion )

  Set TextStream  = fso.OpenTextFile ( PackageSourceFile, ForReading )
  FileText = TextStream.ReadAll
  TextStream.Close
  Set TextStream = Nothing
         
  Dim matches
  Dim SubMatch
  Dim FullMatch
  Dim NewText

  re.Pattern = "InstalledProductRegistration(\s*\(\s*""[^""]*""\s*,\s*""[^""]*""\s*,\s*"")[^""]*" 
  set matches = re.execute ( FileText )
  
  'msgbox matches.Count
  'msgbox matches(0)
  'msgbox matches(0).Submatches(0)
  
  FullMatch = matches(0)
  SubMatch  = matches(0).Submatches(0)  
  NewText   = "InstalledProductRegistration" & SubMatch & AssemblyVersion 

  FileText = Replace ( FileText, FullMatch, NewText )

  set TextStream = fso.CreateTextFile ( PackageSourceFile, True, False )
  TextStream.Write FileText
  TextStream.Close
  Set TextStream = Nothing

End Sub

'-------------------------------------------------------------------------------
'Function: BuildSolution
'-------------------------------------------------------------------------------
Private Sub BuildSolution ( SolutionFile )

  Dim MSBuildCommand
  
  MSBuildCommand = """C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe"" " & SolutionFile & " /t:Build /p:Configuration=Release /p:Platform=""Any CPU"" /p:TargetFramework=v4.7.2 /fl /flp:logfile=ProjectDependerBuild.log /nodeReuse:false"
  
  ExitCode = WshShell.Run ( MSBuildCommand, 1, True )
  If ExitCode <> 0 Then
    MsgBox "Build error in " & SolutionFile
    WScript.Quit
  End if  
  
End Sub

'===============================================================================
'MAIN CODE
'===============================================================================

'-------------------------------------------------------------------------------
'Get some objects
'-------------------------------------------------------------------------------
set re = New RegExp
Set fso = CreateObject("Scripting.FileSystemObject")
Set WshShell = WScript.CreateObject("WScript.Shell")

'-------------------------------------------------------------------------------
'Get some values for patching
'-------------------------------------------------------------------------------

Build_Version     = InputBox ( "Enter the build version" )
Build_Version     = Right ( "0000" & Build_Version, 4 )
Assembly_Version  = "1.03.0." & Build_Version
Vsix_Version      = "1.03." & Build_Version      

RootDir = fso.GetParentFolderName(WScript.ScriptFullName)
'MsgBox RootDir

'-------------------------------------------------------------------------------
'Create the output directory
'-------------------------------------------------------------------------------
OutputDirectory = BaseOutputDirectory & "\1_03_" & Build_Version & "\"
if Not fso.FolderExists ( OutputDirectory ) Then
  fso.CreateFolder OutputDirectory 
End If

'Patch the assembly config files
PatchAssemblyFileVersion "Properties\AssemblyInfo.cs", Assembly_Version

'Patch the version in the vsixmanifest file
PatchVsixManifest "source.extension.vsixmanifest", Vsix_Version 
                                                                                                              
'Patch the version in the package defintion file for the visual studio about dialog
PatchPackageDefinition "ProjectDependenciesPackage.cs", Assembly_Version

'Compile the projects
BuildSolution "ProjectDepender.sln" 

'Copy output file
fso.CopyFile "bin\Release\ProjectDepender2022.vsix", OutputDirectory 

MsgBox "Done"
 