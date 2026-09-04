# Audit History Extractor Pro — Plugin XrmToolBox

Plugin de XrmToolBox para extraer, exportar y validar el historial de auditoría de Dataverse
bajo demanda, dentro del modelo de host interactivo de un solo usuario que ofrece XrmToolBox.

## Estado actual

Compila de punta a punta (`Core`, `Plugin`, `Core.Tests` — 0 errores) y empaqueta
correctamente. **Todavía no se probó contra una instancia real de XrmToolBox/Dataverse** — ver
[Cómo probar en una instancia real](#cómo-probar-en-una-instancia-real) más abajo.

- ✅ `AuditChangeDataParser` completo (parsea `oldValues`/`newValues` del XML `changedata`
  de la entidad `audit`).
- ✅ "Extraer" (filtros → grilla en memoria vía `WorkAsync`) y "Exportar..." (grilla → xlsx/csv/json,
  E/S local) separados en dos botones en `ExtraccionView`.
- ✅ "Validar" (spot-check de un `AuditId` contra el estado actual en Dataverse) funcional.
- ✅ Referencia al SDK real verificada: el paquete NuGet correcto es **`XrmToolBoxPackage`**
  (no `XrmToolBox.Extensibility`, que no existe como id). Se fijó en `1.2023.10.67` — la
  última versión que todavía publica binarios `net462` (desde `1.2025.7.71`, jul-2025, el
  paquete pasó a `net48` únicamente; usar una versión anterior a esa si se necesita net462).
- ✅ `.nuspec` corregido: no declara `XrmToolBox.Extensibility`/`XrmToolBoxPackage` como
  dependencia NuGet (el host ya la trae cargada, y ese mecanismo de resolución no aplica en
  Tool Library) — sigue la convención real de XrmToolBox: todos los archivos empaquetados
  bajo `lib\net462\Plugins\`, incluyendo como archivos sueltos (no como "dependencias") los
  ensamblados de terceros que el host no trae (`ClosedXML` y su árbol de dependencias,
  `CsvHelper`).
- ✅ `PluginPackage.png` es un ícono real de 32x32 (no un placeholder).
- ✅ Empaquetado verificado con `nuget.exe pack` sobre el `.nuspec` (ver más abajo cómo generarlo).

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

### 2. Instalar en XrmToolBox: copiar los DLLs a la carpeta `Plugins`

La Tool Library de XrmToolBox no siempre expone un botón "Install from disk" visible (varía
según la versión del host) — el método que funciona en cualquier versión, incluso el que
recomienda la propia guía de desarrollo de XrmToolBox para debug local, es copiar los
ensamblados directamente a la carpeta `Plugins` del host:

1. Ubicá la carpeta `Plugins` de tu instalación de XrmToolBox — normalmente
   `%AppData%\MscrmTools\XrmToolBox\Plugins` (creála si no existe).
2. Copiá ahí, desde `src\AuditHistoryExtractorPro.XrmToolBox.Plugin\bin\Release\net462\`:
   - `AuditHistoryExtractorPro.XrmToolBox.Plugin.dll`
   - `AuditHistoryExtractorPro.XrmToolBox.Core.dll`
   - `ClosedXML.dll`, `CsvHelper.dll`, `DocumentFormat.OpenXml.dll`, `ExcelNumberFormat.dll`,
     `SixLabors.Fonts.dll`, `XLParser.dll`, `Irony.dll`, `System.IO.Packaging.dll` — son las
     dependencias de terceros que necesita la exportación a Excel/CSV y que el host **no**
     trae de fábrica. No copiar el resto de las DLLs de esa carpeta (SDK de Dataverse,
     `XrmToolBox.exe`, `McTools.Xrm.Connection*`, etc.) — esas ya están cargadas por el host
     y copiar una versión distinta puede generar conflictos de assembly loading.
   - `src\AuditHistoryExtractorPro.XrmToolBox.Plugin\Resources\PluginPackage.png` (el ícono).
3. Abrí (o reiniciá) XrmToolBox. El plugin **"Audit History Extractor Pro"** debería aparecer
   directo en la pantalla principal, sin necesidad de pasar por Tool Library.

### 2b. Alternativa: generar el `.nupkg` (si tu versión sí tiene "Install from disk")

Con `nuget.exe` (viene con Visual Studio, o [descargalo](https://www.nuget.org/downloads) —
`dotnet` CLI no soporta empaquetar un `.nuspec` suelto directamente):

```
nuget pack packaging\AuditHistoryExtractorPro.XrmToolBox.nuspec -OutputDirectory packaging\output
```

El `.nuspec` ya sigue la convención real de XrmToolBox (todo bajo `lib\net462\Plugins\`, sin
declarar dependencias NuGet). Verificá que el `.nupkg` generado contenga, al menos:
- `lib\net462\Plugins\AuditHistoryExtractorPro.XrmToolBox.Core.dll`
- `lib\net462\Plugins\AuditHistoryExtractorPro.XrmToolBox.Plugin.dll`
- `lib\net462\Plugins\PluginPackage.png`
- Los DLLs de `ClosedXML`/`CsvHelper` y su árbol de dependencias listados en el paso 2.

(podés confirmarlo cambiándole la extensión a `.zip` y abriéndolo, un `.nupkg` es un zip). Si
tu XrmToolBox tiene la opción, es **Tool Library** → botón de instalar desde archivo (el
nombre exacto varía entre versiones) → apuntar a este `.nupkg`.

### 3. Conectar y probar el flujo

1. Abrí el plugin contra una conexión a un entorno Dataverse de prueba (¡no producción!).
2. **Pestaña "Extraer":**
   - Ingresá un nombre lógico de entidad que tenga auditoría habilitada y con historial real
     (ej. `account`, `contact`, o alguna entidad custom que sepas que tiene cambios auditados).
   - Elegí un rango de fechas que incluya actividad conocida.
   - Marcá al menos "Update" en Operaciones (es el caso más importante: valida que
     `AuditChangeDataParser` esté leyendo bien el XML real de `changedata` — si el formato que
     devuelve tu entorno difiere del asumido, `OldValues`/`NewValues` van a salir vacíos incluso
     con registros de auditoría reales).
   - Click "Extraer" → confirmá que la grilla se llena y que las columnas se ven razonables
     (fecha, entidad, acción, usuario). Si `OldValues`/`NewValues` quedan vacíos en updates que
     sabés que cambiaron campos, es el primer bug a reportar.
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
src/
  AuditHistoryExtractorPro.XrmToolBox.Core/     Lógica pura (modelos, query builder, export, comparación)
  AuditHistoryExtractorPro.XrmToolBox.Plugin/   UserControl + integración con el host de XrmToolBox
tests/
  AuditHistoryExtractorPro.XrmToolBox.Core.Tests/
packaging/
  AuditHistoryExtractorPro.XrmToolBox.nuspec
```

## Próximos pasos (roadmap corto)

1. ~~`AuditChangeDataParser` (parseo de `changedata`)~~ — hecho.
2. ~~Grilla de resultados en `ExtraccionView`~~ — hecho.
3. ~~Ícono real~~ — hecho.
4. **Pruebas manuales en una instancia real de XrmToolBox + Dataverse de prueba** — próximo
   paso real, ver sección de arriba.
5. Empaquetado y distribución interna (`.nupkg` local) — ya validado el mecanismo, falta el
   paso 4 antes de considerarlo listo para repartir.
6. Recolectar feedback de 2-3 usuarios internos antes de evaluar publicación pública.
