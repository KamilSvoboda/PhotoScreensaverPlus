;TODO: 
;pøidat do screensaveru druhou ikonu pro web site link
;zjistit proè nefunguje spouštìní konfigurace spoøièe z vytvoøeného linku

;--------------------------------
;Includes
	!include "MUI2.nsh"
	!include "Library.nsh"
	
;--------------------------------
; The name of the installer
Name "Photo Screensaver Plus"
Caption $(instCaption)
Icon "pssp.ico"

UninstallText $(uninstCaption)
UninstallIcon "pssp.ico"

; The file to write
OutFile "PhotoScreensaverPlusSetup.exe"

; The default installation directory
InstallDir '$PROGRAMFILES\Photo Screensaver Plus'

; Request application privileges for Windows Vista - admin is required, because of writing system log source to registry (see below)
RequestExecutionLevel admin

;--------------------------------
;Interface Settings
  !define MUI_ABORTWARNING

  ;--------------------------------
;Reserve Files
  
  ;If you are using solid compression, files that are required before
  ;the actual installation should be stored first in the data block,
  ;because this will make your installer start faster.
  
  !insertmacro MUI_RESERVEFILE_LANGDLL
  
;--------------------------------
;Pages
  !insertmacro MUI_PAGE_COMPONENTS
  !insertmacro MUI_PAGE_INSTFILES	
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES

;--------------------------------
;Languages
  !insertmacro MUI_LANGUAGE "English"
  !insertmacro MUI_LANGUAGE "Czech"

 ;Strings - must be defined after "!insertmacro MUI_LANGUAGE" lines.
 LangString selectLangCaption ${LANG_ENGLISH} "Installer Language"
 LangString selectLangCaption ${LANG_CZECH} "Jazyk instalace"
 LangString selectLangQuestion ${LANG_ENGLISH} "Please select a language."
 LangString selectLangQuestion ${LANG_CZECH} "Vyberte si prosím jazyk:"
 LangString instCaption ${LANG_ENGLISH} "Installation of Photo Screensaver Plus"
 LangString instCaption ${LANG_CZECH} "Instalace spoøièe Photo Screensaver Plus"
 LangString uninstCaption ${LANG_ENGLISH} "This will uninstall Photo Screensaver Plus. Hit Uninstall to continue."
 LangString uninstCaption ${LANG_CZECH} "Odinstalace Photo Screensaver Plus. Kliknìte na Odinstalovat."
 LangString sec1Title ${LANG_ENGLISH} "Photo Screensaver Plus (required)"
 LangString sec1Title ${LANG_CZECH} "Photo Screensaver Plus (vyžadováno)"
 LangString sec2Title ${LANG_ENGLISH} "Start Menu Shortcuts"
 LangString sec2Title ${LANG_CZECH} "Pøidat do nabídky Start"
 LangString sec3Title ${LANG_ENGLISH} "Desktop Shortcut"
 LangString sec3Title ${LANG_CZECH} "Pøidat odkaz na plochu"
 LangString sec4Title ${LANG_ENGLISH} "Add Folder Context Menu Command"
 LangString sec4Title ${LANG_CZECH} "Pøidat pøíkaz do kontextové nabídky adresáøe"
 LangString sec1Desc ${LANG_ENGLISH} "Installs screensaver to windows system directory (typically c:\windows\system32\"
 LangString sec1Desc ${LANG_CZECH} "Nainstaluje spoøiè do systémového adresáøe windows (typicky c:\windows\system32\"
 LangString sec2Desc ${LANG_ENGLISH} "Creates start menu shortcuts to screensaver, web site etc. for all PC users"
 LangString sec2Desc ${LANG_CZECH} "Vytvoøí pro všechny uživatele poèítaèe položku v nabídce Start a pøidá do ní odkazy na spuštìní spoøièe, na jeho domovskou stránku atd."	
 LangString sec3Desc ${LANG_ENGLISH} "Creates desktop shortcut to run screensaver for all PC users"
 LangString sec3Desc ${LANG_CZECH} "Vytvoøí pro všechny uživatele poèítaèe odkaz na ploše pro spuštìní spoøièe"
 LangString sec4Desc ${LANG_ENGLISH} "Adds a context menu command in the directory that starts a slideshow of pictures within the directory"
 LangString sec4Desc ${LANG_CZECH} "Pøidá pøíkaz do kontextové nabídky adresáøe, který spustí prezentaci obrázkù uvnitø adresáøe"
 LangString folderCommand ${LANG_ENGLISH} "Slide show with Photo Screensaver Plus"
 LangString folderCommand ${LANG_CZECH} "Prezentace obrázkù s Photo Screensaver Plus"
 LangString shortcutDesc1 ${LANG_ENGLISH} "Uninstall screensaver"
 LangString shortcutDesc1 ${LANG_CZECH} "Odinstaluje spoøiè"
 LangString shortcutDesc2 ${LANG_ENGLISH} "Run screensaver manually"
 LangString shortcutDesc2 ${LANG_CZECH} "Spustí ruènì spoøiè"
 LangString shortcutDesc3 ${LANG_ENGLISH}  "Configuration of the screensaver"
 LangString shortcutDesc3 ${LANG_CZECH} "Spustí konfiguraci spoøièe" 
 LangString shortcutDesc4 ${LANG_ENGLISH}  "Link to screensaver web site"
 LangString shortcutDesc4 ${LANG_CZECH} "Odkaz na webovou stránku spoøièe" 
 LangString setAsDefaultQuestion ${LANG_ENGLISH} "Do you wish set the screensaver as your default and run its configuration?"
 LangString setAsDefaultQuestion ${LANG_CZECH} "Pøejete si nastavit Photo Screensaver Plus jako aktuální spoøiè a otevøít jeho nastavení?"
	
;--------------------------------
;Initialization of installlation
Function .onInit
	!insertmacro MUI_DEFAULT MUI_LANGDLL_WINDOWTITLE $(selectLangCaption)
	!insertmacro MUI_DEFAULT MUI_LANGDLL_INFO $(selectLangQuestion)
	!insertmacro MUI_LANGDLL_DISPLAY

FunctionEnd

;--------------------------------
; The stuff to install
Section $(sec1Title) "Pssp"

  SectionIn RO
  
  ;Set output path to the system directory (windows\system32)
  SetOutPath $SYSDIR 
  ;Put file there
  File "PhotoScreensaverPlus.scr"
      
  ;Set output path to the installation directory.
  SetOutPath $INSTDIR
  ;Put files there
  File "website.url"  
  File "NLog.dll"
  File "PsspInstallUtils.exe"
  
  ;register DLLs
  nsExec::Exec '"$INSTDIR\PsspInstallUtils.exe" i "$INSTDIR"'
        
  CreateShortCut "$INSTDIR\Photo Screensaver Plus.lnk" "$SYSDIR\PhotoScreensaverPlus.scr" "" "$SYSDIR\PhotoScreensaverPlus.scr" 0
  
  ;delete previous event log source
  DeleteRegKey HKLM "SYSTEM\CurrentControlSet\Services\EventLog\Application\Photo Screensaver Plus"
    
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PhotoScreensaverPlus" "DisplayName" "Photo Screensaver Plus"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PhotoScreensaverPlus" "Publisher" "Kamil Svoboda"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PhotoScreensaverPlus" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PhotoScreensaverPlus" "DisplayIcon" "$SYSDIR\PhotoScreensaverPlus.scr,0"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PhotoScreensaverPlus" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PhotoScreensaverPlus" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  
SectionEnd

; Optional section (can be disabled by the user)
Section $(sec2Title) StartMenuSchortcuts
  
  ;sets the shortcuts are for all users - must be in every section
  SetShellVarContext All
  CreateDirectory "$SMPROGRAMS\Photo Screensaver Plus"
  CreateShortCut "$SMPROGRAMS\Photo Screensaver Plus\Uninstall Photo Screensaver Plus.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0 "" "" $(shortcutDesc1)
  CreateShortCut "$SMPROGRAMS\Photo Screensaver Plus\Photo Screensaver Plus.lnk" "$SYSDIR\PhotoScreensaverPlus.scr" "" "$SYSDIR\PhotoScreensaverPlus.scr" 0 "" "" $(shortcutDesc2)
  ;CreateShortCut "$SMPROGRAMS\Photo Screensaver Plus\Photo Screensaver Plus Configuration.lnk" "$SYSDIR\PhotoScreensaverPlus.scr" "/c" "$SYSDIR\PhotoScreensaverPlus.scr" 0 "" "" $(shortcutDesc3)
  CreateShortCut "$SMPROGRAMS\Photo Screensaver Plus\Photo Screensaver Plus Web Site.lnk" "$INSTDIR\website.url" "" "$PROGRAMFILES\Internet Explorer\iexplore.exe" 0 "" "" $(shortcutDesc4)
  
SectionEnd

; Optional section (can be disabled by the user)
Section $(sec3Title) DescktopShortcut
  ;sets the shortcuts are for all users - must be in every section
  SetShellVarContext All
  CreateShortCut "$DESKTOP\Photo Screensaver Plus.lnk" "$SYSDIR\PhotoScreensaverPlus.scr"
  
SectionEnd

; Optional section (can be disabled by the user)
Section $(sec4Title) FolderContextMenuCommand
  ;adds command to the directory context menu (mouse right click)
  SetShellVarContext All
  WriteRegStr HKCR "Directory\shell\PhotoScreensaverPlus" "" $(folderCommand)
  WriteRegStr HKCR "Directory\shell\PhotoScreensaverPlus\command" "" '"$SYSDIR\PhotoScreensaverPlus.scr" /f "%1"'
SectionEnd

Section -LogSourceInstaller ;zavolá vytvoøení log source v registrech windows
	;write registry entry for system log source - needed for screensaver logging
	;WriteRegExpandStr HKLM "SYSTEM\CurrentControlSet\Services\EventLog\Application\Photo Screensaver Plus" "EventMessageFile" "$SYSDIR\PhotoScreensaverPlus.scr"
  
	nsexec::exectolog '"$SYSDIR\PhotoScreensaverPlus.scr" /l'
SectionEnd

Section -SetAsUserDefault ;když má sekce pøed identifikátorem "-" tak se spustí až po dokonèení všech pøedchozích (nakopírování souborù)
  MessageBox MB_YESNO $(setAsDefaultQuestion) IDYES Configure IDNO End
  Configure: 
	GetFullPathName /SHORT $R9 "$SYSDIR\PhotoScreensaverPlus.scr" 
	;MessageBox MB_OK $R9
	WriteRegStr HKCU "Control Panel\Desktop" "SCRNSAVE.EXE" $R9
	nsexec::exectolog '"$SYSDIR\PhotoScreensaverPlus.scr" /c'
  End:
SectionEnd

;--------------------------------
; Uninstaller
Section "Uninstall"
  ;sets the shortcuts are for all users - must be in every section
  SetShellVarContext All
  
  ;unregister DLLs
  nsexec::Exec '"$INSTDIR\PsspInstallUtils.exe" u "$INSTDIR"'
  
  ; Remove registry keys - settings  are not removed from registry
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PhotoScreensaverPlus"
  
  DeleteRegKey HKCR "Directory\shell\PhotoScreensaverPlus"
  
  ; Remove files and uninstaller
  Delete $SYSDIR\PhotoScreensaverPlus.scr
  Delete "$INSTDIR\Photo Screensaver Plus.lnk"
  Delete $INSTDIR\website.url
  Delete $INSTDIR\NLog.dll
  Delete $INSTDIR\PsspInstallUtils.exe
  Delete $INSTDIR\uninstall.exe

  ; Remove shortcuts, if any
  Delete "$SMPROGRAMS\Photo Screensaver Plus\*.*"
  Delete "$DESKTOP\Photo Screensaver Plus.lnk"
  
  ; Remove directories used
  RMDir "$SMPROGRAMS\Photo Screensaver Plus"
  RMDir "$INSTDIR"

SectionEnd

;--------------------------------
;Descriptions
  ;Assign descriptions to sections
  !insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${Pssp} $(sec1Desc)
    !insertmacro MUI_DESCRIPTION_TEXT ${StartMenuSchortcuts} $(sec2Desc)
	!insertmacro MUI_DESCRIPTION_TEXT ${DescktopShortcut} $(sec3Desc)
	!insertmacro MUI_DESCRIPTION_TEXT ${FolderContextMenuCommand} $(sec4Desc)
  !insertmacro MUI_FUNCTION_DESCRIPTION_END