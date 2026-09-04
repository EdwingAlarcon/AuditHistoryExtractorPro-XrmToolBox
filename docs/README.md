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
  dependencia (el host ya la trae cargada), sí declara las dependencias de terceros reales
  que el host no trae (`ClosedXML`, `CsvHelper`, `Newtonsoft.Json`).
- ✅ `PluginPackage.png` es un ícono real de 32x32 (no un placeholder).
- ✅ Empaquetado verificado con `dotnet pack` sobre el `.nuspec` (ver más abajo cómo generarlo).

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

### 2. Generar el `.nupkg`

Si tenés `nuget.exe` (viene con Visual Studio, o [descargalo](https://www.nuget.org/downloads)):

```
nuget pack packaging\AuditHistoryExtractorPro.XrmToolBox.nuspec -OutputDirectory packaging\output
```

Si preferís `dotnet` CLI (no soporta empaquetar un `.nuspec` suelto directamente — hace falta
un `.csproj` mínimo que lo referencie vía `<NuspecFile>`; es el método que se usó para validar
el empaquetado en esta sesión, ver `CLAUDE.md`). Más simple: instalar `nuget.exe` y usar el
comando de arriba.

Verificá que el `.nupkg` generado (`packaging\output\AuditHistoryExtractorPro.XrmToolBox.0.1.0.nupkg`)
contenga, al menos:
- `lib\net462\AuditHistoryExtractorPro.XrmToolBox.Core.dll`
- `lib\net462\AuditHistoryExtractorPro.XrmToolBox.Plugin.dll`
- `content\PluginPackage.png`

(podés confirmarlo cambiándole la extensión a `.zip` y abriéndolo, un `.nupkg` es un zip).

### 3. Instalar en XrmToolBox

1. Abrí XrmToolBox.
2. Menú **Tool Library** (ícono de tienda) → pestaña **"My installed tools"** o similar según
   la versión → botón **"Install from disk"** (o desde el menú principal, buscar la opción
   equivalente — el nombre exacto varía un poco entre versiones del host).
3. Apuntá al `.nupkg` generado en el paso 2.
4. Reiniciá XrmToolBox si te lo pide.
5. Confirmá que "Audit History Extractor Pro" aparece en la lista de herramientas y que el
   ícono (lupa sobre círculo azul) se ve correctamente — si no aparece o el ícono sale roto,
   es la primera señal de que algo del empaquetado (paso 2) está mal.

### 4. Conectar y probar el flujo

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

### 5. Qué reportar si algo falla

- Si el plugin no carga o tira excepción al abrir: revisar el log de XrmToolBox
  (`%APPDATA%\MscrmTools\XrmToolBox\Logs` o similar) — probablemente sea un problema de
  dependencias faltantes (`ClosedXML`/`CsvHelper` no instalados junto al plugin).
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
