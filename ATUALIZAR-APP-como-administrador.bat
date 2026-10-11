@echo off
echo ============================================
echo  ATUALIZACAO DO APP INSTALADO (WhatsBot)
echo  Copia o build recente para Program Files
echo  RODOU COMO ADMINISTRADOR? Precisa ser!
echo ============================================
echo.

set "INST=C:\Program Files (x86)\DigitPrime\WhatsBotSetUp"
rem Caminho relativo a pasta deste .bat (projeto pode ser movido)
set "DEBUG=%~dp0CodeHere\WASender\bin\Debug"

if not exist "%DEBUG%\WhatsBot.exe" (
    echo ERRO: build nao encontrado em:
    echo   %DEBUG%
    echo Compile primeiro com COMPILAR-E-ATUALIZAR-PORTABLE.bat
    echo.
    pause
    exit /b 1
)

copy /Y "%INST%\WhatsBot.exe" "%INST%\WhatsBot.exe.bak" >nul 2>&1
copy /Y "%INST%\ProjectCommon.dll" "%INST%\ProjectCommon.dll.bak" >nul
copy /Y "%INST%\chatTemplate.csv" "%INST%\chatTemplate.csv.bak" >nul

copy /Y "%DEBUG%\WhatsBot.exe" "%INST%\WhatsBot.exe"
if errorlevel 1 goto :erro
copy /Y "%DEBUG%\ProjectCommon.dll" "%INST%\ProjectCommon.dll"
if errorlevel 1 goto :erro
copy /Y "%DEBUG%\chatTemplate.csv" "%INST%\chatTemplate.csv"
if errorlevel 1 goto :erro
xcopy /Y /I /Q "%DEBUG%\languages" "%INST%\languages"
if errorlevel 1 goto :erro

echo.
echo SUCESSO! Arquivos atualizados no app instalado:
echo   - WhatsBot.exe
echo   - ProjectCommon.dll
echo   - chatTemplate.csv
echo   - languages (traducoes)
echo   Backups salvos com extensao .bak
echo.
echo   ATENCAO: o executavel agora chama-se WhatsBot.exe.
echo   Se houver um WASender.exe antigo na pasta, apague-o e
echo   aponte o atalho (desktop/inicio) para o WhatsBot.exe.
echo.
echo Nao esqueca: depois disto, rode tambem o
echo COMPILAR-E-ATUALIZAR-PORTABLE.bat para manter os Portables
echo em sincronia (ou use so os Portables).
echo.
pause
exit /b 0

:erro
echo.
echo ERRO ao copiar. Causas possiveis:
echo   1. Este .bat nao foi executado COMO ADMINISTRADOR
echo      (clique direito - Executar como administrador)
echo   2. O app WhatsBot esta aberto - feche e tente de novo
echo.
pause
exit /b 1
