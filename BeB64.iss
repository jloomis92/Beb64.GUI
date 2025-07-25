#define MyAppName        "BeB64"
#define MyAppVersion     "1.0.0"
#define MySourceDir      "bin\Release\net8.0-windows\win-x64\publish"  ; <- adjust to your TF/rid

; Optional: change company name, URL, etc.
#define MyPublisher      "Jack Loomis"
#define MyAppId          "{{8D0F9B09-2B4D-4C31-A2E3-9E6B9A3C1234}}"   ; any new GUID

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableDirPage=no
DisableProgramGroupPage=no
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}
OutputDir=Output
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
PrivilegesRequired=admin

; (Optional) If you want to show your app icon in Add/Remove Programs,
; set it later via [Registry] using {code:GetAppExe} (Setup section can't call {code}).

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu
Name: "{group}\{#MyAppName}"; Filename: "{code:GetAppExe}";
; Desktop (optional task)
Name: "{commondesktop}\{#MyAppName}"; Filename: "{code:GetAppExe}"; Tasks: desktopicon

[Run]
Filename: "{code:GetAppExe}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  CachedExe: string;

function FindFirstExe(const Dir: string): string;
var
  FindRec: TFindRec;
begin
  Result := '';
  if FindFirst(Dir + '\*.exe', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) = 0 then
        begin
          Result := Dir + '\' + FindRec.Name;
          break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function GetAppExe(Param: string): string;
begin
  if CachedExe = '' then
  begin
    CachedExe := FindFirstExe(ExpandConstant('{app}'));
    if CachedExe = '' then
    begin
      CachedExe := ExpandConstant('{app}\BeB64GUI.exe');
    end;
  end;
  Result := CachedExe;
end;
