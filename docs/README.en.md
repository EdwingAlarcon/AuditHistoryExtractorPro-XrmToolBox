# Audit History Extractor Pro — XrmToolBox Plugin

🌐 **English** | [Español](README.md)

XrmToolBox plugin to extract, export, and validate Dataverse audit history on demand, within
the single-user interactive host model that XrmToolBox provides.

## Current status

Builds end-to-end (`Core`, `Plugin`, `Core.Tests` — 0 errors) and packages correctly.
**It hasn't been tested against a real XrmToolBox/Dataverse instance yet** — see
[Testing against a real instance](#testing-against-a-real-instance) below.

- ✅ `AuditChangeDataParser` complete (parses `oldValues`/`newValues` from the `changedata`
  XML on the `audit` entity).
- ✅ "Extract" (filters → in-memory grid via `WorkAsync`) and "Export..." (grid → xlsx/csv/json,
  local I/O) split into two buttons in `ExtraccionView`.
- ✅ "Validate" (spot-check an `AuditId` against the current state in Dataverse) functional.
- ✅ Real SDK reference verified: the correct NuGet package is **`XrmToolBoxPackage`**
  (not `XrmToolBox.Extensibility`, which doesn't exist as an id). Pinned to `1.2023.10.67` —
  the last version that still ships `net462` binaries (as of `1.2025.7.71`, jul-2025, the
  package became `net48`-only; use an earlier version than that if you need net462).
- ✅ `.nuspec` corrected: it doesn't declare `XrmToolBox.Extensibility`/`XrmToolBoxPackage` as a
  NuGet dependency (the host already has it loaded, and that resolution mechanism doesn't apply
  in Tool Library) — it follows the real XrmToolBox convention: all packaged files live under
  `lib\net462\Plugins\`, including as loose files (not as "dependencies") the third-party
  assemblies the host doesn't ship (`ClosedXML` and its dependency tree, `CsvHelper`).
- ✅ `PluginPackage.png` is a real 32x32 icon (not a placeholder).
- ✅ Packaging verified with `nuget.exe pack` against the `.nuspec` (see below how to generate it).

## Scope (decided with the user)

- ✅ Extract audit history (filters by entity/date/operation) and export to Excel/CSV/JSON.
- ✅ Validate (spot-check) a specific `AuditId` against the current state in Dataverse.
- ❌ No security role reporting (excluded on explicit request).
- ❌ No persistence of history between sessions (all in-memory — see rationale below).
- ❌ No incremental extraction / 24x7 jobs (out of scope: XrmToolBox is a single-user
  interactive host, doesn't fit persistent background jobs).

### About persistence (or the lack thereof)

The plugin **doesn't save anything to disk between sessions** beyond its configuration
(`PluginSettings`, via the host's `SettingsHelper`). Every time you open XrmToolBox and use the
plugin, it starts blank: no "previous extraction history" visible in the UI. The file you
exported (Excel/CSV/JSON) does stay on your disk wherever you saved it — what doesn't persist
is the record *inside the plugin* of what you did. This was decided this way for the MVP
because it significantly reduces the bug surface (no state-file handling, backups, corruption)
and the value of "remembering between sessions" is low in a point-in-time tool. It's additive:
it can be added later without redesigning anything, if real users ask for it.

## Testing against a real instance

None of the above has been validated against a real XrmToolBox/Dataverse — this is the first
thing left to do, and it requires access this development environment doesn't have (an
installed XrmToolBox instance and a Dataverse environment). Steps:

### 1. Build in Release

```
dotnet build AuditHistoryExtractorPro.XrmToolBox.sln -c Release
```

(or from Visual Studio 2022: open the `.sln`, select the `Release` configuration, `Build Solution`).

### 2. Install into XrmToolBox: copy the DLLs into the `Plugins` folder

XrmToolBox's Tool Library doesn't always expose a visible "Install from disk" button (varies by
host version) — the method that works on any version, and is also what XrmToolBox's own
development guide recommends for local debugging, is copying the assemblies directly into the
host's `Plugins` folder:

1. Locate your XrmToolBox installation's `Plugins` folder — usually
   `%AppData%\MscrmTools\XrmToolBox\Plugins` (create it if it doesn't exist).
2. Copy there, from `src\AuditHistoryExtractorPro.XrmToolBox.Plugin\bin\Release\net462\`:
   - `AuditHistoryExtractorPro.XrmToolBox.Plugin.dll`
   - `AuditHistoryExtractorPro.XrmToolBox.Core.dll`
   - `ClosedXML.dll`, `CsvHelper.dll`, `DocumentFormat.OpenXml.dll`, `ExcelNumberFormat.dll`,
     `SixLabors.Fonts.dll`, `XLParser.dll`, `Irony.dll`, `System.IO.Packaging.dll` — these are
     the third-party dependencies the Excel/CSV export needs and that the host does **not**
     ship out of the box. Don't copy the rest of the DLLs in that folder (Dataverse SDK,
     `XrmToolBox.exe`, `McTools.Xrm.Connection*`, etc.) — those are already loaded by the host,
     and copying a different version can cause assembly-loading conflicts.
   - `src\AuditHistoryExtractorPro.XrmToolBox.Plugin\Resources\PluginPackage.png` (the icon).
3. Open (or restart) XrmToolBox. The **"Audit History Extractor Pro"** plugin should show up
   directly on the home screen, no need to go through Tool Library.

### 2b. Alternative: generate the `.nupkg` (if your version does have "Install from disk")

With `nuget.exe` (bundled with Visual Studio, or [download it](https://www.nuget.org/downloads)
— the `dotnet` CLI doesn't support packing a bare `.nuspec` directly):

```
nuget pack packaging\AuditHistoryExtractorPro.XrmToolBox.nuspec -OutputDirectory packaging\output
```

The `.nuspec` already follows the real XrmToolBox convention (everything under
`lib\net462\Plugins\`, no NuGet dependencies declared). Check that the generated `.nupkg`
contains, at least:
- `lib\net462\Plugins\AuditHistoryExtractorPro.XrmToolBox.Core.dll`
- `lib\net462\Plugins\AuditHistoryExtractorPro.XrmToolBox.Plugin.dll`
- `lib\net462\Plugins\PluginPackage.png`
- The `ClosedXML`/`CsvHelper` DLLs and their dependency tree listed in step 2.

(you can confirm this by renaming it to `.zip` and opening it — a `.nupkg` is a zip). If your
XrmToolBox has the option, it's **Tool Library** → the install-from-file button (exact name
varies by version) → point it to this `.nupkg`.

### 3. Connect and test the flow

1. Open the plugin against a connection to a test Dataverse environment (not production!).
2. **"Extract" tab:**
   - Enter the logical name of an entity that has auditing enabled and real history (e.g.
     `account`, `contact`, or a custom entity you know has audited changes).
   - Pick a date range that includes known activity.
   - Check at least "Update" under Operations (this is the most important case: it validates
     that `AuditChangeDataParser` is correctly reading your environment's real `changedata` XML
     — if the format your environment returns differs from the one assumed,
     `OldValues`/`NewValues` will come out empty even for real audit records).
   - Click "Extract" → confirm the grid fills up and the columns look reasonable (date, entity,
     action, user). If `OldValues`/`NewValues` come out empty on updates you know changed
     fields, that's the first bug to report.
   - Click "Export..." → pick a format and a path, confirm the file gets generated and opens
     correctly (Excel/CSV/JSON depending on the format).
3. **"Validate" tab:**
   - Grab a real `AuditId` (you can copy one from the "Extract" grid, or look one up in
     Dataverse's standard audit view).
   - Enter the corresponding entity and the `AuditId`, click "Validate against Dynamics".
   - Confirm the grid shows the compared fields and that the "differences found" message (or
     its absence) makes sense given whether the record changed after that audit or not.

### 4. What to report if something fails

- If the plugin doesn't load or throws an exception on open: check the XrmToolBox log
  (`%APPDATA%\MscrmTools\XrmToolBox\Logs` or similar) — it's probably a missing dependency
  (one of the third-party DLLs from step 2 is missing from the `Plugins` folder).
- If `OldValues`/`NewValues` come out empty: copy the raw `changedata` XML from a real audit
  record (you can get it via `RetrieveAuditDetailsRequest` from a quick script, or by
  inspecting the raw response) to compare against the format `AuditChangeDataParser` assumes
  (`<audit><oldValues><field value="..."/></oldValues><newValues>...`).
- Any other exception during "Extract"/"Validate": the error message is already shown in a
  `MessageBox` with the exception text — copy it as-is.

## Distribution

- **Now:** internal. See [Testing against a real instance](#testing-against-a-real-instance)
  to build + package + install.
- **Stated goal:** public release on the
  [XrmToolBox Plugin Store](https://www.xrmtoolbox.com/plugins/) once validated with real
  users. Before submitting for certification:
  - Register as an author on the store.
  - Review the certification checklist (no blocking dialogs on plugin load, exception handling
    that doesn't crash the host, complete icon and metadata, a clear license).
  - Consider the framework's standard optional telemetry (`LogUse`, already inherited from
    `PluginControlBase`) to know how many people use it.

## Structure

```
src/
  AuditHistoryExtractorPro.XrmToolBox.Core/     Pure logic (models, query builder, export, comparison)
  AuditHistoryExtractorPro.XrmToolBox.Plugin/   UserControl + integration with the XrmToolBox host
tests/
  AuditHistoryExtractorPro.XrmToolBox.Core.Tests/
packaging/
  AuditHistoryExtractorPro.XrmToolBox.nuspec
```

## Next steps (short roadmap)

1. ~~`AuditChangeDataParser` (parsing `changedata`)~~ — done.
2. ~~Result grid in `ExtraccionView`~~ — done.
3. ~~Real icon~~ — done.
4. **Manual testing against a real XrmToolBox + test Dataverse instance** — next real step,
   see the section above.
5. Packaging and internal distribution (local `.nupkg`) — the mechanism is already validated,
   step 4 is needed before considering this ready to hand out.
6. Collect feedback from 2-3 internal users before evaluating a public release.
