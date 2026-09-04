# Audit History Extractor Pro — Plugin XrmToolBox

🌐 [English](README.en.md) | **Español**

[![Build and Test](https://github.com/EdwingAlarcon/AuditHistoryExtractorPro-XrmToolBox/actions/workflows/build.yml/badge.svg)](https://github.com/EdwingAlarcon/AuditHistoryExtractorPro-XrmToolBox/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![XrmToolBox](https://img.shields.io/badge/XrmToolBox-Plugin-0072C6.svg)](https://www.xrmtoolbox.com/)

Plugin de [XrmToolBox](https://www.xrmtoolbox.com/) para **extraer, exportar y validar el
historial de auditoría de Dataverse** bajo demanda, sin necesidad de desplegar una app propia.

## Funcionalidad

- **Extraer**: filtrar historial de auditoría por entidad (con autocompletado desde metadata
  real), rango de fechas y tipo de operación (Create/Update/Delete/Access), previsualizar en
  grilla (con un resumen legible de qué campos cambiaron), cancelar una extracción larga en
  curso, y exportar a Excel, CSV o JSON.
- **Validar**: comparar un registro de auditoría puntual (por `AuditId`) contra el estado
  actual del registro en Dataverse — útil para detectar si un valor auditado sigue vigente.

No incluye reporte de roles de seguridad ni extracción incremental/jobs en segundo plano;
tampoco persiste historial entre sesiones — el plugin arranca en blanco cada vez, aunque los
archivos exportados sí quedan en tu disco. Ver
[`docs/README.md`](docs/README.md#alcance-decidido-con-el-usuario) para el detalle y la
justificación de cada decisión.

## Requisitos

- [XrmToolBox](https://www.xrmtoolbox.com/) instalado.
- Una conexión configurada en XrmToolBox a un entorno Dataverse / Dynamics 365.
- .NET Framework 4.8 o superior (ya lo trae XrmToolBox).

## Instalación

Este plugin todavía **no está publicado en el Plugin Store** de XrmToolBox (ver
[estado del proyecto](docs/README.md#estado-actual)) — hay que instalarlo manualmente. La
Tool Library de XrmToolBox ya no siempre muestra una opción "Install from disk" visible según
la versión; el método que funciona en cualquier versión es copiar los archivos directamente a
la carpeta `Plugins` del propio XrmToolBox:

1. Compilá el proyecto en `Release` (ver
   [instalación desde código fuente](#instalación-desde-código-fuente-para-desarrolladores)
   abajo) — eso deja listos, en `packaging\Plugins\`, los 11 archivos exactos que hacen falta
   (no hay que rescatarlos entre los ~150 DLLs de `bin\Release\net48\`).
2. Copiá esos archivos a `%AppData%\MscrmTools\XrmToolBox\Plugins\` (creá la carpeta si no
   existe) — o corré `powershell -File packaging\install-local.ps1`, que lo hace por vos.
3. Abrí (o reiniciá) XrmToolBox — el plugin **"Audit History Extractor Pro"** debería aparecer
   directo en la pantalla principal, sin pasar por Tool Library.

Si tu versión de XrmToolBox sí tiene "Install from disk" (Tool Library → botón correspondiente
según la versión), también podés generar el `.nupkg` (`nuget pack
packaging\AuditHistoryExtractorPro.XrmToolBox.nuspec -OutputDirectory packaging\output`) y
apuntarle ahí — el `.nuspec` ya está armado con la estructura `lib\net48\Plugins\` que exige
esa convención.

## Instalación desde código fuente (para desarrolladores)

```bash
git clone https://github.com/EdwingAlarcon/AuditHistoryExtractorPro-XrmToolBox.git
cd AuditHistoryExtractorPro-XrmToolBox
dotnet build AuditHistoryExtractorPro.XrmToolBox.sln -c Release
```

Al compilar en `Release`, `packaging\Plugins\` queda listo con los archivos exactos para copiar
según el paso 2 de arriba.

## Estructura del repositorio

```
src/
  AuditHistoryExtractorPro.XrmToolBox.Core/     Lógica pura (modelos, query builder, export, comparación)
  AuditHistoryExtractorPro.XrmToolBox.Plugin/   UserControl + integración con el host de XrmToolBox
tests/
  AuditHistoryExtractorPro.XrmToolBox.Core.Tests/
packaging/
  AuditHistoryExtractorPro.XrmToolBox.nuspec
  install-local.ps1                             Copia packaging\Plugins\ a tu XrmToolBox real
  Plugins/                                       (generado al compilar en Release, no versionado)
```

## Estado del proyecto

Compila de punta a punta y el empaquetado está verificado, pero **todavía no se probó contra
una instancia real de XrmToolBox/Dataverse**. Ver [`docs/README.md`](docs/README.md) para el
estado detallado y el roadmap corto.

## Contribuir

Los issues y pull requests son bienvenidos. Este es un proyecto de distribución interna en
camino a evaluar publicación en el Plugin Store — ver el roadmap en
[`docs/README.md`](docs/README.md#próximos-pasos-roadmap-corto).

## Licencia

[MIT](LICENSE) © Edwing Alarcón
