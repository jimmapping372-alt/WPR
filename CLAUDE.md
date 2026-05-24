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

## Building the Android leg (WPR.UI.Android)

The Android project builds today, but **not via Rider with default config** — it needs
the user-local .NET 8 SDK and a non-default Android SDK path. Verified working CLI recipe
(no admin elevation required):

```powershell
$env:DOTNET_ROOT       = "C:\Users\BenSl\.dotnet"
$env:ANDROID_HOME      = "C:\Users\BenSl\AppData\Local\Android\Sdk"
$env:ANDROID_SDK_ROOT  = $env:ANDROID_HOME
$env:JAVA_HOME         = "C:\Program Files\Android\Android Studio\jbr"
& "C:\Users\BenSl\.dotnet\dotnet.exe" build `
    "Src\UI\WPR.UI.Android\WPR.UI.Android.csproj" `
    -c Debug -maxcpucount:1 -nodeReuse:false --nologo `
    -p:AndroidSdkDirectory="$env:ANDROID_HOME"
```

Output: `Src\UI\WPR.UI.Android\bin\Debug\net8.0-android34.0\com.wpr.android-Signed.apk` (~31 MB).

### Why this is fiddly
- **System SDK is .NET 10 only.** `C:\Program Files\dotnet` has no .NET 8 manifest, so its
  Android workload (`36.1.x/10.0.100`) targets API 36 only — incompatible with the project's
  `net8.0-android34.0` TFM. Restoring with the system SDK fails NU1202 on `Avalonia.Android 11.1-beta1`.
- **User-local SDK has the right workload.** `C:\Users\BenSl\.dotnet` (`.NET 8.0.420`) has
  the .NET 8 Android workload (`34.0.154/8.0.100`) with packs `Microsoft.Android.Ref.34` etc.
  Invoking that `dotnet.exe` directly resolves restore correctly.
- **android-34 platform must exist somewhere the build can find.** The build default is
  `C:\Program Files (x86)\Android\android-sdk\platforms\android-34\android.jar` which is
  missing. Android Studio installs API 34 into `C:\Users\BenSl\AppData\Local\Android\Sdk`
  (per `android.sdk.path.xml`) — non-admin. Set `AndroidSdkDirectory` / `ANDROID_HOME`
  to that user-local path.

### .NET / Android API mapping (Microsoft locked these)
- `net8.0-android*` → API **34** only. There is no `net8.0-android35.0`.
- `net9.0-android*` → API **35** only.
- `net10.0-android*` → API **36** only.

Also: `Avalonia.Android` skipped .NET 9. Version 11.x ships only `lib/net8.0-android34.0/`;
12.x ships only `lib/net10.0-android36.0/`. To move off API 34 you must move all the way
to net10 + Avalonia 12.

### Rider build path (not currently working)
Rider's MSBuild only sees the system .NET 10 SDK at `C:\Program Files\dotnet`. Two ways
to make Rider build Android:

1. **Install .NET 8 SDK system-wide** (admin):
   - Download .NET 8 SDK installer from dot.net or `winget install Microsoft.DotNet.SDK.8`.
   - In an elevated shell: `dotnet workload install android` (with .NET 8 active via a
     temporary global.json pinning to 8.0.x).
   - Set `ANDROID_HOME` user env var to `C:\Users\BenSl\AppData\Local\Android\Sdk`.
2. **Point Rider at the user-local SDK** (no admin): Settings → Build → Toolset and
   Build → .NET CLI executable path → `C:\Users\BenSl\.dotnet\dotnet.exe`. Same
   `ANDROID_HOME` env var as above.

The project itself is fine — there's no code or csproj change needed beyond what's
already in place. The fix lives entirely in the environment.

## Environment notes (as of 2026-05-11)

- System .NET SDK: `C:\Program Files\dotnet` — **.NET 10.0.203**, Android
  workload `36.1.43/10.0.100` (no .NET 8 manifest). Rider's MSBuild uses
  this one.
- User-local .NET SDK: `C:\Users\{user}\.dotnet` — **.NET 8.0.420**, Android
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
