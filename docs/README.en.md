# Audit History Extractor Pro — XrmToolBox Plugin

🌐 **English** | [Español](README.md)

XrmToolBox plugin to extract, export, and validate Dataverse audit history on demand, within
the single-user interactive host model that XrmToolBox provides.

## Current status

Builds end-to-end (`Core`, `Plugin`, `Core.Tests` — 0 errors) and packages correctly.
**Both bugs that kept the plugin from showing up in XrmToolBox are fixed** (a malformed MEF
export, and referencing host assembly versions that didn't match a real install — see below),
verified with a real MEF composition against the exact assemblies of a `1.2025.10.74`
XrmToolBox install. Still pending confirmation that the functional flow (Extract/Validate)
works against Dataverse — see
[Testing against a real instance](#testing-against-a-real-instance) below.

- ✅ `AuditChangeDataParser` complete (parses `oldValues`/`newValues` from the `changedata`
  XML on the `audit` entity).
- ✅ "Extract" (filters → in-memory grid via a cancelable `WorkAsync`) and "Export..." (grid →
  xlsx/csv/json, local I/O) split into two buttons in `ExtraccionView`. The record limit
  (`MaxRegistros`) is a visible `NumericUpDown` in the UI (previously fixed at 50,000 and
  hidden), and the entity is picked from an autocomplete combo populated from real metadata
  (a "Load entities..." button, filtered to only audit-enabled entities) — free text is still
  accepted if the combo isn't loaded.
- ✅ "Validate" (spot-check an `AuditId` against the current state in Dataverse) functional.
- ✅ `AuditRecord.ResumenCambios`: a computed column ("field: before → after") on the "Extract"
  grid for the most useful case (Update), without exposing the raw dictionaries.
- ✅ Real SDK reference verified and migrated to the exact host version: the correct NuGet
  package is **`XrmToolBoxPackage`** (not `XrmToolBox.Extensibility`, which doesn't exist as an
  id). First tried pinned to `1.2023.10.67` (the last `net462` version, before the package went
  `net48`-only in `1.2025.7.71`, jul-2025), but testing against a real XrmToolBox instance
  (`1.2025.10.74`) the plugin failed to load: .NET Framework refuses to reference a different
  version of a strongly-signed host assembly (`McTools.Xrm.Connection`) than what the host
  actually ships, without a binding redirect — and none exists for versions that old. Migrated
  the whole project (`Core`, `Plugin`, `Core.Tests`) to **`net48`** with
  `XrmToolBoxPackage 1.2025.10.74` (the verified real version), also adding a direct reference
  to `MscrmTools.Xrm.Connection 1.2025.9.64` (NuGet's transitive resolution landed on an older
  version than the real one). Verified with a real MEF composition against the exact assemblies
  of the user's XrmToolBox install.
- ✅ `.nuspec` corrected: it doesn't declare `XrmToolBox.Extensibility`/`XrmToolBoxPackage` as a
  NuGet dependency (the host already has it loaded, and that resolution mechanism doesn't apply
  in Tool Library) — it follows the real XrmToolBox convention: all packaged files live under
  `lib\net48\Plugins\`, including as loose files (not as "dependencies") the third-party
  assemblies the host doesn't ship (`ClosedXML` and its dependency tree, `CsvHelper`).
- ✅ `PluginPackage.png` is a real 32x32 icon (not a placeholder).
- ✅ Packaging verified with `nuget.exe pack` against the `.nuspec` (see below how to generate it).
- ✅ Real bug fixed: `AuditQueryBuilder` was setting `TopCount` on the query, and
  `PluginControl` was also setting `PageInfo` — Dataverse doesn't allow combining both (fails
  at runtime). `TopCount` was removed; `MaxRegistros` now caps the client-side paging loop.
- ✅ Version centralized in `Directory.Build.props` (repo root) — `Core.csproj` inherits it
  (previously it had no `<Version>` of its own and defaulted to `1.0.0.0`, out of sync with
  `Plugin.dll`; now fixed, both at `0.1.0.0`).
- ✅ CI on GitHub Actions (`.github/workflows/build.yml`): build + tests on every push/PR.
- ✅ **Critical bug fixed: the plugin didn't show up in XrmToolBox.** The
  `[Export(typeof(IXrmToolBoxPlugin))]` attribute was on `PluginControl` (the `UserControl`),
  which doesn't implement that interface — it implements `IGitHubPlugin`/`IHelpPlugin`, and
  inherits `PluginControlBase`, which implements `IXrmToolBoxPluginControl` (a different
  interface). MEF silently discarded the export, no visible error. The correct pattern uses two
  classes: a small descriptor (`Plugin.cs`, new file, inherits `PluginBase`) carrying the
  `[Export]`/`[ExportMetadata]` attributes and only implementing
  `GetControl() => new PluginControl()`; the `UserControl` keeps no Export attributes. Verified
  with a real out-of-process MEF composition: before, 0 exports found; after, exactly 1, of the
  right type.

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

### 2. Install into XrmToolBox: copy the files into the `Plugins` folder

XrmToolBox's Tool Library doesn't always expose a visible "Install from disk" button (varies by
host version) — the method that works on any version, and is also what XrmToolBox's own
development guide recommends for local debugging, is copying the assemblies directly into the
host's `Plugins` folder.

Building in Release (step 1) already leaves the exact 11 files you need ready in
**`packaging\Plugins\`** — no need to fish them out of the ~150 DLLs in
`bin\Release\net48\` (which also holds everything XrmToolBox itself already ships, that you
must NOT touch). Two ways to install from there:

- **Automatic:** `powershell -File packaging\install-local.ps1` — copies everything in
  `packaging\Plugins\` into `%AppData%\MscrmTools\XrmToolBox\Plugins` (creating the folder if
  it doesn't exist). If your XrmToolBox install doesn't use the default path, pass
  `-XrmToolBoxPluginsPath "C:\path\to\your\Plugins"`.
- **Manual:** copy the full contents of `packaging\Plugins\` into
  `%AppData%\MscrmTools\XrmToolBox\Plugins` (create it if it doesn't exist).

Then open (or restart) XrmToolBox. The **"Audit History Extractor Pro"** plugin should show up
directly on the home screen, no need to go through Tool Library.

### 2b. Alternative: generate the `.nupkg` (if your version does have "Install from disk")

With `nuget.exe` (bundled with Visual Studio, or [download it](https://www.nuget.org/downloads)
— the `dotnet` CLI doesn't support packing a bare `.nuspec` directly):

```
nuget pack packaging\AuditHistoryExtractorPro.XrmToolBox.nuspec -OutputDirectory packaging\output
```

The `.nuspec` already follows the real XrmToolBox convention (everything under
`lib\net48\Plugins\`, no NuGet dependencies declared). Check that the generated `.nupkg`
contains, at least:
- `lib\net48\Plugins\AuditHistoryExtractorPro.XrmToolBox.Core.dll`
- `lib\net48\Plugins\AuditHistoryExtractorPro.XrmToolBox.Plugin.dll`
- `lib\net48\Plugins\PluginPackage.png`
- The `ClosedXML`/`CsvHelper` DLLs and their dependency tree listed in step 2.

(you can confirm this by renaming it to `.zip` and opening it — a `.nupkg` is a zip). If your
XrmToolBox has the option, it's **Tool Library** → the install-from-file button (exact name
varies by version) → point it to this `.nupkg`.

### 3. Connect and test the flow

1. Open the plugin against a connection to a test Dataverse environment (not production!).
2. **"Extract" tab:**
   - Click "Load entities..." → confirm the combo fills with real entities (only the ones with
     auditing enabled) and that autocomplete works while typing. If it fails, check that the
     connected user has permission for `RetrieveAllEntitiesRequest` (a metadata privilege,
     normally included in any role with customization access).
   - Pick an entity with real history (from the combo, or by typing the logical name directly
     if you'd rather skip loading the combo).
   - Pick a date range that includes known activity.
   - Check at least "Update" under Operations (this is the most important case: it validates
     that `AuditChangeDataParser` is correctly reading your environment's real `changedata` XML
     — if the format your environment returns differs from the one assumed,
     `OldValues`/`NewValues` will come out empty even for real audit records).
   - Click "Extract" → confirm the grid fills up, the `ResumenCambios` column shows something
     like `field: old value → new value` on updates, and the rest of the columns look reasonable
     (date, entity, action, user). If `ResumenCambios` comes out empty on updates you know
     changed fields, that's the first bug to report.
   - With a high-volume entity, try cancelling the extraction partway through (the "Cancel"
     button the host itself shows while `WorkAsync` runs) and confirm the grid still fills with
     the records obtained up to that point.
   - Lower "Max records" to a small number (e.g. 100) with a high-volume entity and confirm the
     extraction stops there instead of continuing to page.
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
.github/workflows/
  build.yml                                     CI: build + tests on every push/PR
src/
  AuditHistoryExtractorPro.XrmToolBox.Core/     Pure logic (models, query builder, export, comparison)
  AuditHistoryExtractorPro.XrmToolBox.Plugin/   UserControl + integration with the XrmToolBox host
tests/
  AuditHistoryExtractorPro.XrmToolBox.Core.Tests/
packaging/
  AuditHistoryExtractorPro.XrmToolBox.nuspec
  install-local.ps1                             Copies packaging\Plugins\ into your real XrmToolBox
  Plugins/                                       (generated on Release build, not source-controlled)
Directory.Build.props                            Version shared by Core and Plugin
```

## Next steps (short roadmap)

1. ~~`AuditChangeDataParser` (parsing `changedata`)~~ — done.
2. ~~Result grid in `ExtraccionView`~~ — done.
3. ~~Real icon~~ — done.
4. ~~CI, cancellation, visible record limit, metadata-backed entity combo, changes-summary
   grid column, centralized version~~ — done.
5. **Manual testing against a real XrmToolBox + test Dataverse instance** — next real step,
   see the section above.
6. Packaging and internal distribution (local `.nupkg`) — the mechanism is already validated,
   step 5 is needed before considering this ready to hand out.
7. Collect feedback from 2-3 internal users before evaluating a public release.
