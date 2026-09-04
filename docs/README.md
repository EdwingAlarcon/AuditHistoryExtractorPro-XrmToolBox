# Audit History Extractor Pro — Plugin XrmToolBox

🌐 [English](README.en.md) | **Español**

Plugin de XrmToolBox para extraer, exportar y validar el historial de auditoría de Dataverse
bajo demanda, dentro del modelo de host interactivo de un solo usuario que ofrece XrmToolBox.

## Estado actual

Compila de punta a punta (`Core`, `Plugin`, `Core.Tests` — 0 errores) y empaqueta
correctamente. **Se corrigieron los dos bugs que impedían que el plugin apareciera en
XrmToolBox** (el export de MEF mal armado, y la referencia a versiones de ensamblados del host
que no coincidían con una instalación real — ver más abajo), y se verificó con una composición
MEF real contra los ensamblados exactos de una instalación de XrmToolBox `1.2025.10.74`. Todavía
falta confirmar que el flujo funcional (Extraer/Validar) funciona contra Dataverse — ver
[Cómo probar en una instancia real](#cómo-probar-en-una-instancia-real) más abajo.

- ✅ `AuditChangeDataParser` completo (parsea `oldValues`/`newValues` del XML `changedata`
  de la entidad `audit`).
- ✅ "Extraer" (filtros → grilla en memoria vía `WorkAsync`, cancelable) y "Exportar..." (grilla
  → xlsx/csv/json, E/S local) separados en dos botones en `ExtraccionView`. El límite de
  registros (`MaxRegistros`) es un `NumericUpDown` visible en la UI (antes estaba fijo en 50.000
  y oculto), y la entidad se elige con autocompletado desde un combo poblado por metadata real
  (botón "Cargar entidades...", filtra solo entidades con auditoría habilitada) — sigue
  aceptando texto libre si no se carga el combo.
- ✅ "Validar" (spot-check de un `AuditId` contra el estado actual en Dataverse) funcional.
- ✅ `AuditRecord.ResumenCambios`: columna calculada ("campo: antes → después") en la grilla de
  "Extraer" para el caso más útil (Update), sin exponer los diccionarios crudos.
- ✅ Referencia al SDK real verificada y migrada a la versión exacta del host: el paquete
  NuGet correcto es **`XrmToolBoxPackage`** (no `XrmToolBox.Extensibility`, que no existe como
  id). Se probó primero fijándolo en `1.2023.10.67` (la última versión `net462`, desde antes de
  que el paquete pasara a `net48`-only en `1.2025.7.71`, jul-2025), pero al probar contra una
  instancia real de XrmToolBox (`1.2025.10.74`) el plugin no cargaba: .NET Framework rechaza
  referenciar una versión de un ensamblado firmado del host (`McTools.Xrm.Connection`) distinta
  a la que el host realmente trae, si no hay una redirección de binding — y no la hay para
  versiones tan viejas. Se migró todo el proyecto (`Core`, `Plugin`, `Core.Tests`) a **`net48`**
  con `XrmToolBoxPackage 1.2025.10.74` (la versión real verificada), agregando también una
  referencia directa a `MscrmTools.Xrm.Connection 1.2025.9.64` (la resolución transitiva de
  NuGet caía en una versión más vieja que la real). Verificado con una composición MEF real
  contra los ensamblados exactos de la instalación de XrmToolBox del usuario.
- ✅ `.nuspec` corregido: no declara `XrmToolBox.Extensibility`/`XrmToolBoxPackage` como
  dependencia NuGet (el host ya la trae cargada, y ese mecanismo de resolución no aplica en
  Tool Library) — sigue la convención real de XrmToolBox: todos los archivos empaquetados
  bajo `lib\net48\Plugins\`, incluyendo como archivos sueltos (no como "dependencias") los
  ensamblados de terceros que el host no trae (`ClosedXML` y su árbol de dependencias,
  `CsvHelper`).
- ✅ `PluginPackage.png` es un ícono real de 32x32 (no un placeholder).
- ✅ Empaquetado verificado con `nuget.exe pack` sobre el `.nuspec` (ver más abajo cómo generarlo).
- ✅ Bug real corregido: `AuditQueryBuilder` seteaba `TopCount` en la consulta y
  `PluginControl` también seteaba `PageInfo` — Dataverse no permite combinar ambos (falla en
  runtime). Se sacó `TopCount`; `MaxRegistros` corta la paginación del lado del cliente.
- ✅ Versión centralizada en `Directory.Build.props` (raíz del repo) — `Core.csproj` la hereda
  (antes no tenía `<Version>` propia y quedaba en `1.0.0.0` por default, desincronizada de
  `Plugin.dll`; ya corregido, ambas en `0.1.0.0`).
- ✅ CI en GitHub Actions (`.github/workflows/build.yml`): build + tests en cada push/PR.
- ✅ **Bug crítico corregido: el plugin no aparecía en XrmToolBox.** El `[Export(typeof(IXrmToolBoxPlugin))]`
  estaba puesto en `PluginControl` (el `UserControl`), que no implementa esa interfaz —
  implementa `IGitHubPlugin`/`IHelpPlugin`, y hereda de `PluginControlBase`, que implementa
  `IXrmToolBoxPluginControl` (otra interfaz). MEF descartaba el export en silencio, sin error
  visible. El patrón correcto usa dos clases: una descriptora chica (`Plugin.cs`, nuevo, hereda
  `PluginBase`) que lleva el `[Export]`/`[ExportMetadata]` y solo implementa
  `GetControl() => new PluginControl()`; el `UserControl` queda sin atributos Export. Verificado
  con una composición MEF real fuera de proceso: antes, 0 exports encontrados; después, 1,
  del tipo correcto.

## Alcance (decidido con el usuario)

- ✅ Extraer historial de auditoría (filtros por entidad/fecha/operación) y exportar a
  Excel/CSV/JSON.
- ✅ Validar (spot-check) un `AuditId` puntual contra el estado actual en Dataverse.
- ❌ Sin reporte de roles de seguridad (excluido a pedido explícito).
- ❌ Sin persistencia de historial entre sesiones (todo en memoria — ver justificación abajo).
- ❌ Sin extracción incremental / jobs 24x7 (fuera de alcance: XrmToolBox es un host
  interactivo de un solo usuario, no encaja con jobs persistentes en background).

### Sobre la persistencia (o la falta de ella)

El plugin **no guarda nada en disco entre sesiones** más allá de la configuración
(`PluginSettings`, vía `SettingsHelper` del host). Cada vez que abres XrmToolBox y usas el
plugin, arranca en blanco: sin "historial de extracciones anteriores" visible en la UI. El
archivo que exportaste (Excel/CSV/JSON) sí queda en tu disco donde lo guardaste — lo que no
persiste es el registro *dentro del plugin* de qué hiciste. Se decidió así para el MVP porque
reduce mucho la superficie de bugs (nada de manejo de archivos de estado, backups, corrupción)
y el valor de "recordar entre sesiones" es bajo en una herramienta de uso puntual. Es aditivo:
se puede incorporar más adelante sin rediseñar nada, si usuarios reales lo piden.

## Cómo probar en una instancia real

Nada de lo anterior se validó contra un XrmToolBox/Dataverse reales — esto es lo primero que
falta hacer, y requiere acceso que este entorno de desarrollo no tiene (una instancia de
XrmToolBox instalada y un entorno Dataverse). Pasos:

### 1. Compilar en Release

```
dotnet build AuditHistoryExtractorPro.XrmToolBox.sln -c Release
```

(o desde Visual Studio 2022: abrir la `.sln`, seleccionar configuración `Release`, `Build Solution`).

### 2. Instalar en XrmToolBox: copiar los archivos a la carpeta `Plugins`

La Tool Library de XrmToolBox no siempre expone un botón "Install from disk" visible (varía
según la versión del host) — el método que funciona en cualquier versión, incluso el que
recomienda la propia guía de desarrollo de XrmToolBox para debug local, es copiar los
ensamblados directamente a la carpeta `Plugins` del host.

Compilar en Release (paso 1) ya deja listos, en **`packaging\Plugins\`**, exactamente los 11
archivos que hacen falta — no hace falta rescatarlos entre los ~150 DLLs de
`bin\Release\net48\` (ahí están también todas las dependencias que ya trae el propio
XrmToolBox, que no hay que tocar). Dos formas de instalar desde ahí:

- **Automático:** `powershell -File packaging\install-local.ps1` — copia todo
  `packaging\Plugins\` a `%AppData%\MscrmTools\XrmToolBox\Plugins` (creando la carpeta si no
  existe). Si tu instalación de XrmToolBox no usa la ruta por defecto, pasala con
  `-XrmToolBoxPluginsPath "C:\ruta\que\corresponda\Plugins"`.
- **Manual:** copiá el contenido completo de `packaging\Plugins\` a
  `%AppData%\MscrmTools\XrmToolBox\Plugins` (creála si no existe).

Después, abrí (o reiniciá) XrmToolBox. El plugin **"Audit History Extractor Pro"** debería
aparecer directo en la pantalla principal, sin necesidad de pasar por Tool Library.

### 2b. Alternativa: generar el `.nupkg` (si tu versión sí tiene "Install from disk")

Con `nuget.exe` (viene con Visual Studio, o [descargalo](https://www.nuget.org/downloads) —
`dotnet` CLI no soporta empaquetar un `.nuspec` suelto directamente):

```
nuget pack packaging\AuditHistoryExtractorPro.XrmToolBox.nuspec -OutputDirectory packaging\output
```

El `.nuspec` ya sigue la convención real de XrmToolBox (todo bajo `lib\net48\Plugins\`, sin
declarar dependencias NuGet). Verificá que el `.nupkg` generado contenga, al menos:
- `lib\net48\Plugins\AuditHistoryExtractorPro.XrmToolBox.Core.dll`
- `lib\net48\Plugins\AuditHistoryExtractorPro.XrmToolBox.Plugin.dll`
- `lib\net48\Plugins\PluginPackage.png`
- Los DLLs de `ClosedXML`/`CsvHelper` y su árbol de dependencias listados en el paso 2.

(podés confirmarlo cambiándole la extensión a `.zip` y abriéndolo, un `.nupkg` es un zip). Si
tu XrmToolBox tiene la opción, es **Tool Library** → botón de instalar desde archivo (el
nombre exacto varía entre versiones) → apuntar a este `.nupkg`.

### 3. Conectar y probar el flujo

1. Abrí el plugin contra una conexión a un entorno Dataverse de prueba (¡no producción!).
2. **Pestaña "Extraer":**
   - Click "Cargar entidades..." → confirmá que el combo se llena con entidades reales (solo
     las que tienen auditoría habilitada) y que el autocompletado funciona al tipear. Si falla,
     revisá que el usuario conectado tenga permiso para `RetrieveAllEntitiesRequest` (privilegio
     de metadata, normalmente lo tiene cualquier rol con acceso de personalización).
   - Elegí una entidad con historial real (del combo, o tipeando el nombre lógico directo si
     preferís no cargar el combo).
   - Elegí un rango de fechas que incluya actividad conocida.
   - Marcá al menos "Update" en Operaciones (es el caso más importante: valida que
     `AuditChangeDataParser` esté leyendo bien el XML real de `changedata` — si el formato que
     devuelve tu entorno difiere del asumido, `OldValues`/`NewValues` van a salir vacíos incluso
     con registros de auditoría reales).
   - Click "Extraer" → confirmá que la grilla se llena, que la columna `ResumenCambios` muestra
     algo como `nombre: valor anterior → valor nuevo` en los updates, y que el resto de las
     columnas se ven razonables (fecha, entidad, acción, usuario). Si `ResumenCambios` queda
     vacío en updates que sabés que cambiaron campos, es el primer bug a reportar.
   - Con una entidad de mucho volumen, probá cancelar la extracción a mitad de camino (botón
     "Cancelar" que muestra el propio host mientras corre `WorkAsync`) y confirmá que la grilla
     igual se llena con los registros obtenidos hasta ese punto.
   - Bajá el "Máximo de registros" a un número chico (ej. 100) con una entidad de mucho volumen
     y confirmá que la extracción corta ahí en vez de seguir paginando.
   - Click "Exportar..." → elegí un formato y una ruta, confirmá que el archivo se genera y que
     abre bien (Excel/CSV/JSON según corresponda).
3. **Pestaña "Validar":**
   - Tomá un `AuditId` real (podés copiarlo desde la grilla de "Extraer", o buscarlo en la
     vista de auditoría estándar de Dataverse).
   - Ingresá la entidad correspondiente y el `AuditId`, click "Validar contra Dynamics".
   - Confirmá que la grilla muestra los campos comparados y que el mensaje de "diferencias
     detectadas" (o su ausencia) tiene sentido según si el registro cambió después de esa
     auditoría o no.

### 4. Qué reportar si algo falla

- Si el plugin no carga o tira excepción al abrir: revisar el log de XrmToolBox
  (`%APPDATA%\MscrmTools\XrmToolBox\Logs` o similar) — probablemente sea un problema de
  dependencias faltantes (falta alguno de los DLLs de terceros del paso 2 en la carpeta
  `Plugins`).
- Si `OldValues`/`NewValues` salen vacíos: copiar el XML crudo de `changedata` de un registro
  de auditoría real (se puede obtener con `RetrieveAuditDetailsRequest` desde un script rápido,
  o inspeccionando la respuesta cruda) para comparar contra el formato que asume
  `AuditChangeDataParser` (`<audit><oldValues><campo value="..."/></oldValues><newValues>...`).
- Cualquier otra excepción durante "Extraer"/"Validar": el mensaje de error ya se muestra en un
  `MessageBox` con el texto de la excepción — copiarlo tal cual.

## Distribución

- **Ahora:** interna. Ver [Cómo probar en una instancia real](#cómo-probar-en-una-instancia-real)
  para compilar + empaquetar + instalar.
- **Meta declarada:** publicación pública en el
  [XrmToolBox Plugin Store](https://www.xrmtoolbox.com/plugins/) una vez validado con
  usuarios reales. Para eso, antes de enviar a certificación:
  - Registrarse como autor en el store.
  - Revisar el checklist de certificación (sin diálogos bloqueantes al cargar el plugin,
    manejo de excepciones que no tumbe el host, ícono y metadata completos, licencia clara).
  - Considerar telemetría opcional estándar del framework (`LogUse`, ya heredado de
    `PluginControlBase`) para saber cuánta gente lo usa.

## Estructura

```
.github/workflows/
  build.yml                                     CI: build + tests en cada push/PR
src/
  AuditHistoryExtractorPro.XrmToolBox.Core/     Lógica pura (modelos, query builder, export, comparación)
  AuditHistoryExtractorPro.XrmToolBox.Plugin/   UserControl + integración con el host de XrmToolBox
tests/
  AuditHistoryExtractorPro.XrmToolBox.Core.Tests/
packaging/
  AuditHistoryExtractorPro.XrmToolBox.nuspec
  install-local.ps1                             Copia packaging\Plugins\ a tu XrmToolBox real
  Plugins/                                       (generado al compilar en Release, no versionado)
Directory.Build.props                            Versión compartida por Core y Plugin
```

## Próximos pasos (roadmap corto)

1. ~~`AuditChangeDataParser` (parseo de `changedata`)~~ — hecho.
2. ~~Grilla de resultados en `ExtraccionView`~~ — hecho.
3. ~~Ícono real~~ — hecho.
4. ~~CI, cancelación, límite visible, combo de entidades con metadata, resumen de cambios en
   grilla, versión centralizada~~ — hecho.
5. **Pruebas manuales en una instancia real de XrmToolBox + Dataverse de prueba** — próximo
   paso real, ver sección de arriba.
6. Empaquetado y distribución interna (`.nupkg` local) — ya validado el mecanismo, falta el
   paso 5 antes de considerarlo listo para repartir.
7. Recolectar feedback de 2-3 usuarios internos antes de evaluar publicación pública.
