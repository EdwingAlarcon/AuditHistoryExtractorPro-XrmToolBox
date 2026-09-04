# Audit History Extractor Pro — Plugin XrmToolBox

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6.2-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![XrmToolBox](https://img.shields.io/badge/XrmToolBox-Plugin-0072C6.svg)](https://www.xrmtoolbox.com/)

Plugin de [XrmToolBox](https://www.xrmtoolbox.com/) para **extraer, exportar y validar el
historial de auditoría de Dataverse** bajo demanda, sin necesidad de desplegar una app propia.

Proyecto independiente de [`AuditHistoryExtractorPro`](https://github.com/EdwingAlarcon)
(la versión Blazor Server + Worker Service on-premise): no comparte código en runtime con esa
app, es una reimplementación del subconjunto de funcionalidad que tiene sentido en un host
interactivo de un solo usuario. Ver [`CLAUDE.md`](CLAUDE.md) para el detalle de esa decisión.

## Funcionalidad

- **Extraer**: filtrar historial de auditoría por entidad, rango de fechas y tipo de operación
  (Create/Update/Delete/Access), previsualizar en grilla, y exportar a Excel, CSV o JSON.
- **Validar**: comparar un registro de auditoría puntual (por `AuditId`) contra el estado
  actual del registro en Dataverse — útil para detectar si un valor auditado sigue vigente.

No incluye reporte de roles de seguridad ni extracción incremental/jobs 24x7 (eso lo sigue
cubriendo el `Worker` de la app principal); tampoco persiste historial entre sesiones — el
plugin arranca en blanco cada vez, aunque los archivos exportados sí quedan en tu disco. Ver
[`docs/README.md`](docs/README.md#alcance-decidido-con-el-usuario) para el detalle y la
justificación de cada decisión.

## Requisitos

- [XrmToolBox](https://www.xrmtoolbox.com/) instalado.
- Una conexión configurada en XrmToolBox a un entorno Dataverse / Dynamics 365.
- .NET Framework 4.6.2 o superior (ya lo trae XrmToolBox).

## Instalación

Este plugin todavía **no está publicado en el Plugin Store** de XrmToolBox (ver
[estado del proyecto](docs/README.md#estado-actual)) — hay que instalarlo manualmente:

1. Compilá el proyecto en `Release` y generá el `.nupkg` (ver
   [«Cómo probar en una instancia real»](docs/README.md#cómo-probar-en-una-instancia-real)
   para el paso a paso completo), **o** descargá el `.nupkg` si alguien ya te lo compartió.
2. Abrí XrmToolBox → **Tool Library** → **Install from disk**.
3. Apuntá al archivo `.nupkg`.
4. Reiniciá XrmToolBox si te lo pide.
5. Buscá **"Audit History Extractor Pro"** en la lista de herramientas.

## Instalación desde código fuente (para desarrolladores)

```bash
git clone https://github.com/EdwingAlarcon/AuditHistoryExtractorPro-XrmToolBox.git
cd AuditHistoryExtractorPro-XrmToolBox
dotnet build AuditHistoryExtractorPro.XrmToolBox.sln -c Release
```

Con `nuget.exe` (viene con Visual Studio, o [descargalo](https://www.nuget.org/downloads)):

```bash
nuget pack packaging\AuditHistoryExtractorPro.XrmToolBox.nuspec -OutputDirectory packaging\output
```

El `.nupkg` queda en `packaging\output\`, listo para instalar con los pasos de arriba.

## Estructura del repositorio

```
src/
  AuditHistoryExtractorPro.XrmToolBox.Core/     Lógica pura (modelos, query builder, export, comparación)
  AuditHistoryExtractorPro.XrmToolBox.Plugin/   UserControl + integración con el host de XrmToolBox
tests/
  AuditHistoryExtractorPro.XrmToolBox.Core.Tests/
packaging/
  AuditHistoryExtractorPro.XrmToolBox.nuspec
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
