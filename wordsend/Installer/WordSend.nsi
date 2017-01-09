; wordsend.nsi
;
; © 2006-2009 EBT and SIL. Released under the Gnu LGPL 3 or later.
; See License.rtf.
;
; This NSIS script installs the WordSend program and support files into the
; directory chosen by the user (default is SIL\WordSend under the user's
; program files directory) and adds that path to the search path, if
; not already there. It creates an uninstall program and registers that
; with Windows. It also optionally provides the user with menu and
; desktop shortcuts to start the GUI programs associated with this
; project.

!include "MUI.nsh"
Name "WordSend project"
OutFile "installwordsend.exe"
SetCompressor /SOLID lzma
BrandingText "http://eBible.org/wordsend/ WordSend Scripture typesetting and file format conversions"
!define MUI_ICON ..\wordsend\App.ico
!define MUI_UNICON ..\wordsend\App.ico

; The default installation directory
InstallDir $PROGRAMFILES\SIL\WordSend
; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\SIL\WordSend" "Install_Dir"

; Pages
  !insertmacro MUI_PAGE_LICENSE "..\License.rtf"
  !insertmacro MUI_PAGE_COMPONENTS
  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_INSTFILES
  
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES

  !insertmacro MUI_LANGUAGE "English"

; The components to install
Section "WordSend (required)" Section1
	ClearErrors
	UserInfo::GetName
	IfErrors adminstateknown
	Pop $0
	UserInfo::GetAccountType
	Pop $1
	StrCmp $1 "Admin" 0 +3
		SetShellVarContext all
	adminstateknown:
	ClearErrors

  SectionIn RO
  
  ; Set output path to the installation directory and put files there.
  SetOutPath $INSTDIR
  SetOverwrite on
  File "..\wordsend\*"
  
  ; Write the installation path into the registry
  WriteRegStr HKLM SOFTWARE\SIL\WordSend "Install_Dir" "$INSTDIR"
  Push $INSTDIR
  Call AddToPath

	; Create start menu entries.
  CreateDirectory "$SMPROGRAMS\WordSend"
  CreateShortCut "$SMPROGRAMS\WordSend\Uninstall WordSend.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
  CreateShortCut "$SMPROGRAMS\WordSend\WordSend USFM to WordML.lnk" "$INSTDIR\usfm2word.exe" "" "$INSTDIR\usfm2word.exe" 0
  CreateShortCut "$SMPROGRAMS\WordSend\WordSend Documentation.lnk" "$INSTDIR\index.htm" "" "$INSTDIR\index.htm" 0
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WordSend-SIL" "DisplayName" "WordSend (remove only)"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WordSend-SIL" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WordSend-SIL" "URLInfoAbout" '"http://eBible.org/WordSend/"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WordSend-SIL" "URLUpdateInfo" '"http://eBible.org/WordSend/"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WordSend-SIL" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WordSend-SIL" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
SectionEnd
LangString DESC_Section1 ${LANG_ENGLISH} "Core files required to run WordSend project programs and their documentation"

Section "Desktop Shortcuts" Section2
  SetOverwrite on
  CreateShortCut "$DESKTOP\WordSend USFM to WordML.lnk" "$INSTDIR\usfm2word.exe" "" "$INSTDIR\usfm2word.exe" 0
  CreateShortCut "$DESKTOP\WordSend Documentation.lnk" "$INSTDIR\index.htm" "" "$INSTDIR\index.htm" 0
SectionEnd
LangString DESC_Section3 ${LANG_ENGLISH} "Desktop icons for easy launching of WordSend GUI programs"

Section "Paratext Integration" Section3
  ; Set output path to the installation directory and put files there.
  ClearErrors
  StrCpy $2 "\cms"
  ReadRegStr $0 HKLM SOFTWARE\ScrChecks\1.0\Settings_Directory ""
  IfErrors 0 +3
    StrCpy $0 'C:\My Paratext Projects'
    CreateDirectory $0
  StrCpy $1 '$0$2'
  CreateDirectory $1
  SetOutPath $1
  SetOverwrite on
  File "..\cms\*"
SectionEnd
LangString DESC_Section4 ${LANG_ENGLISH} "Installs checking plugin to export from a Paratext project to Microsoft Word XML"

Section "Fonts (used in default seed files)" Section4
  SetOverwrite ifnewer
  SetOutPath $FONTS
  File "..\fonts\*.ttf"
  SetOutPath $INSTDIR
  File "..\fonts\*.txt"
  File "..\fonts\*.pdf"
  MessageBox MB_YESNO|MB_ICONQUESTION "New fonts will not be available until after rebooting. Do you wish to reboot now?" IDNO +2
    Reboot
SectionEnd
LangString DESC_Section2 ${LANG_ENGLISH} "Gentium, Charis SIL, Charis SIL Literacy, Doulos, Doulos SIL Literacy, and EBT Basic fonts"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${Section1} $(DESC_Section1)
  !insertmacro MUI_DESCRIPTION_TEXT ${Section2} $(DESC_Section2)
  !insertmacro MUI_DESCRIPTION_TEXT ${Section3} $(DESC_Section3)
  !insertmacro MUI_DESCRIPTION_TEXT ${Section4} $(DESC_Section4)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------

; Uninstaller

Section "Uninstall"
  MessageBox MB_YESNO "This will delete ALL files in $INSTDIR, even if you have modified them or added to them.$\r$\nContinue?" IDYES deleteit
    Abort "Uninstall aborted. No files deleted."
  deleteit:

	ClearErrors
	UserInfo::GetName
	IfErrors adminknown
	Pop $0
	UserInfo::GetAccountType
	Pop $1
	StrCmp $1 "Admin" 0 +3
		SetShellVarContext all
	adminknown:
	ClearErrors

  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WordSend"
  DeleteRegKey HKLM SOFTWARE\SIL\WordSend
  Push $INSTDIR
  Call un.RemoveFromPath

  ; Remove files and uninstaller
  Delete $INSTDIR\uninstall.exe
  Delete $INSTDIR\*

  ; Remove shortcuts, if any
  Delete "$SMPROGRAMS\WordSend\*.*"
  Delete "$DESKTOP\WordSend USFM to WordML.lnk"
  Delete "$DESKTOP\WordSend Documentation.lnk"

  ; Remove directories used
  RMDir "$SMPROGRAMS\WordSend"
  RMDir "$INSTDIR"

  SetShellVarContext current
  Delete $APPDATA\SIL\WordSend\*
  RMDir "$APPDATA\SIL\WordSend"
SectionEnd

;----------------------------------------
; based upon a script of "Written by KiCHiK 2003-01-18 05:57:02"
;----------------------------------------
!verbose 3
!include "WinMessages.NSH"
!verbose 4
;====================================================
; get_NT_environment 
;     Returns: the selected environment
;     Output : head of the stack
;====================================================
;----------------------------------------------------
!define NT_current_env 'HKCU "Environment"'
!define NT_all_env     'HKLM "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"'
;====================================================
; IsNT - Returns 1 if the current system is NT, 0
;        otherwise.
;     Output: head of the stack
;====================================================
!macro IsNT UN
Function ${UN}IsNT
  Push $0
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" CurrentVersion
  StrCmp $0 "" 0 IsNT_yes
  ; we are not NT.
  Pop $0
  Push 0
  Return
 
  IsNT_yes:
    ; NT!!!
    Pop $0
    Push 1
FunctionEnd
!macroend
!insertmacro IsNT ""
!insertmacro IsNT "un."
;====================================================
; AddToPath - Adds the given dir to the search path.
;        Input - head of the stack
;        Note - Win9x systems requires reboot
;====================================================
Function AddToPath
   Exch $0
   Push $1
   Push $2
  
   Call IsNT
   Pop $1
   StrCmp $1 1 AddToPath_NT
      ; Not on NT
      StrCpy $1 $WINDIR 2
      FileOpen $1 "$1\autoexec.bat" a
      FileSeek $1 0 END
      GetFullPathName /SHORT $0 $0
      FileWrite $1 "$\r$\nSET PATH=%PATH%;$0$\r$\n"
      FileClose $1
      Goto AddToPath_done
 
   AddToPath_NT:
      StrCpy $4 "all"
      AddToPath_NT_selection_done:
      StrCmp $4 "current" read_path_NT_current
         ReadRegStr $1 ${NT_all_env} "PATH"
         Goto read_path_NT_resume
      read_path_NT_current:
         ReadRegStr $1 ${NT_current_env} "PATH"
      read_path_NT_resume:

      ; Do the actual adding of the directory to the path. 
      StrCmp $1 "" AddToPath_NTdoIt
         StrCpy $2 "$1;$0"	; New path is old path + ; + our directory
         Goto AddToPath_NTdoIt
      StrCpy $2 "$0"	; Old path was empty, so it is now just our directory
      AddToPath_NTdoIt:
			; Full search path with our directory is now in $2.
			
      ; Check to see if our directory is already in the path.
      ; MessageBox MB_OK|MB_ICONINFORMATION "Before StrStr 2: $2 1: $1; 0: $0"
      Push $1	; String to search with StrStr (Current path)
      Push $0	; String to search for with StrStr (Directory to add)
      Call StrStr ; Find $0 in $1
      Pop $0 ; pos of our dir
      ; MessageBox MB_OK|MB_ICONINFORMATION "After StrStr 2: $2 1: $1; 0: $0"
      IntCmp $0 -1 NeedToWrite write_path_NT_failed write_path_NT_failed
      NeedToWrite:
         ; Our directory isn't in the path, so continue to add it.
         StrCmp $4 "current" write_path_NT_current
            ClearErrors
            WriteRegExpandStr ${NT_all_env} "PATH" $2
            IfErrors 0 write_path_NT_resume
            ; change selection
            StrCpy $4 "current"
            Goto AddToPath_NT_selection_done
         write_path_NT_current:
            ClearErrors
            WriteRegExpandStr ${NT_current_env} "PATH" $2
            IfErrors 0 write_path_NT_resume
            MessageBox MB_OK|MB_ICONINFORMATION "The path could not be set for the current user."
            Goto write_path_NT_failed
         write_path_NT_resume:
         SendMessage ${HWND_BROADCAST} ${WM_WININICHANGE} 0 "STR:Environment" /TIMEOUT=5000
         DetailPrint "added path for user ($4), $0"
         write_path_NT_failed:
      Pop $4
   AddToPath_done:
   Pop $2
   Pop $1
   Pop $0
FunctionEnd
 
;====================================================
; RemoveFromPath - Remove a given dir from the path
;     Input: head of the stack
;====================================================
Function un.RemoveFromPath
   Exch $0
   Push $1
   Push $2
   Push $3
   Push $4
   
   Call un.IsNT
   Pop $1
   StrCmp $1 1 unRemoveFromPath_NT
      ; Not on NT
      StrCpy $1 $WINDIR 2
      FileOpen $1 "$1\autoexec.bat" r
      GetTempFileName $4
      FileOpen $2 $4 w
      GetFullPathName /SHORT $0 $0
      StrCpy $0 "SET PATH=%PATH%;$0"
      SetRebootFlag true
      Goto unRemoveFromPath_dosLoop
     
      unRemoveFromPath_dosLoop:
         FileRead $1 $3
         StrCmp $3 "$0$\r$\n" unRemoveFromPath_dosLoop
         StrCmp $3 "$0$\r" unRemoveFromPath_dosLoop
         StrCmp $3 "$0" unRemoveFromPath_dosLoop
         StrCmp $3 "" unRemoveFromPath_dosLoopEnd
         FileWrite $2 $3
         Goto unRemoveFromPath_dosLoop
 
      unRemoveFromPath_dosLoopEnd:
         FileClose $2
         FileClose $1
         StrCpy $1 $WINDIR 2
         Delete "$1\autoexec.bat"
         CopyFiles /SILENT $4 "$1\autoexec.bat"
         Delete $4
         Goto unRemoveFromPath_done
 
   unRemoveFromPath_NT:
      StrLen $2 $0
      StrCpy $4 "all"
 
      StrCmp $4 "current" un_read_path_NT_current
         ReadRegStr $1 ${NT_all_env} "PATH"
         Goto un_read_path_NT_resume
      un_read_path_NT_current:
         ReadRegStr $1 ${NT_current_env} "PATH"
      un_read_path_NT_resume:
 
      Push $1
      Push $0
      Call un.StrStr ; Find $0 in $1
      Pop $0 ; pos of our dir
      IntCmp $0 -1 unRemoveFromPath_done
         ; else, it is in path
         IntOp $0 $0 - 1 ; position of ; before our dir
         StrCpy $3 $1 $0 ; $3 now has the part of the path before our dir
         IntOp $2 $2 + $0 ; $2 now contains the position after our dir in the path (';')
         IntOp $2 $2 + 1 ; $2 now contains the position after our dir and the semicolon.
         StrLen $0 $1
         StrCpy $1 $1 $0 $2
         StrCpy $3 "$3$1"
 
         StrCmp $4 "current" un_write_path_NT_current
            WriteRegExpandStr ${NT_all_env} "PATH" $3
            Goto un_write_path_NT_resume
         un_write_path_NT_current:
            WriteRegExpandStr ${NT_current_env} "PATH" $3
         un_write_path_NT_resume:
         SendMessage ${HWND_BROADCAST} ${WM_WININICHANGE} 0 "STR:Environment" /TIMEOUT=5000
   unRemoveFromPath_done:
   Pop $4
   Pop $3
   Pop $2
   Pop $1
   Pop $0
FunctionEnd
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Uninstall sutff
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
 
 
;====================================================
; StrStr - Finds a given string in another given string.
;               Returns -1 if not found and the pos if found.
;          Input: head of the stack - string to find
;                      second in the stack - string to find in
;          Output: head of the stack
;====================================================
Function un.StrStr
  Push $0
  Exch
  Pop $0 ; $0 now have the string to find
  Push $1
  Exch 2
  Pop $1 ; $1 now have the string to find in
  Exch
  Push $2
  Push $3
  Push $4
  Push $5
 
  StrCpy $2 -1
  StrLen $3 $0
  StrLen $4 $1
  IntOp $4 $4 - $3
 
  unStrStr_loop:
    IntOp $2 $2 + 1
    IntCmp $2 $4 0 0 unStrStrReturn_notFound
    StrCpy $5 $1 $3 $2
    StrCmp $5 $0 unStrStr_done unStrStr_loop
 
  unStrStrReturn_notFound:
    StrCpy $2 -1
 
  unStrStr_done:
    Pop $5
    Pop $4
    Pop $3
    Exch $2
    Exch 2
    Pop $0
    Pop $1
FunctionEnd

Function StrStr
  Push $0
  Exch
  Pop $0 ; $0 now have the string to find
  Push $1
  Exch 2
  Pop $1 ; $1 now have the string to find in
  Exch
  Push $2
  Push $3
  Push $4
  Push $5
 
  StrCpy $2 -1
  StrLen $3 $0
  StrLen $4 $1
  IntOp $4 $4 - $3
 
  unStrStr_loop:
    IntOp $2 $2 + 1
    IntCmp $2 $4 0 0 unStrStrReturn_notFound
    StrCpy $5 $1 $3 $2
    StrCmp $5 $0 unStrStr_done unStrStr_loop
 
  unStrStrReturn_notFound:
    StrCpy $2 -1
 
  unStrStr_done:
    Pop $5
    Pop $4
    Pop $3
    Exch $2
    Exch 2
    Pop $0
    Pop $1
FunctionEnd
;==================================================== 
 