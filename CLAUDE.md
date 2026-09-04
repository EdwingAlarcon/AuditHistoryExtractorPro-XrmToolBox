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
- **Target framework: `net48`** (tanto `Core` como `Plugin`) — migrado desde `net462` el
  2026-09-04 tras confirmar en runtime que `net462` + `XrmToolBoxPackage` viejo no cargaba
  contra una instalación real de XrmToolBox (ver "Estado actual" abajo para el detalle). Seguir
  la versión real del host de XrmToolBox instalado, no una vieja "por compatibilidad". No
  migrar a .NET 8 sin reevaluar la adopción real de "XrmToolBox 2.0".
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

**Compila de punta a punta** (`Core`, `Plugin`, `Core.Tests` — 0 errores, 9/9 tests OK vía
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
- ✅ `.nuspec` de empaquetado, `AssemblyInfo.cs`.
- ✅ Referencia al SDK real verificada y **migrada a la versión exacta del host** (ver bug
  crítico #2 más abajo): el id de paquete correcto es **`XrmToolBoxPackage`** (no
  `XrmToolBox.Extensibility`, que no existe). Se probó primero fijado en `1.2023.10.67` (la
  última versión `net462`, de antes de que el paquete pasara a `net48`-only en `1.2025.7.71`,
  jul-2025) — compilaba y componía bien en una simulación MEF, pero fallaba contra una
  instalación real. Ahora fijado en **`1.2025.10.74`** (verificado en runtime, con `net48`), más
  una referencia directa a `MscrmTools.Xrm.Connection 1.2025.9.64` (la resolución transitiva de
  NuGet caía en `1.2025.7.63`, más vieja que la real). No hizo falta tocar la lógica de
  `PluginControlBase`/`SettingsManager`/`IGitHubPlugin`/`IHelpPlugin` — solo las referencias de
  paquete y el target framework.
- ✅ `PluginPackage.png` reemplazado por un ícono real de 32x32 (lupa sobre círculo azul).
- ✅ `.nuspec` corregido dos veces: primero se sacó la dependencia NuGet inexistente
  (`XrmToolBox.Extensibility 4.0.0`); después se corrigió la estructura completa siguiendo la
  convención real de XrmToolBox (confirmada contra la wiki oficial de MscrmTools/XrmToolBox):
  los DLLs van bajo `lib\net48\Plugins\` (no `lib\net48\` a secas), y las dependencias de
  terceros (`ClosedXML` + su árbol — `DocumentFormat.OpenXml`, `ExcelNumberFormat`,
  `SixLabors.Fonts`, `XLParser`, `Irony`, `System.IO.Packaging` —, `CsvHelper`) se incluyen
  como **archivos** dentro de `<files>`, no como `<dependencies>` NuGet (ese mecanismo de
  resolución no aplica al instalar un plugin en Tool Library).
- ✅ Empaquetado verificado con `nuget.exe pack` real (descargado para esta sesión, ya que no
  hay Visual Studio instalado acá) y se confirmó que el `.nupkg` contiene todo bajo
  `lib\net48\Plugins\`. No se versiona (`.gitignore` ya lo excluye) — hay que regenerarlo
  localmente antes de instalar.
- ✅ Instalación manual documentada y automatizada: como la Tool Library de XrmToolBox no
  siempre expone "Install from disk" según la versión del host, se documentó (y es el método
  principal en `docs/README.md`) copiar los archivos directo a
  `%AppData%\MscrmTools\XrmToolBox\Plugins\`. Para no tener que rescatarlos entre los ~150 DLLs
  de `bin\Release\net48\` (todo lo que trae `XrmToolBoxPackage`/`Microsoft.CrmSdk`, que no hay
  que tocar), `Plugin.csproj` tiene un `Target AfterTargets="Build"` (solo en `Configuration ==
  Release`) que junta los 11 archivos exactos en `packaging\Plugins\` — mismo contenido que
  `lib\net48\Plugins\` en el `.nuspec`, hay que mantener ambas listas sincronizadas si cambian
  las dependencias de terceros de `Core`. `packaging\install-local.ps1` copia esa carpeta directo
  a la instalación real de XrmToolBox (autodetecta `%AppData%\...\Plugins`, o se le puede pasar
  `-XrmToolBoxPluginsPath`). Ninguna de las dos carpetas (`packaging\Plugins\`, `packaging\output\`)
  se versiona.
- ✅ **Bug real corregido antes de probarse**: `AuditQueryBuilder` seteaba `TopCount` en la
  `QueryExpression` Y `PluginControl.EjecutarExtraccion` seteaba `PageInfo` — Dataverse no
  permite combinar ambos en la misma consulta (falla en runtime). Se sacó `TopCount`; el límite
  de `MaxRegistros` ahora se aplica cortando la paginación del lado del cliente.
- ✅ **Cancelación real en "Extraer"**: `WorkAsyncInfo.IsCancelable = true` + chequeo de
  `worker.CancellationPending` en cada página. Al cancelar, se muestran los registros ya
  obtenidos hasta ese punto (se acumulan en una variable de método, no en `args.Result`, porque
  `RunWorkerCompletedEventArgs.Result` no es accesible cuando `Cancelled == true`).
- ✅ **`MaxRegistros` expuesto en la UI**: `NumericUpDown` en "Extraer" (100 a 500.000, default
  50.000) en vez de quedar fijo y oculto en el modelo.
- ✅ **Combo de entidades con autocompletado**: botón "Cargar entidades..." dispara
  `RetrieveAllEntitiesRequest` (filtrando solo entidades con `IsAuditEnabled = true`) y llena un
  `ComboBox` con autocompletado — reduce el error de tipear mal el nombre lógico, que antes daba
  "0 resultados" en silencio. Sigue aceptando texto libre si el usuario no quiere cargar el combo.
- ✅ **`AuditRecord.ResumenCambios`**: propiedad calculada ("campo: antes → después") que arma la
  grilla de "Extraer" sin exponer los diccionarios `OldValues`/`NewValues` crudos (que se siguen
  ocultando como columnas). Con 3 tests unitarios.
- ✅ **Versión centralizada**: `Directory.Build.props` en la raíz fija `<Version>0.1.0</Version>`,
  heredada por `Core.csproj` (antes no tenía `<Version>` propia y su ensamblado quedaba en
  `1.0.0.0` por default — bug real, ya corregido: ahora `0.1.0.0` en ambos). `Plugin.csproj` tiene
  `GenerateAssemblyInfo=false`, así que su versión real la sigue gobernando `AssemblyInfo.cs`
  (manual, por los atributos MEF) — quedan 2 lugares para sincronizar al bumpear versión
  (`AssemblyInfo.cs` + `packaging/*.nuspec`), uno menos que antes.
- ✅ **CI en GitHub Actions** (`.github/workflows/build.yml`): `dotnet build` + `dotnet test` en
  cada push/PR a `master`, en `windows-latest` (necesario por `net48`).
- ✅ **DOS BUGS CRÍTICOS CORREGIDOS (2026-09-04, primera prueba real del usuario contra
  XrmToolBox `1.2025.10.74`): el plugin no aparecía en XrmToolBox.** Se encontraron y
  corrigieron en secuencia, cada uno verificado antes de pasar al siguiente:

  1. **Export de MEF mal armado.** `[Export(typeof(IXrmToolBoxPlugin))]` estaba puesto directo
     en `PluginControl` (el `UserControl : PluginControlBase`), que **no implementa**
     `IXrmToolBoxPlugin` — implementa `IGitHubPlugin`/`IHelpPlugin`, y `PluginControlBase`
     implementa `IXrmToolBoxPluginControl` (una interfaz *distinta*). El patrón correcto de
     XrmToolBox (confirmado contra la wiki oficial "Develop your own custom plugin") usa **dos
     clases separadas**: una clase descriptora chica (`Plugin : PluginBase`, nuevo archivo
     `Plugin.cs`) que lleva el `[Export]`/`[ExportMetadata]` y solo implementa
     `GetControl() => new PluginControl()`; el `UserControl` (`PluginControl.cs`) queda sin
     atributos Export. MEF compone el catálogo por contrato de tipo — al exportar un tipo que
     no satisface `IXrmToolBoxPlugin`, el host lo descarta **en silencio** (sin excepción, sin
     log), que es exactamente el síntoma que reportó el usuario (otros plugins cargaban bien,
     el nuestro no aparecía, logs vacíos).

  2. **Referencias de ensamblado desactualizadas (`net462` → `net48`).** Corregido el bug #1 y
     verificado por composición MEF simulada (contra el `XrmToolBoxPackage 1.2023.10.67` con el
     que compilaba el proyecto: `GetExports<IXrmToolBoxPlugin>()` pasó de 0 a 1 export), el
     usuario probó en su XrmToolBox real y **seguía sin aparecer**. Repitiendo la misma
     composición MEF pero cargando los ensamblados *reales* de la instalación del usuario
     (`C:\...\XrmToolBox.exe` y sus DLLs, no los del paquete NuGet), la composición falló con
     `ReflectionTypeLoadException` → `No se puede cargar el archivo o ensamblado
     'McTools.Xrm.Connection, Version=1.2023.6.56'`. Causa: `McTools.Xrm.Connection` es un
     ensamblado firmado (tiene `PublicKeyToken`), y .NET Framework no permite bindear una
     versión distinta a la referenciada sin una redirección explícita — que no existe en el
     host para una versión tan vieja. Se migró `Core`/`Plugin`/`Core.Tests` de `net462` a
     `net48`, `XrmToolBoxPackage` de `1.2023.10.67` a `1.2025.10.74` (la versión real
     verificada), `Microsoft.CrmSdk.CoreAssemblies` de `9.0.2.51` a `9.0.2.60` (mínimo que exige
     la nueva `XrmToolBoxPackage`), y se agregó una referencia directa a
     `MscrmTools.Xrm.Connection 1.2025.9.64` (sin ella, NuGet resolvía transitivamente al
     mínimo `1.2025.7.63`, más vieja que la real — mismo problema de nuevo, un escalón más
     arriba). Re-verificado con la misma composición MEF fuera de proceso, esta vez cargando los
     ensamblados reales del host: 1 export encontrado, tipo correcto, sin ningún error de carga.

  Ambos verificados con `System.ComponentModel.Composition.Hosting.DirectoryCatalog` +
  `CompositionContainer` sobre `packaging\Plugins\`, corridos con Windows PowerShell clásico
  (por ser .NET Framework real, no PowerShell 7/.NET). Archivos copiados a
  `%AppData%\MscrmTools\XrmToolBox\Plugins` del usuario tras cada fix para reintentar en vivo.
- ⚠️ Sigue sin confirmarse con el usuario que el plugin ahora carga en su XrmToolBox real tras
  el segundo fix (el fix está verificado por composición MEF fuera de proceso contra los
  ensamblados reales del host, no por una corrida real del proceso `XrmToolBox.exe` todavía).
  Falta también probar el flujo funcional completo (Extraer/Validar) contra Dataverse.

## Próximos pasos (en orden)

1. ~~Previsualización en grilla en `ExtraccionView`~~ — hecho.
2. ~~Generar `PluginPackage.png` real~~ — hecho.
3. ~~CI, cancelación, límite visible, combo de entidades, resumen de cambios en grilla,
   versión centralizada~~ — hecho (ver arriba).
4. **Confirmar con el usuario que el plugin ahora aparece en su XrmToolBox real** (recompilar,
   volver a copiar `packaging\Plugins\` — el fix de MEF cambió `Plugin.dll`) y seguir con
   `docs/README.md#cómo-probar-en-una-instancia-real` para el resto del flujo funcional
   (Extraer/Validar contra Dataverse, combo de entidades, cancelación).
5. Empaquetar y distribuir internamente (ya validado que el `.nupkg` empaqueta correctamente —
   falta el paso 4 antes de considerar esto "distribuible").
6. Recolectar feedback de 2-3 usuarios antes de evaluar el checklist de certificación pública
   del Plugin Store.

## Convenciones

- Comentarios y textos de UI en español.
- Sin DI de MediatR, sin `IHostedService`, sin `appsettings.json` — el modelo de host de
  XrmToolBox no los necesita (una sola conexión, un solo usuario, sin jobs en background).
