# Working in this repo

## Reference projects

When researching how to shim a WP7 type or theme, the closest external prior
art is **yangzhongke/Windows-Phone-Emulator**
(https://github.com/yangzhongke/Windows-Phone-Emulator). It targets Silverlight
4 (not WPF/Avalonia) — its C# is a real prior implementation of WP toolkit
controls, but its Style / Setter / ControlTemplate parsing all defers to the
Silverlight XAML parser, so it's not transplantable for our XAML reader.

Worth lifting:
- `Microsoft.Phone/ThemeResources.xaml` + `Microsoft.Phone/System.Windows.xaml` —
  the WP7 typography + default control templates. Reference values for
  PhoneText*Style font sizes, FontFamily, brushes. Mirrored in our
  `PhoneTheme.cs`.
- `Microsoft.Phone.Controls/Panorama.cs` + `PanoramaItem.cs` + `Pivot.cs` —
  substantive (600+ LoC) reference for the swipe / parallax / layer logic.
- `Microsoft.Phone.Controls.Toolkit/Transitions/*.cs` — turnstile / swivel /
  slide transition state machines.

Not transplantable: the `Gestures/GestureHelper.cs` is a thin singleton
delegator with no inertia — we have to write our own pointer-events-to-gesture
pipeline on Avalonia.

Missing from that fork entirely: `LongListSelector`, `WrapPanel`,
`PhoneTextBox`, `PerformanceProgressBar`, `GestureService`/`GestureListener`,
plus `ButtonStyleLight`, `DarkThemePanoramaStyle`, and `PhoneApplicationPageStyle`
(app-supplied, not in the system theme).

## Running & build workflow

The user normally builds and runs from **Rider** (system .NET 10 MSBuild). The
CLI `dotnet build` path is for verifying small edits — it hits known
limitations on this machine and should not be the primary build mechanism.

### How runs actually happen

- **Build**: the user clicks build/run in Rider, which builds `WPR.UI.Desktop`
  for `net8.0-windows10.0.17763.0` and pulls in the rest by project reference.
- **Run**: `WPR.UI.Desktop` is the entry point. The UI lists installed games;
  picking one launches via `SilverlightLauncher.LaunchAsync` or
  `XnaLauncher.LaunchAsync`.
- **Install pipeline** (per game, runs once when the user clicks Install on a
  newly-discovered `.xap`/XNA folder):
  1. `LibraryScanner` discovers the package.
  2. `ApplicationInstaller` unpacks to `%LocalAppData%\WPR\Apps\<ProductId>`.
  3. `ApplicationPatcher.PatchDll` rewrites every `*.dll` in the install dir:
     Silverlight / WP / XNA types redirected to our shims (`Patches` dict),
     a handful of CLR methods redirected (`MemberPatches` dict).
  4. `XnaAchievementSeeder.SeedAsync` populates the SQLite achievements DB.
- **Game launch loads the patched DLLs.** If the patcher table changes, every
  game installed before the change still has the old IL — it must be
  **reinstalled** to pick up new redirects. The user knows this; I should say
  "reinstall <game>" rather than "rebuild" when the fix lands in
  `ApplicationPatcher.cs`.

### When I touch a shim type

Two distinct rebuild paths depending on what changed:

1. **Shim implementation only** (`WPR.SilverlightCompability/*.cs`,
   `WPR.WindowsCompability/*.cs`, `WPR.XnaCompability/*.cs`, GamerServices,
   etc.): just rebuild — installed games will pick up the new behaviour on
   next launch because they reference the shim assembly, not a snapshot of it.
   **No reinstall needed.**
2. **Patcher table change** (`ApplicationPatcher.cs` — adding entries to
   `Patches` / `MemberPatches`, changing target types): rebuild **and**
   **reinstall the affected games**. The IL was rewritten at install time;
   adding a new redirect now does nothing to already-installed `.dll`s.

The common "add a new shim type" task is **both**: add the shim class, add the
patcher entry, rebuild, reinstall the affected game.

### Shim file layout (`WPR.SilverlightCompability`)

This project's source tree mirrors the real Silverlight / Windows Phone
namespace hierarchy as directories — **one C# class per file, file path
matches where the type lives upstream**. The C# `namespace` declaration in
every file stays `WPR.SilverlightCompability` regardless of where on disk the
file lives — the directory structure is pure organisation, the assembly is one
flat DLL, and the patcher target paths (`NewNamespace` in
`ApplicationPatcher.cs`) refer to that flat namespace.

Examples:
- `System.Windows.Shapes.Rectangle` → `System/Windows/Shapes/Rectangle.cs`
- `System.Windows.Controls.Primitives.Popup` → `System/Windows/Controls/Primitives/Popup.cs`
- `System.Windows.Media.Animation.Storyboard` → `System/Windows/Media/Animation/Storyboard.cs`
- `Microsoft.Phone.Shell.PhoneApplicationService` → `Microsoft/Phone/Shell/PhoneApplicationService.cs`
- `System.ComponentModel.DesignerProperties` → `System/ComponentModel/DesignerProperties.cs`

When adding a new shim type, look up the real upstream namespace (usually in
the type's MSDN docs or a Silverlight 4 reference assembly), create the
mirror directory if it doesn't exist, and drop the class file in it. The
filename is the type name verbatim. Keep the doc comment that says
`/// Shim for <c>System.X.Y.TypeName</c>.` — it's the canonical record of
which upstream type the file shadows, and tooling can grep for it.

Files at the project root are **not** type shims — they're WPR-internal
runtime/helper code that doesn't shadow any upstream type:
- Renderers (`SilverlightRenderer.cs`, `D3D11ImageSplashRenderer.cs`, …)
- Pointer-to-gesture bridge (`Gestures.cs`, `PanoramaState.cs`,
  `PanoramaStateTable.cs`, `PanoramaSelectedItemSync.cs`)
- XAML helpers (`XamlTypeConverter.cs`, `MarkupExtensionParser.cs`)
- Hosting glue (`HostContext.cs`, `HitTester.cs`, `BingWallpaper.cs`,
  `ResourceBundleReader.cs`, `GameMakerAssetExtractor.cs`)
- Theme constants (`PhoneTheme.cs`)
- `AssemblyInfo.cs`

If you're adding something that *is* a shim, it goes in the namespace tree.
If you're adding new hosting logic, it stays at the root.

`WPR.WindowsCompability` and `WPR.XnaCompability` are still flat — the
mirror-tree convention has only been applied to `WPR.SilverlightCompability`
so far. Apply the same pattern when you next touch those projects, but don't
make a separate pass just to reorganise them.

### CLI build shortcuts that work

Full solution builds hit `NU1202` on `Avalonia.Android` (workload-version
mismatch — see Environment notes). When verifying a small edit:

```
dotnet build <project>.csproj -c Debug -f net8.0-windows10.0.17763.0 \
    -maxcpucount:1 -nodeReuse:false --nologo
```

- `-f net8.0-windows10.0.17763.0` skips the broken Android leg.
- `-maxcpucount:1 -nodeReuse:false` avoids the parallel-build CS0006
  "metadata file not found" race that hits in MSBuild's default settings.
- Build leaf projects first (e.g. `WPR.SilverlightCompability`) — they have
  no project deps that need staging and give the fastest yes/no on a shim edit.
- Building `WPR` / `WPR.UI` / `WPR.UI.Desktop` from CLI often fails with
  spurious "namespace not found" cascades because the CLI doesn't restage
  transitive project references the way Rider does. **Treat a successful
  leaf-project build as sufficient validation; defer the full chain to
  Rider.** If the user reports a runtime error after their next IDE build,
  that's the real signal.

### Verifying a patcher entry took effect

If the user says "still the same error after reinstall," check whether
`ApplicationPatcher.PatchDll` actually wrote a `.dll.original` sibling next to
the user assembly in the per-game install dir (its path is built in
`ApplicationInstaller.CreateApplicationEntryAndExtract`; ask the user for the
exact folder once and stash it for the session). If the `.original` is older
than the patcher source changes (or missing entirely), the install didn't
re-run — the user may have hit "launch" instead of "reinstall," or the install
dir wasn't cleared.

## Cleanup at end of session

Before finishing a task, clean up anything created for diagnosis/verification
that isn't part of the change itself:

- **Log files**: anything I wrote into the repo root or under `Src/` for build
  capture (e.g. `build_*.log`, `restore_*.log`, `install_*.log`). Leave
  pre-existing files alone — only remove ones I authored this session.
- **Stray `global.json` / scratch csprojs**: anything I created purely to
  probe SDK behavior should be removed before declaring done.
- **Build processes**: check for orphaned `dotnet` / `MSBuild` /
  `VBCSCompiler` instances I spawned (`Get-Process` + match command line).
  Do **not** kill processes belonging to Rider (`ReSharperHost`,
  `JetBrains.*`) or Visual Studio (`devenv`) — those are the user's IDE.
- **`obj/` and `bin/`**: leave these. They're normal incremental-build
  artifacts; removing them would force a rebuild the user didn't ask for.

## Environment notes (as of 2026-05-11)

- System .NET SDK: `C:\Program Files\dotnet` — **.NET 10.0.203**, Android
  workload `36.1.43/10.0.100` (no .NET 8 manifest). Rider's MSBuild uses
  this one.
- User-local .NET SDK: `C:\Users\BenSl\.dotnet` — **.NET 8.0.420**, Android
  workload `34.0.154`. **Not visible to Rider** — don't pin to it via
  `global.json` or builds will fail with `NETSDK1141`.
- Android SDK platforms installed at `C:\Program Files (x86)\Android\android-sdk\platforms`:
  **android-35**, **android-36**. **No android-34.**
- Consequence: every `net8.0-android*` TFM resolves to `TargetPlatformVersion=21.0`
  on the system SDK (the workload has no net8 manifest, falls back to 21).
  `SupportedOSPlatformVersion` higher than `21.0` triggers NETSDK1135.
  Setting `<TargetPlatformVersion>` in the csproj does **not** override
  the workload fallback.
- `net10.0-android` resolves to `TargetPlatformVersion=36.0` on the same SDK.
