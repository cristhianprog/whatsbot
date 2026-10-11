@echo off
chcp 65001 >nul
title Compilar WhatsBot e atualizar o Portable do projeto
echo ============================================================
echo   COMPILAR WhatsBot  -  atualiza o Portable DESTE projeto
echo   (pasta: %~dp0)
echo ============================================================
echo.

rem Caminhos relativos a pasta deste .bat (funciona mesmo se a
rem pasta do projeto for movida para outro lugar)
set "RAIZ=%~dp0"
set "PROJ=%RAIZ%CodeHere\WASender\WASender.csproj"
set "DEBUG=%RAIZ%CodeHere\WASender\bin\Debug"
set "PORTABLE=%RAIZ%AppHere\Portable"
set "PDATA=C:\ProgramData\WaSender\SysFiles"
set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set "DEVENV=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.com"
set "SETUPDIR=%RAIZ%CodeHere\WaSenderSetUp\Debug"
set "SETUPDEST=%RAIZ%AppHere\SetUp"
set "MSILOG=%TEMP%\whatsbot-msi-build.log"

echo [1/5] Verificando se o app esta fechado...
tasklist /FI "IMAGENAME eq WhatsBot.exe" 2>nul | findstr /I "WhatsBot.exe" >nul
if %errorlevel%==0 (
    echo.
    echo   ERRO: o WhatsBot esta aberto!
    echo   Feche o app e rode este arquivo novamente.
    echo.
    pause
    exit /b 1
)
echo   App fechado - OK
echo.

echo [2/5] Compilando o projeto (Rebuild completo, ~1-2 min)...
if not exist "%MSBUILD%" (
    echo   ERRO: MSBuild nao encontrado em:
    echo   %MSBUILD%
    echo   Ajuste a variavel MSBUILD dentro deste .bat se necessario.
    pause
    exit /b 1
)
"%MSBUILD%" "%PROJ%" -t:Rebuild -p:Configuration=Debug -verbosity:minimal -nologo
if errorlevel 1 (
    echo.
    echo   ERRO DE COMPILACAO - leia as mensagens acima.
    echo   Se falar de "obj" travado, apague a pasta obj\Debug
    echo   e tente de novo.
    echo.
    pause
    exit /b 1
)
echo   Compilado com sucesso
echo.

echo [2b/5] Compilando o ScheduleChecker (precisa saber o novo nome do exe)...
"%MSBUILD%" "%RAIZ%CodeHere\ScheduleChecker\ScheduleChecker.csproj" -t:Rebuild -p:Configuration=Debug -verbosity:minimal -nologo
if errorlevel 1 (
    echo.
    echo   ERRO DE COMPILACAO no ScheduleChecker - leia as mensagens acima.
    pause
    exit /b 1
)
echo   ScheduleChecker compilado com sucesso
echo.

echo [3/5] Copiando arquivos novos para o Portable do projeto...
copy /Y "%DEBUG%\WhatsBot.exe"      "%PORTABLE%\" >nul
copy /Y "%DEBUG%\ScheduleChecker.exe" "%PORTABLE%\" >nul
del /F /Q "%PORTABLE%\WASender.exe" "%PORTABLE%\WASender.vshost.exe" >nul 2>&1
copy /Y "%DEBUG%\ProjectCommon.dll" "%PORTABLE%\" >nul
copy /Y "%DEBUG%\chatTemplate.csv"  "%PORTABLE%\" >nul
xcopy /Y /I /Q "%DEBUG%\languages"  "%PORTABLE%\languages" >nul
echo   Portable atualizado: %PORTABLE%
echo.

echo [4/5] Atualizando o CSV de execucao em ProgramData...
copy /Y "%DEBUG%\chatTemplate.csv" "%PDATA%\chatTemplate.csv" >nul
echo   ProgramData atualizado: %PDATA%
echo.

echo [5/5] Gerando o instalador MSI (Visual Studio, ~1-2 min)...
if not exist "%DEVENV%" (
    echo   AVISO: devenv.com nao encontrado - instalador NAO gerado:
    echo     %DEVENV%
    goto :instalador_fim
)
del /F /Q "%SETUPDIR%\WhatsBotSetUp.msi" >nul 2>&1
call :build_msi
if errorlevel 1 (
    echo   Primeira tentativa falhou - tentando de novo, erro de corrida conhecido do vdproj...
    ping -n 4 127.0.0.1 >nul
    call :build_msi
)
if not exist "%SETUPDIR%\WhatsBotSetUp.msi" (
    echo   ERRO: o instalador NAO foi gerado. Log completo:
    echo     %MSILOG%
    echo   O arquivo em AppHere\SetUp - se existir - e de um build ANTERIOR.
    echo   NAO distribua sem compilar de novo com sucesso.
    echo   Causas comuns: extensao "Visual Studio Installer Projects 2022"
    echo   ausente no VS, ou bug de corrida do vdproj - rodar de novo resolve.
    goto :instalador_fim
)
mkdir "%SETUPDEST%" >nul 2>&1
copy /Y "%SETUPDIR%\WhatsBotSetUp.msi" "%SETUPDEST%\" >nul
copy /Y "%SETUPDIR%\setup.exe"      "%SETUPDEST%\" >nul
echo   Instalador atualizado: %SETUPDEST%\WhatsBotSetUp.msi
:instalador_fim
echo.

echo ============================================================
echo   PRONTO! Versao nova compilada.
echo.
echo   Para usar, abra:
echo     %PORTABLE%\WhatsBot.exe
echo.
echo   Instalador para distribuir:
echo     %SETUPDEST%\WhatsBotSetUp.msi  (+ setup.exe)
echo.
echo   Dados (conta, sessao, campanhas) sao os mesmos - esta tudo
echo   em C:\ProgramData\WaSender, nao precisa escanear QR de novo.
echo.
echo   Para atualizar o app INSTALADO (Program Files):
echo   rode como administrador o ATUALIZAR-APP-como-administrador.bat
echo ============================================================
echo.
pause
exit /b 0

:build_msi
"%DEVENV%" "%RAIZ%CodeHere\WASender.sln" /Build Debug /Project WaSenderSetUp /ProjectConfig Debug > "%MSILOG%" 2>&1
exit /b %errorlevel%