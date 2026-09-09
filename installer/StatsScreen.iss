#define AppVersion "0.3.1"
[Setup]
AppId={{45260BCA-87BC-42A4-A19B-A92EA5145D49}
AppName=Stats Screen
AppVersion={#AppVersion}
DefaultDirName={autopf}\Stats Screen
DefaultGroupName=Stats Screen
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\artifacts\installer
OutputBaseFilename=StatsScreen-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\StatsScreen.exe
CloseApplications=yes
SetupLogging=yes
InfoBeforeFile=setup-info.txt

[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:";
Name: startup; Description: "Start with Windows for this account (at sign-in)"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "configure-startup.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "vendor\PawnIO_setup.exe"; Flags: dontcopy

[Icons]
Name: "{autoprograms}\Stats Screen"; Filename: "{app}\StatsScreen.exe"
Name: "{autodesktop}\Stats Screen"; Filename: "{app}\StatsScreen.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\StatsScreen.exe"; Description: "Open Stats Screen"; Flags: postinstall nowait skipifsilent runascurrentuser

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File ""{app}\configure-startup.ps1"" -Mode Disable -Executable ""{app}\StatsScreen.exe"""; Flags: runhidden waituntilterminated; RunOnceId: RemoveStartup

[Code]
function NeedsPawnIO: Boolean;
var
  Version: String;
  InstalledVersion, MinimumVersion: Int64;
begin
  Result := True;
  if RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO', 'DisplayVersion', Version) then
    if StrToVersion(Version, InstalledVersion) and StrToVersion('2.2.0', MinimumVersion) then
      Result := ComparePackedVersion(InstalledVersion, MinimumVersion) < 0;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ExitCode: Integer;
begin
  Result := '';
  if NeedsPawnIO then begin
    ExtractTemporaryFile('PawnIO_setup.exe');
    if not Exec(ExpandConstant('{tmp}\PawnIO_setup.exe'), '-install -silent', '', SW_HIDE, ewWaitUntilTerminated, ExitCode) then
      Result := 'Unable to start PawnIO setup. Stats Screen has not been installed.'
    else if ExitCode = 3010 then NeedsRestart := True
    else if ExitCode <> 0 then Result := 'PawnIO setup failed with code ' + IntToStr(ExitCode) + '. Restart Windows and retry.';
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ExitCode: Integer;
  Mode: String;
begin
  if CurStep = ssPostInstall then begin
    Mode := 'Disable';
    if WizardIsTaskSelected('startup') then Mode := 'Enable';
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\configure-startup.ps1') +
      '" -Mode ' + Mode + ' -Executable "' + ExpandConstant('{app}\StatsScreen.exe') + '"', '', SW_HIDE, ewWaitUntilTerminated, ExitCode) then
      RaiseException('Could not configure Windows startup. Run setup again to retry.');
    if ExitCode <> 0 then RaiseException('Windows startup configuration failed. Run setup again to retry.');
  end;
end;
