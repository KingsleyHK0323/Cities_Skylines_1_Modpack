; Signature: OpenAI-Codex
#ifndef PackageDir
  #error PackageDir is required
#endif
#ifndef OutputDir
  #error OutputDir is required
#endif
#ifndef AppVersion
  #error AppVersion is required
#endif

[Setup]
AppId=Cities_Skylines_1_Modpack_Dev_Manager-{#AppVersion}
AppName=Cities Skylines 1 Modpack Dev Manager
AppVersion={#AppVersion}
AppPublisher=Kingsley
DefaultDirName={localappdata}\Programs\Cities Skylines 1 Modpack Dev Manager\{#AppVersion}
DefaultGroupName=Cities Skylines 1 Modpack Dev Manager {#AppVersion}
UsePreviousAppDir=no
UsePreviousGroup=no
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
CloseApplications=no
LicenseFile={#PackageDir}\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=Cities_Skylines_1_Modpack_Dev_Manager_Setup
UninstallDisplayIcon={app}\DevManager.exe

[Files]
Source: "{#PackageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Dev Manager"; Filename: "{app}\DevManager.exe"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Entry: TFindRec;
  HasContent: Boolean;
begin
  Result := '';
  HasContent := False;
  if DirExists(ExpandConstant('{app}')) then begin
    if not FindFirst(AddBackslash(ExpandConstant('{app}')) + '*', Entry) then begin
      Result := 'The destination folder could not be checked. Select another empty folder.';
      exit;
    end;
    try
      repeat
        if (Entry.Name <> '.') and (Entry.Name <> '..') then
          HasContent := True;
      until HasContent or not FindNext(Entry);
    finally
      FindClose(Entry);
    end;
    if HasContent then
      Result := 'The destination folder is not empty. Select another empty folder.';
  end;
end;
