; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "VedAstro"
#define MyAppVersion "1.9"
#define MyAppPublisher "VedAstro"
#define MyAppURL "https://vedastro.org/"
#define MyAppExeName "Desktop.exe"
; IMPORTANT : SET version number below
#define NetInstaller "windowsdesktop-runtime-8.0.1-win-x64.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{36D1EEDA-139C-4001-AB14-E15B9ACE4AD9}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
; Remove the following line to run in administrative install mode (install for all users.)
PrivilegesRequired=lowest
OutputDir=C:\Users\ASUS\Desktop\Projects\VedAstro\Desktop\InnoSetupOutput
OutputBaseFilename=VedAstroSetup
SetupIconFile=C:\Users\ASUS\Desktop\Projects\VedAstro\Website\wwwroot\images\favicon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\ASUS\Desktop\Projects\VedAstro\Desktop\bin\Release\net8.0-windows10.0.19041.0\win10-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "C:\Users\ASUS\Desktop\Projects\VedAstro\Desktop\bin\Release\net8.0-windows10.0.19041.0\win10-x64\*"; DestDir: "{app}"; Flags: onlyifdoesntexist recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "{#SourcePath}\dotnet\{#NetInstaller}"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "{#SourcePath}\dotnet\netcorecheck.exe"; DestDir: {tmp}; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; IMPORTANT : SET version number below
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
Filename: "{tmp}\{#NetInstaller}"; Parameters: /install /quiet /norestart; Check: NetCoreNeedsInstall('8.0.1');  StatusMsg: Installing .NET needed for calculations...


[Code]

//////////////////////////////////////////////////////////////////////
// netcorecheck.exe added extra for detecting .net version
function NetCoreNeedsInstall(version: String): Boolean;
var
	netcoreRuntime: String;
	resultCode: Integer;
begin
  // Example: 'Microsoft.NETCore.App', 'Microsoft.AspNetCore.App', 'Microsoft.WindowsDesktop.App'
  netcoreRuntime := 'Microsoft.WindowsDesktop.App'
	Result := not(Exec(ExpandConstant('{tmp}{\}') + 'netcorecheck.exe', netcoreRuntime + ' ' + version, '', SW_HIDE, ewWaitUntilTerminated, resultCode) and (resultCode = 0));
end;