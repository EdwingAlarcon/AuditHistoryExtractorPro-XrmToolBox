<#
.SYNOPSIS
    Copia los archivos del plugin (ya juntados por el build en Release en packaging\Plugins\)
    directo a la carpeta Plugins de tu instalación de XrmToolBox.

.DESCRIPTION
    Compilá primero en Release (dotnet build AuditHistoryExtractorPro.XrmToolBox.sln -c Release,
    o desde Visual Studio) — eso deja listos en packaging\Plugins\ los ~11 archivos que hace
    falta copiar (el plugin, sus dependencias de terceros que XrmToolBox no trae, y el ícono).
    Este script los copia de ahí a la carpeta Plugins real, sin que tengas que buscarlos entre
    los ~150 DLLs de bin\Release\net462\.

.PARAMETER XrmToolBoxPluginsPath
    Carpeta Plugins de tu instalación de XrmToolBox. Por defecto %AppData%\MscrmTools\XrmToolBox\Plugins.

.EXAMPLE
    .\packaging\install-local.ps1
#>
param(
    [string]$XrmToolBoxPluginsPath = (Join-Path $env:APPDATA "MscrmTools\XrmToolBox\Plugins")
)

$ErrorActionPreference = "Stop"

$source = Join-Path $PSScriptRoot "Plugins"

if (-not (Test-Path $source)) {
    Write-Error "No se encontró '$source'. Compilá el proyecto en Release primero: dotnet build AuditHistoryExtractorPro.XrmToolBox.sln -c Release"
    exit 1
}

$archivos = Get-ChildItem -Path $source -File
if ($archivos.Count -eq 0) {
    Write-Error "'$source' está vacío. Compilá el proyecto en Release primero."
    exit 1
}

if (-not (Test-Path $XrmToolBoxPluginsPath)) {
    Write-Output "Creando '$XrmToolBoxPluginsPath'..."
    New-Item -ItemType Directory -Path $XrmToolBoxPluginsPath -Force | Out-Null
}

foreach ($archivo in $archivos) {
    Copy-Item -Path $archivo.FullName -Destination $XrmToolBoxPluginsPath -Force
}

Write-Output "Copiados $($archivos.Count) archivos a '$XrmToolBoxPluginsPath'."
Write-Output "Abrí (o reiniciá) XrmToolBox — 'Audit History Extractor Pro' debería aparecer en la pantalla principal."
