; ============================================================
;  Logbook — Inno Setup script
;  Версия: 1.1.12
; ============================================================

#define MyAppName "Logbook"
#define MyAppVersion "1.1.16"
#define MyAppPublisher "Skeorz_Lab"
#define MyAppExeName "Logbook.exe"

; Папка публикации (относительно .iss)
#define PublishDir "bin\Release\LogBook"

; !!! ВАЖНО !!!
; AppId менять ТОЛЬКО при первом релизе. Никогда не менять между версиями,
; иначе апдейт встанет как второе приложение рядом.
; Сгенерировать свой: Tools -> Generate GUID... (Ctrl+Shift+G)
#define MyAppId "{{66A5F7A5-0A63-46D0-BD20-163A7DEDCDA1}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion=1.1.16.0
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=Logbook_Setup_{#MyAppVersion}
SetupIconFile=Resources\Icons\logbook.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Удаляем только служебные папки приложения, не трогая пользовательские данные
; (%AppData%\LogbookApp и Documents\Logbook остаются — там настройки и данные)
Type: filesandordirs; Name: "{app}\Logs"
Type: filesandordirs; Name: "{app}\Cache"
