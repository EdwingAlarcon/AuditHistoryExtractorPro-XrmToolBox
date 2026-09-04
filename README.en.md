# Audit History Extractor Pro — XrmToolBox Plugin

🌐 **English** | [Español](README.md)

[![Build and Test](https://github.com/EdwingAlarcon/AuditHistoryExtractorPro-XrmToolBox/actions/workflows/build.yml/badge.svg)](https://github.com/EdwingAlarcon/AuditHistoryExtractorPro-XrmToolBox/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6.2-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![XrmToolBox](https://img.shields.io/badge/XrmToolBox-Plugin-0072C6.svg)](https://www.xrmtoolbox.com/)

[XrmToolBox](https://www.xrmtoolbox.com/) plugin to **extract, export, and validate Dataverse
audit history** on demand, without deploying a dedicated app.

> The rest of the repository (code comments, UI text) is in Spanish, but the deeper technical
> doc is also available in English: [`docs/README.en.md`](docs/README.en.md).

## Features

- **Extract**: filter audit history by entity (with autocomplete from real metadata), date
  range, and operation type (Create/Update/Delete/Access), preview it in a grid (with a
  readable summary of which fields changed), cancel a long-running extraction, and export to
  Excel, CSV, or JSON.
- **Validate**: compare a specific audit record (by `AuditId`) against the current state of
  the record in Dataverse — useful to check whether an audited value is still current.

Does not include security role reporting or background/incremental extraction jobs; it also
doesn't persist history between sessions — the plugin starts blank every time, though exported
files do remain on disk. See
[`docs/README.en.md`](docs/README.en.md#scope-decided-with-the-user) for the detail and
rationale behind each decision.

## Requirements

- [XrmToolBox](https://www.xrmtoolbox.com/) installed.
- A connection configured in XrmToolBox to a Dataverse / Dynamics 365 environment.
- .NET Framework 4.6.2 or later (already bundled with XrmToolBox).

## Installation

This plugin is **not yet published on the XrmToolBox Plugin Store** (see
[project status](docs/README.en.md#current-status)) — it needs to be installed manually.
XrmToolBox's Tool Library doesn't always show a visible "Install from disk" option depending on
the host version; the method that works on any version is copying the files directly into
XrmToolBox's `Plugins` folder:

1. Build the project in `Release` (see
   [building from source](#building-from-source-for-developers) below) — that leaves the exact
   11 files you need ready in `packaging\Plugins\` (no need to fish them out of the ~150 DLLs
   in `bin\Release\net462\`).
2. Copy those files into `%AppData%\MscrmTools\XrmToolBox\Plugins\` (create the folder if it
   doesn't exist) — or just run `powershell -File packaging\install-local.ps1`, which does it
   for you.
3. Open (or restart) XrmToolBox — **"Audit History Extractor Pro"** should show up directly on
   the home screen, no need to go through Tool Library.

If your XrmToolBox version does have "Install from disk" (Tool Library → the corresponding
button, name varies by version), you can also generate the `.nupkg` (`nuget pack
packaging\AuditHistoryExtractorPro.XrmToolBox.nuspec -OutputDirectory packaging\output`) and
point it there — the `.nuspec` already follows the `lib\net462\Plugins\` structure that
convention requires.

## Building from source (for developers)

```bash
git clone https://github.com/EdwingAlarcon/AuditHistoryExtractorPro-XrmToolBox.git
cd AuditHistoryExtractorPro-XrmToolBox
dotnet build AuditHistoryExtractorPro.XrmToolBox.sln -c Release
```

Building in `Release` leaves `packaging\Plugins\` ready with the exact files to copy per step 2
above.

## Repository structure

```
src/
  AuditHistoryExtractorPro.XrmToolBox.Core/     Pure logic (models, query builder, export, comparison)
  AuditHistoryExtractorPro.XrmToolBox.Plugin/   UserControl + integration with the XrmToolBox host
tests/
  AuditHistoryExtractorPro.XrmToolBox.Core.Tests/
packaging/
  AuditHistoryExtractorPro.XrmToolBox.nuspec
  install-local.ps1                             Copies packaging\Plugins\ into your real XrmToolBox
  Plugins/                                       (generated on Release build, not source-controlled)
```

## Project status

Builds end-to-end and packaging is verified, but **it hasn't been tested against a real
XrmToolBox/Dataverse instance yet**. See [`docs/README.en.md`](docs/README.en.md) for the
detailed status and short-term roadmap.

## Contributing

Issues and pull requests are welcome. This is currently an internally-distributed project on
its way to evaluating a Plugin Store release — see the roadmap in
[`docs/README.en.md`](docs/README.en.md#next-steps-short-roadmap).

## License

[MIT](LICENSE) © Edwing Alarcón
