# CLAUDE.md

Guía de contexto para Claude Code al trabajar en este repositorio.

## Qué es este proyecto

Plugin de **XrmToolBox** para extraer, exportar y validar el historial de auditoría de
Dataverse bajo demanda, sin necesidad de desplegar una app propia — pensado para el trabajo
manual/ad-hoc de un consultor frente al cliente, dentro del modelo de host interactivo de un
solo usuario que ofrece XrmToolBox.

## Decisiones ya tomadas (no re-discutir sin motivo)

- **Sin reporte de roles de seguridad** — excluido a pedido explícito del usuario.
- **Sin persistencia de historial entre sesiones** — todo en memoria por diseño para el MVP.
  El plugin no guarda registro de "qué se extrajo antes" al cerrar XrmToolBox; el archivo
  exportado (Excel/CSV/JSON) sí queda en disco donde el usuario lo guardó, pero el plugin no
  lo recuerda al reabrir. Es una decisión deliberada (menos superficie de bugs, bajo valor en
  herramienta de uso puntual) y es aditiva: se puede agregar después sin rediseñar nada.
- **Sin extracción incremental / jobs 24x7** — fuera de alcance: XrmToolBox es un host
  interactivo de un solo usuario, no encaja con jobs persistentes en background.
- **Target framework: `net462`** (tanto `Core` como `Plugin`) — compatibilidad máxima con el
  host de XrmToolBox actual, que sigue siendo mayoritariamente .NET Framework 4.6.2. No migrar
  a .NET 8 sin reevaluar la adopción real de "XrmToolBox 2.0".
- **Sin autenticación propia** — el plugin recibe `IOrganizationService`/`ConnectionDetail` ya
  autenticados desde el host (`PluginControlBase.UpdateConnection`).
- **Distribución:** ahora interna. La Tool Library de XrmToolBox no siempre expone "Install
  from disk" según la versión del host — el método confiable es copiar los DLLs a la carpeta
  `Plugins` del host (ver `docs/README.md`); el `.nupkg` queda como alternativa para cuando esa
  opción sí está disponible, y es el formato que se necesitará igual para el Plugin Store más
  adelante. Meta declarada por el usuario: publicación pública en el XrmToolBox Plugin Store
  una vez validado con usuarios reales — diseñar/documentar pensando en eventual certificación
  pública, pero sin bloquear el avance por eso todavía.

## Estado actual (última sesión: 2026-09-04)

**Compila de punta a punta** (`Core`, `Plugin`, `Core.Tests` — 0 errores, 6/6 tests OK vía
`dotnet build` / `dotnet test`). Ver `docs/README.md` para el detalle de estructura y estado.
Resumen:

- ✅ Estructura de solución completa (`Core`, `Plugin`, `Core.Tests`, `packaging/`, `docs/`).
- ✅ Modelos (`AuditRecord`, `AuditAction`, `AuditQueryFilters`, `AuditComparisonResult`).
- ✅ `AuditQueryBuilder` (arma `QueryExpression` contra `audit`) — con test de ejemplo.
- ✅ Exportadores Excel (ClosedXML) / CSV (CsvHelper) / JSON (Newtonsoft.Json).
- ✅ `AuditComparisonService` (spot-check contra Dataverse).
- ✅ `AuditChangeDataParser` (`Core/Parsing/`) — parsea `oldValues`/`newValues` del XML
  `changedata` de la entidad `audit`. Con 4 tests unitarios.
- ✅ `PluginControl` con pestañas "Extraer" (filtros + `WorkAsync`, ya llena
  `OldValues`/`NewValues`/`EntityLogicalName`) y "Validar" (ya no es un placeholder: dispara
  `WorkAsync`, reconstruye el `AuditRecord` desde el `AuditId` y compara contra Dataverse vía
  `AuditComparisonService`).
- ✅ `ExtraccionView` ahora previsualiza en grilla: "Extraer" llena una `DataGridView` en memoria
  (vía `WorkAsync`/Dataverse) y "Exportar..." (habilitado solo con datos cargados) exporta lo que
  está en la grilla a xlsx/csv/json — la exportación es E/S local, no pasa por `WorkAsync`.
- ✅ `.nuspec` de empaquetado, `AssemblyInfo.cs` con atributos MEF (`[Export(typeof(IXrmToolBoxPlugin))]`).
- ✅ Referencia al SDK real verificada: el id de paquete correcto es **`XrmToolBoxPackage`**
  (no `XrmToolBox.Extensibility`, que no existe). Fijado en `1.2023.10.67` — la última versión
  que aún publica binarios `net462` (desde `1.2025.7.71`, jul-2025, el paquete pasó a ser
  `net48`-only en NuGet; si en el futuro se necesita seguir el paquete más nuevo, ahí sí hay
  que reevaluar la decisión de target `net462`, no antes). Con `1.2023.10.67` no hizo falta
  tocar la API de `PluginControlBase`/`SettingsManager`/`IGitHubPlugin`/`IHelpPlugin`.
- ✅ `PluginPackage.png` reemplazado por un ícono real de 32x32 (lupa sobre círculo azul).
- ✅ `.nuspec` corregido dos veces: primero se sacó la dependencia NuGet inexistente
  (`XrmToolBox.Extensibility 4.0.0`); después se corrigió la estructura completa siguiendo la
  convención real de XrmToolBox (confirmada contra la wiki oficial de MscrmTools/XrmToolBox):
  los DLLs van bajo `lib\net462\Plugins\` (no `lib\net462\` a secas), y las dependencias de
  terceros (`ClosedXML` + su árbol — `DocumentFormat.OpenXml`, `ExcelNumberFormat`,
  `SixLabors.Fonts`, `XLParser`, `Irony`, `System.IO.Packaging` —, `CsvHelper`) se incluyen
  como **archivos** dentro de `<files>`, no como `<dependencies>` NuGet (ese mecanismo de
  resolución no aplica al instalar un plugin en Tool Library).
- ✅ Empaquetado verificado con `nuget.exe pack` real (descargado para esta sesión, ya que no
  hay Visual Studio instalado acá) y se confirmó que el `.nupkg` contiene todo bajo
  `lib\net462\Plugins\`. No se versiona (`.gitignore` ya lo excluye) — hay que regenerarlo
  localmente antes de instalar.
- ✅ Instalación manual documentada: como la Tool Library de XrmToolBox no siempre expone
  "Install from disk" según la versión del host, se documentó (y es el método principal en
  `docs/README.md`) copiar los DLLs directo a `%AppData%\MscrmTools\XrmToolBox\Plugins\` — el
  propio proyecto (`Plugin.dll`, `Core.dll`), los 8 DLLs de terceros de arriba, y el ícono. No
  copiar ensamblados del SDK/host (`Microsoft.Xrm.Sdk.dll`, `XrmToolBox.Extensibility.dll`,
  etc.) — esos ya están cargados por el host y duplicarlos puede romper el assembly loading.
- ⚠️ Sigue sin probarse contra una instancia real de XrmToolBox/Dataverse (solo se validó que
  compila, que los tests unitarios de `Core` pasan, y que el `.nupkg` empaqueta correctamente;
  no hay smoke test end-to-end con el host real). Ver instrucciones de prueba manual abajo.

## Próximos pasos (en orden)

1. ~~Previsualización en grilla en `ExtraccionView`~~ — hecho.
2. ~~Generar `PluginPackage.png` real~~ — hecho.
3. **Probar en una instancia real de XrmToolBox contra un entorno Dataverse de prueba** — ver
   `docs/README.md#cómo-probar-en-una-instancia-real` para el paso a paso. Es el primer punto
   pendiente real; nada de lo anterior se validó contra un host XrmToolBox de verdad.
4. Empaquetar y distribuir internamente (ya validado que `dotnet pack` genera el `.nupkg`
   correctamente — falta el paso 3 antes de considerar esto "distribuible").
5. Recolectar feedback de 2-3 usuarios antes de evaluar el checklist de certificación pública
   del Plugin Store.

## Convenciones

- Comentarios y textos de UI en español.
- Sin DI de MediatR, sin `IHostedService`, sin `appsettings.json` — el modelo de host de
  XrmToolBox no los necesita (una sola conexión, un solo usuario, sin jobs en background).
