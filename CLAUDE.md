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
- ✅ **TERCER Y CUARTO BUG CRÍTICO CORREGIDOS (2026-09-04, misma sesión, mismo síntoma "no
  aparece").** Con los dos bugs anteriores resueltos, el usuario actualizó a XrmToolBox
  `1.2026.8.76` y el plugin seguía sin aparecer — pero esta vez sin ningún error visible. Se
  investigó con evidencia directa contra la instalación real del usuario (no solo simulación):

  1. **Ícono vacío tumbaba XrmToolBox al abrir.** `Plugin.cs` tenía
     `[ExportMetadata("SmallImageBase64", "")]`/`BigImageBase64` en blanco desde el principio —
     nunca se había probado esa parte del pipeline (la verificación MEF anterior solo chequeaba
     que el export existiera, no que el host lograra construir el tile visual). El Visor de
     eventos de Windows (`Get-WinEvent`, log `Application`, proveedor `.NET Runtime`/`Application
     Error`) mostró crashes repetidos de `XrmToolBox.exe` con excepción no controlada en el
     mensaje de UI justo en el rango horario en que el usuario decía "no aparece" — coincide con
     el host intentando decodificar un Base64 vacío como imagen al armar el tile del plugin.
     Se generó el ícono real en Base64 (a partir de `Resources\PluginPackage.png`, con Python/PIL:
     32×32 para `SmallImageBase64`, 150×150 para `BigImageBase64`) y se embebió en `Plugin.cs`.
     Tras el fix, XrmToolBox dejó de crashear (confirmado abriendo el proceso real y monitoreando
     el Visor de eventos: cero crashes nuevos).
  2. **`SecondaryFontColor` faltante excluía el plugin en silencio (sin crash).** Con el crash
     resuelto, el plugin seguía sin aparecer en el buscador de herramientas (confirmado con
     capturas de pantalla reales de la ventana de XrmToolBox abierta). Se encontró
     `Plugins\manifest.json` (el catálogo que el host arma escaneando `Plugins\` — contiene
     `ScannedAssemblies`, todo DLL encontrado, y `PluginMetadata`, solo los que el host logra
     materializar como plugin válido): nuestros DLLs aparecían en `ScannedAssemblies` pero nunca
     en `PluginMetadata`. Comparando contra las 371 entradas reales de `PluginMetadata` de otros
     plugins instalados, **las 371 tenían `SecondaryFontColor` seteado — ninguna vacía** — campo
     que `Plugin.cs` no declaraba. Causa probable: el host arma esa lista vía una consulta MEF con
     vista de metadata tipada (a diferencia de la consulta simple `GetExportedValues<T>()` sin
     vista que usamos para verificar, que sí encontraba el export igual) — sin esa propiedad, el
     export queda excluido de esa consulta en particular, en silencio. Se agregó
     `[ExportMetadata("SecondaryFontColor", "Gray")]`. **Verificado de punta a punta abriendo el
     proceso real `XrmToolBox.exe 1.2026.8.76` del usuario, tomando captura de pantalla real y
     buscando "Audit" en el buscador de herramientas: "Audit History Extractor Pro" aparece con
     ícono, descripción y autor correctos.** Primera confirmación visual real de que el plugin
     carga en un XrmToolBox real, no solo por composición MEF fuera de proceso.

  De paso se corrigió un bug menor preexistente en `packaging\install-local.ps1`: sin BOM UTF-8,
  Windows PowerShell clásico (5.1) interpreta mal los acentos del archivo y tira
  `TerminatorExpectedAtEndOfString` — se le agregó BOM.

- ✅ **NullReferenceException al abrir la herramienta ("Referencia a objeto no establecida...")
  — corregido.** `_extraccionView`/`_validarView` se creaban en el evento `Load` de
  `PluginControl`, pero el host llama a `UpdateConnection` (que las usa) apenas instancia el
  control vía `Plugin.GetControl()` — antes de que el control tenga handle de ventana y dispare
  `Load`. Se movió la construcción de las vistas al constructor.
- ✅ **Ícono rediseñado.** El anterior salía de estirar un PNG de 32×32 a 150×150 sin
  antialiasing real (se veía borroso). Rediseñado a 512×512 con supersampling 4×
  (documento + lupa); se regeneraron `SmallImageBase64` (50×50), `BigImageBase64` (150×150) y
  `Resources\PluginPackage.png` (32×32) a partir de ese original.
- ✅ **Paridad de columnas con la app web hermana (`audit-history-extractor-pro`) y fix del
  límite silencioso de registros (2026-09-04, feedback directo del usuario tras probar el
  plugin).** El usuario notó dos problemas reales comparando contra la app web:
  1. **Columnas de más en la app web.** Su export CSV (17 columnas, esquema
     `RecordId, AuditId, EntityId, ActionId, OperationId, OldValue, NewValue, CreatedOn,
     EntityName, UserId, AttributeName, RecordKeyValue, Action, Operation, Username,
     LookupOldValue, LookupNewValue`) es **una fila por campo cambiado**; el plugin exportaba
     una fila por evento con los cambios colapsados en texto libre (`ResumenCambios`), sin
     `operation` (el DML base, atributo Dataverse distinto de `action`, ya estaba en el
     `ColumnSet` de `AuditQueryBuilder` pero no se leía) ni el *display name* de los lookups
     (`LookupOldValue`/`LookupNewValue`). Se agregó `AuditRecord.Operation`
     (nuevo enum `AuditOperation`) y `LookupOldValues`/`LookupNewValues` (diccionarios, igual
     forma que `OldValues`/`NewValues`). El *display name* del lookup se obtiene del propio XML
     de `changedata` que ya se leía — Dataverse pone el Id en el atributo `value` del nodo y el
     nombre legible como texto del nodo (`AuditChangeDataParser` ahora separa ambos). En esta
     primera vuelta, a propósito NO se migró a `RetrieveAuditDetailsRequest` (lo que realmente
     usa la app web) para no pagar el costo de una llamada SDK extra por registro en una
     herramienta interactiva — **decisión revertida el mismo día, ver bug crítico de abajo: sin
     ese mensaje, `changedata` viene vacío para la enorme mayoría de los eventos en un entorno
     real, así que el aplanado por campo no tenía nada que aplanar.** Los exportadores CSV/Excel
     se reescribieron para aplanar
     cada `AuditRecord` a una fila por campo cambiado (`AuditExportRowFlattener`, compartido por
     ambos, con fallback a una fila con columnas de campo vacías para eventos sin cambios de
     campo — Create/Delete/Access). El export JSON no se tocó: como no es tabular, sigue
     serializando el objeto completo (ahora con los campos nuevos incluidos). La grilla de
     previsualización interactiva **se dejó como estaba** (evento por evento, con
     `ResumenCambios`) — es más legible para escanear visualmente; el aplanado detallado es
     solo al exportar, que es lo que consume el usuario río abajo.
  2. **Límite de registros silencioso.** `MaxRegistros` cortaba la paginación sin avisar si
     había más registros disponibles — para una herramienta de auditoría/compliance eso es
     grave (se puede creer que se vio todo el historial cuando en realidad se cortó). Se agregó
     `AuditQueryFilters.SinLimite` (default `true` — sin límite, saca *todo* lo que haya para el
     rango de fechas, igual que la app web). El checkbox "Sin límite (traer todo)" en
     `ExtraccionView` está tildado por defecto y deshabilita el `NumericUpDown`; si el usuario
     lo destilda y el límite corta el resultado habiendo más disponibles,
     `PluginControl.EjecutarExtraccion` ahora lo detecta (`seCortoPorLimite`, distinto de
     "se acabaron las páginas") y muestra una advertencia explícita en vez de terminar en
     silencio.

- ✅ **BUG CRÍTICO CORREGIDO (2026-09-04, mismo día, primera extracción real del usuario contra
  un entorno con volumen): `changedata` viene vacío en la práctica — el export "aplanado" nunca
  tenía nada que aplanar.** El usuario extrajo enero–marzo 2026 de una entidad con volumen real:
  63.713 eventos, pero **todas** las filas de export tenían las columnas `Campo`/`ValorAnterior`/
  `ValorNuevo`/etc. vacías (el fallback de "evento sin cambios" de `AuditExportRowFlattener`
  disparándose siempre) — y el CSV de la app web para el mismo universo tenía 388.489 filas
  (≈6× más, consistente con ~6 campos cambiados en promedio por evento SÍ aplanado
  correctamente). Causa: un `RetrieveMultiple` simple contra la entidad `audit` con `changedata`
  en el `ColumnSet` **no garantiza traer el detalle real de cambios** contra un entorno real de
  producción (a diferencia de contra los datos de prueba usados para los tests unitarios, donde
  siempre "funcionaba"). El mensaje correcto — y el que la app web ya usa en producción — es
  `RetrieveAuditDetailsRequest` (`Microsoft.Crm.Sdk.Messages`), que devuelve un `AuditDetail`
  (en el caso normal, un `AttributeAuditDetail` con `OldValue`/`NewValue` como `Entity`
  completas, incluidas `FormattedValues` y el `Name` real de los lookups). **Fix**: nuevo
  `AuditDetailPopulator` (`Core/Queries/`) que, por cada página de 5000 registros leída,
  pide el detalle real vía `ExecuteMultipleRequest` en lotes de 500 (mismo tamaño de lote que
  usa la app web) y reemplaza `OldValues`/`NewValues`/`LookupOldValues`/`LookupNewValues` cuando
  el mensaje devuelve algo — si el lote entero falla o un `AuditId` puntual falla, ese registro
  se queda con el fallback de `changedata` que ya traía desde `MapearEntidadAuditRecord` (no se
  pierde el registro, solo puede quedar sin detalle de campos). Costo: una llamada SDK adicional
  por cada 500 registros (antes solo se pagaba el `RetrieveMultiple` de la página) — el trade-off
  de performance que se había evitado a propósito en el punto anterior resultó no ser opcional:
  sin este mensaje, el dato de campos cambiados directamente no existe. Pendiente de confirmar
  con el usuario que, tras el fix, el conteo de filas aplanadas se acerca al de la app web para
  el mismo universo.

- ✅ **Flag de integridad `AuditRecord.DetalleIncompleto`** (2026-09-04, a pedido explícito del
  usuario: "que no se tenga duda de que lo que están descargando es real y verídico"). Antes, si
  `RetrieveAuditDetailsRequest` fallaba para un registro puntual o para un lote entero (batch de
  `ExecuteMultipleRequest`), ese registro caía al fallback de `changedata` **en silencio** — sin
  forma de distinguir "genuinamente no tuvo cambios de campo" de "no se pudo verificar". Se
  agregó `DetalleIncompleto` (bool, default `false`) a `AuditRecord`, seteado en `true` por
  `AuditDetailPopulator` únicamente cuando la llamada SDK falló (fault de `ExecuteMultiple` para
  ese `AuditId`, o excepción en el batch completo) — nunca por "el evento no tenía cambios",
  que es un resultado legítimo. Se agregó como columna al export (`AuditExportRowFlattener`,
  penúltima antes de las columnas de campo) y `PluginControl.EjecutarExtraccion` cuenta cuántos
  registros terminaron con el flag en `true` y muestra una advertencia explícita al final si hay
  alguno, con el conteo. **Nota:** eventos de tipo `RelationshipAuditDetail`/`ShareAuditDetail`/
  `RolePrivilegeAuditDetail` (Associate/Disassociate, Share, cambios de rol) devuelven un
  `AuditDetail` que NO es `AttributeAuditDetail` — hoy `AuditDetailPopulator.AplicarDetalle` los
  ignora (no son un fallo, simplemente no se parsean sus datos específicos) y por lo tanto NO se
  marcan como `DetalleIncompleto` — quedan con columnas de campo vacías como si no hubiera
  cambios, lo cual es engañoso para esos tipos de evento puntuales. Pendiente si se necesita
  soporte real para esos tres tipos (la app web sí los soporta, ver
  `DataverseAuditRepository.ApplyAuditDetail` en el repo hermano).

- ✅ **Botón "Cancelar" restaurado en `ExtraccionView`** (2026-09-04, el usuario preguntó "no lo
  veo en ningún lado" tras notar que la extracción con `RetrieveAuditDetailsRequest` es mucho más
  lenta). La decisión de sacarlo (ver más arriba, "Cancelación real en Extraer") partía de un
  supuesto incorrecto: que el host dibuja su propio botón Cancelar cuando
  `WorkAsyncInfo.IsCancelable = true`. **Falso** — decompilado `XrmToolBoxPackage 1.2025.10.74`
  con `ilspycmd` (`dotnet tool install -g ilspycmd`) para confirmarlo:
  `PluginControlBase.WorkAsync` delega en `Worker.WorkAsync`, que arma el cartel de progreso
  vía `InformationPanel.GetInformationPanel(host, message, width, height)` — un `Panel` con un
  `Label` y un GIF girando, **sin ningún botón**; `IsCancelable` solo se usa para
  `BackgroundWorker.WorkerSupportsCancellation`, que habilita `PluginControlBase.CancelWorker()`
  a nivel de código pero no expone ninguna UI para dispararlo. El cartel además es chico y
  centrado (340×150 default) — no tapa toda la pantalla, así que un botón propio en otra parte
  del control no queda tapado. Se agregó `btnCancelar` en `ExtraccionView` (al lado de
  "Extraer", deshabilitado salvo mientras hay una extracción en curso), evento
  `SolicitarCancelacion` conectado en `PluginControl` directo al `CancelWorker()` heredado de
  `PluginControlBase`.

- ✅ **Progreso con tiempo transcurrido/restante estimado y velocidad** (2026-09-04, el usuario
  preguntó si se podía agregar viendo que la app web ya lo tiene). `AuditQueryBuilder.BuildConteo`
  (nuevo) arma una versión liviana de la misma consulta — mismo filtro, pero `ColumnSet` con
  solo `auditid` y sin el join a `systemuser` — que `EjecutarExtraccion` corre primero, paginando
  solo para sumar cuántos registros hay en total (mensaje "Contando registros a extraer..."), sin
  pagar el costo de `changedata` ni de `RetrieveAuditDetailsRequest`. Con ese total conocido, el
  mensaje de progreso (`PluginControl.FormatearProgreso`) arma "extraídos / total (%) ·
  Transcurrido hh:mm:ss · Restante ~hh:mm:ss · N reg/s" — el cálculo de restante es
  `(total - extraídos) / velocidad`, la misma fórmula que usa `ArchiveService.Eta` en la app web.
  Sin conteo (poco probable, solo si esa fase se cancela) se muestra igual "extraídos" y
  "Transcurrido" sin %/restante. `WorkAsyncInfo.MessageWidth` se subió de 340 (default) a 460
  para que el mensaje, más largo ahora, entre mejor en el cartel de progreso del host.

## Próximos pasos (en orden)

1. ~~Previsualización en grilla en `ExtraccionView`~~ — hecho.
2. ~~Generar `PluginPackage.png` real~~ — hecho.
3. ~~CI, cancelación, límite visible, combo de entidades, resumen de cambios en grilla,
   versión centralizada~~ — hecho (ver arriba).
4. ~~Confirmar con el usuario que el plugin ahora aparece en su XrmToolBox real~~ — hecho y
   verificado visualmente (ver bugs #3 y #4 arriba).
5. **PENDIENTE AHORA MISMO (2026-09-04): confirmar el resultado de la extracción real que el
   usuario acaba de correr** (enero–marzo 2026, entidad con volumen, ya con el fix de
   `RetrieveAuditDetailsRequest` desplegado — commit `89877d9` en adelante). Falta que el usuario
   confirme:
   - Si el conteo de filas del CSV exportado ahora se acerca a las 388.489 filas de la app web
     para el mismo universo (antes de este fix daba 63.713, sin fila por campo cambiado).
   - Si las columnas `Campo`/`ValorAnterior`/`ValorNuevo`/`ValorAnteriorLookup`/`ValorNuevoLookup`
     salieron pobladas (antes salían vacías).
   - Si hubo registros marcados `DetalleIncompleto = Sí` (agregado en `52c3f00`, más nuevo que
     la corrida que el usuario ya hizo — puede que ni siquiera esté en el build que él probó).
   El build más reciente (`1cb8af3`, botón Cancelar + progreso con tiempo transcurrido/restante)
   ya está compilado y copiado a `%AppData%\MscrmTools\XrmToolBox\Plugins` — falta que el usuario
   lo pruebe en una corrida nueva.
6. Probar el resto del flujo funcional contra Dataverse — seguir con
   `docs/README.md#cómo-probar-en-una-instancia-real`: combo de entidades, filtros de fecha,
   grilla, exportar a xlsx/csv/json, y "Validar" (buscar por `AuditId`, comparar contra
   Dataverse) — esta pantalla todavía no se probó contra un entorno real en esta sesión.
7. Empaquetar y distribuir internamente (el `.nupkg` empaqueta correctamente; falta el paso 5
   antes de considerar esto "distribuible").
8. Recolectar feedback de 2-3 usuarios antes de evaluar el checklist de certificación pública
   del Plugin Store.

## Convenciones

- Comentarios y textos de UI en español.
- Sin DI de MediatR, sin `IHostedService`, sin `appsettings.json` — el modelo de host de
  XrmToolBox no los necesita (una sola conexión, un solo usuario, sin jobs en background).
