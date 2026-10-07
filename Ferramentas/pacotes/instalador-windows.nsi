; Instalador Windows da Trilha da Inovação (NSIS 3).
; Gerado por Ferramentas/empacotar-windows.sh — não rode direto.
Unicode true
!include "MUI2.nsh"

!ifndef VERSAO
  !define VERSAO "1.0.0"
!endif
!define NOME "Trilha da Inovação"
!define EMPRESA "Fofucho's Games Group"
!define EXE "TrilhaDaInovacao.exe"
!define CHAVE_DESINSTALAR "Software\Microsoft\Windows\CurrentVersion\Uninstall\TrilhaDaInovacao"

Name "${NOME}"
OutFile "${SAIDA}"
InstallDir "$PROGRAMFILES64\Trilha da Inovacao"
InstallDirRegKey HKLM "Software\TrilhaDaInovacao" "Pasta"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
VIProductVersion "${VERSAO}.0"
VIAddVersionKey "ProductName" "${NOME}"
VIAddVersionKey "CompanyName" "${EMPRESA}"
VIAddVersionKey "FileVersion" "${VERSAO}"
VIAddVersionKey "FileDescription" "Instalador da ${NOME}"
VIAddVersionKey "LegalCopyright" "${EMPRESA}"

!define MUI_ICON "${ICONE}"
!define MUI_UNICON "${ICONE}"
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Abrir a Trilha da Inovação"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "PortugueseBR"

Section "Trilha da Inovação" Principal
  SectionIn RO
  SetOutPath "$INSTDIR"
  File /r "${ORIGEM}\*.*"
  File "/oname=icone.ico" "${ICONE}"

  CreateDirectory "$SMPROGRAMS\${NOME}"
  CreateShortcut "$SMPROGRAMS\${NOME}\${NOME}.lnk" "$INSTDIR\${EXE}" "" "$INSTDIR\icone.ico"
  CreateShortcut "$SMPROGRAMS\${NOME}\${NOME} (sem óculos).lnk" "$INSTDIR\${EXE}" "-desktop" "$INSTDIR\icone.ico"
  CreateShortcut "$SMPROGRAMS\${NOME}\Desinstalar.lnk" "$INSTDIR\Desinstalar.exe"
  CreateShortcut "$DESKTOP\${NOME}.lnk" "$INSTDIR\${EXE}" "" "$INSTDIR\icone.ico"

  WriteUninstaller "$INSTDIR\Desinstalar.exe"
  WriteRegStr HKLM "Software\TrilhaDaInovacao" "Pasta" "$INSTDIR"
  WriteRegStr HKLM "${CHAVE_DESINSTALAR}" "DisplayName" "${NOME}"
  WriteRegStr HKLM "${CHAVE_DESINSTALAR}" "DisplayVersion" "${VERSAO}"
  WriteRegStr HKLM "${CHAVE_DESINSTALAR}" "Publisher" "${EMPRESA}"
  WriteRegStr HKLM "${CHAVE_DESINSTALAR}" "DisplayIcon" "$INSTDIR\icone.ico"
  WriteRegStr HKLM "${CHAVE_DESINSTALAR}" "UninstallString" '"$INSTDIR\Desinstalar.exe"'
  WriteRegDWORD HKLM "${CHAVE_DESINSTALAR}" "NoModify" 1
  WriteRegDWORD HKLM "${CHAVE_DESINSTALAR}" "NoRepair" 1
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\${NOME}.lnk"
  RMDir /r "$SMPROGRAMS\${NOME}"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "${CHAVE_DESINSTALAR}"
  DeleteRegKey HKLM "Software\TrilhaDaInovacao"
SectionEnd
