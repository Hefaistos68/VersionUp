# VersionUp for VS Code

VersionUp is a VS Code extension for incrementing project and manifest versions directly from the Explorer context menu.

It targets mixed .NET and related project layouts and supports common version-bearing files without requiring manual edits.

## Features

- Increment `Major`, `Minor`, `Build`, or `Revision` from the Explorer context menu.
- Update versions in SDK-style project files, manifests, package metadata, and installer definitions.
- Preserve version formatting rules, including resetting lower-order segments when a higher segment is incremented.
- Handle missing or invalid versions by falling back to `1.0.0`.

## Supported Files

- `.csproj`, `.fsproj`, `.vbproj`
- `directory.build.props`, `directory.build.targets`
- `AssemblyInfo.cs`, `AssemblyInfo.fs`, `AssemblyInfo.vb`
- `.nuspec`
- `package.appxmanifest`
- `package.json`
- `.rc`
- `source.extension.vsixmanifest`
- `.wxs`

## Commands

- `VersionUp: Increment Major`
- `VersionUp: Increment Minor`
- `VersionUp: Increment Build`
- `VersionUp: Increment Revision`
- `VersionUp: Set Version...`

## Usage

Right-click a supported file in the Explorer and choose the VersionUp command you want to run. The extension updates the version in place.

## Repository

Project repository: https://github.com/Hefaistos68/VersionUp

## Development

Build the VS Code extension:

```powershell
npm install
npm run compile
```

Package the VS Code extension:

```powershell
npm run package
```

From the repository root you can also run:

```powershell
.\build-vscode-extension.bat
```

## License

Licensed under PolyForm Noncommercial License 1.0.0. See [LICENSE](LICENSE).