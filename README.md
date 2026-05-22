# WPR 0.0.18-alpha
![](Images/Wpr_logo.png)

WPR is a Windows Phone 7/8 game runner that re-hosts XNA (and, increasingly,
Silverlight) titles on modern Windows desktop and Android. This is a fork of
the original [WPR](https://github.com/8212369/WPR) — heavily modified to
target **.NET 8 + Avalonia 11.3.9** with a runtime shim layer that lets
unmodified game `.xap` packages run against modern .NET.

> **Status:** work-in-progress. The `main` branch is not guaranteed to build
> or run cleanly at any given checkpoint. Active development happens on
> per-feature branches (currently `fix-zuma-revenge`).

## Screenshots
![](Images/sshot01.png)

## What's new in this fork

Since branching from upstream WPR:

- **.NET 8 / Avalonia 11.3.9 port.** Replaced the legacy Avalonia 0.9/0.10
  UI stack; rebuilt the desktop and Android entry points
  (`WPR.UI.Desktop`, `WPR.UI.Android`).
- **Silverlight runtime (initial).** Added `WPR.SilverlightCompability`,
  a from-scratch reimplementation of Silverlight 4 / Windows Phone XAML
  controls on top of Avalonia. Layout, gestures and the Panorama / Pivot
  parallax state machine are written in-tree (no Silverlight parser
  dependency). Currently boots a small set of Silverlight XAPs — see the
  compatibility table below. Launched via `SilverlightLauncher.LaunchAsync`.
- **Persistent achievements.** Per-game achievement progress is now seeded
  at install time (`XnaAchievementSeeder` scrapes TrueAchievements once and
  populates a SQLite DB) and stored across runs.
- **Refactored shim layout.** `WPR.SilverlightCompability`'s source tree now
  mirrors the upstream Silverlight namespace hierarchy (one C# file per
  type, file path matches the real namespace) — see
  [CLAUDE.md](CLAUDE.md) for the convention.
- **Per-game debug logs.** `ApplicationLaunch` mirrors `Trace`/`Debug`
  output to `%LocalAppData%\WPR\Apps\<ProductId>\wpr_game_debug.log` so
  silent-crash games leave a diagnostic file.
- **Keyboard accelerometer.** Bind keys to simulate phone tilt for games
  that use `Microsoft.Devices.Sensors.Accelerometer`. The Controls page
  (sidebar) lets you set the four tilt directions, adjust sensitivity,
  toggle the in-game tilt overlay, and live-preview the synthesized
  reading. Orientation-aware: in landscape games the screen-relative
  intent (W = "tilt up the screen") is rotated into the device-portrait
  frame the WP7 sensor contract expects.
- **Startup health-check & explicit Avalonia init logging** on both desktop
  and Android targets.
- **FontAwesome icon provider** registered in the AppBuilder; main app
  list now populates on launch (was waiting on search input).
- **Android target rebuilt** against Avalonia 11; min-SDK raised; SDL2 +
  FFmpeg bindings included via Java bindings projects.


## Architecture

WPR runs a Windows Phone game's original assemblies, IL-rewritten at
install time to redirect WP/Silverlight/XNA API calls to in-tree shims.

```
.xap / XNA folder
      │
      ▼ LibraryScanner          (discovers packages)
      ▼ ApplicationInstaller    (unpacks to %LocalAppData%\WPR\Apps\<ProductId>)
      ▼ ApplicationPatcher      (Cecil-rewrites every .dll; leaves .dll.original)
      ▼ XnaAchievementSeeder    (populates SQLite achievements DB)
      │
      ▼ (user clicks "Run")
      ▼ XnaLauncher  →  ApplicationLaunch.Start  (XNA games via FNA)
      ▼ SilverlightLauncher.LaunchAsync          (Silverlight XAPs)
```

Project layout (`Src/`):

| Project | Role |
| --- | --- |
| `Core/WPR` | Install/launch/patch pipeline, models, EF Core DB |
| `Core/WPR.Common` | Logging, paths, configuration |
| `Core/WPR.SilverlightCompability` | Silverlight 4 / WP XAML re-impl on Avalonia |
| `Core/WPR.WindowsCompability` | `System.Windows.*` shims (Application, BitmapImage, IsolatedStorage, …) |
| `Core/WPR.StandardCompability` | `System.ServiceModel` / WCF-lite shims |
| `Core/WPR.XnaCompabilityPatch` | XNA-side shims layered on top of FNA |
| `Core/Microsoft.Phone` | `Microsoft.Phone.*` (Shell, Tasks, Marketplace, Scheduler, …) |
| `Core/Microsoft.Xna.Framework.GamerServices` | Gamer profile, achievements, leaderboards |
| `Core/Microsoft.Device.Sensors` | Accelerometer / Compass |
| `Core/System.Device` | `System.Device.Location` |
| `UI/WPR.UI` | Shared Avalonia UI (views, view-models, launchers) |
| `UI/WPR.UI.Desktop` | Windows entry point (net8.0-windows10.0.17763.0) |
| `UI/WPR.UI.Android` | Android entry point |
| `ThirdParty/fna` | FNA (XNA reimplementation) |
| `ThirdParty/Icons.Avalonia` | Vendored Projektanker icons, patched for Avalonia 11.3.9 |

See [CLAUDE.md](CLAUDE.md) for the in-depth build/install/patch workflow,
including the rule that **patcher table changes require reinstalling
affected games** (the IL rewrite happens once at install time).


## Build & run

Recommended:

1. Open `Src/WPR.sln` in **Rider** (or VS 2022 17.8+).
2. Build → run `WPR.UI.Desktop`.

Target frameworks:

- Desktop: `net8.0-windows10.0.17763.0`
- Android: `net8.0-android` (set up via the system .NET SDK + Android
  workload — see [CLAUDE.md](CLAUDE.md) for the SDK version pitfalls on
  this dev box)

### CLI build (for quick edit-verify)

The full-solution `dotnet build` hits `NU1202` on `Avalonia.Android` if the
workload version doesn't line up. To verify a small edit on a leaf project:

```pwsh
dotnet build <project>.csproj -c Debug `
    -f net8.0-windows10.0.17763.0 `
    -maxcpucount:1 -nodeReuse:false --nologo
```

The `-maxcpucount:1` flag avoids an MSBuild CS0006 race; the explicit TFM
skips the Android leg.


## Game compatibility

Status of game packages tested under this fork. Compatibility is a fast
moving target — entries are tagged with where they currently sit on the
**XNA** path unless marked `[SL]` (Silverlight).

**Legend:**

| Tag | Meaning |
| --- | --- |
| `Playable` | Boots and runs end-to-end. Minor cosmetic issues OK. |
| `Partial`  | Boots; crashes / glitches / missing features in places. |
| `Broken`   | Doesn't boot, crashes early, or is missing critical shims. |
| `Untested` | Reported playable on legacy WPR; not re-verified on this fork. |

### Current status


| Game                 | Status   | Notes                                                                                                         |
|----------------------|----------|---------------------------------------------------------------------------------------------------------------|
| 3D Brick Breaker     | Playable | Working                                                                                                       |
| Asphalt 5            | Playable | Working                                                                                                       |
| Bejeweled Live +     | Playable | Working                                                                                                       |
| Brain Challenge      | Playable | Working                                                                                                       |
| Bug Village          | Playable | Working                                                                                                       |
| Castlevania Puzzle   | Partial  | Loads but unable to start arcade or story mode                                                                |
| Final Fantasy        | Playable | Working                                                                                                       |
| Fruit Ninja          | Playable | Working                                                                                                       |
| Hydro Thunder GO     | Playable | Runs end-to-end. Steering uses the keyboard accelerometer (Controls page); slow first load is expected.       |
| Plants vs Zombies    | Playable | End-to-end after tolerant SpriteBatch Begin/End                                                               |
| Sonic 4 Episode 1    | Playable | Confirmed playable after `Game.Activated` deferred-fire + multi-touch fixes                                   |
| Tentacles            | Playable | End-to-end after cross-ALC `ContentTypeReader` resolution + `Content/Scenes/` fallback + mouse-as-touch fixes |
| Uno                  | Playable | Working                                                                                                       |
| Zuma's Revenge       | Playable | Working                                                                                                       |
| Minesweeper `[SL]`   | Broken   | Help and Options pages now load; bottom app bar z-order fixed. Core gameplay still under verification.        |
| Penguins Can't Fly   | Broken   | `NullReferenceException` in `Penguin.PenguinGame.get_MenuShowing()` — missing shim/initialisation.            |
| Acedia: Indie Horror | Broken   | `InvalidOperationException: Sequence contains no elements` during `Activator.CreateInstance`.                 |
| Asphalt Pogonya      | Broken   | `PlatformNotSupportedException` on `PhoneDirect3DXamlAppInterop.App` (Direct3D XAML hybrid not supported).    |
| Dig It               | Broken   | `MissingMethodException: Microsoft.Xna.Framework.GamerServices.LeaderboardReader.get_TotalLeaderboardSize()`. |
| Mirror's Edge        | Broken   | Crashes on launch (RE in progress, see commit `c5e988d9`).                                                    |

### Reported by community on legacy WPR (Oct 2023)

These were tested against the upstream WPR before the .NET 8 / Avalonia 11
port. They have **not** all been re-verified on this fork yet — treat as
historical baseline. Source:
[Thetouchedjoe's compatibility list](Research/Thetouchedjoe%20-%20WPR.txt).

| Game                                  | Legacy status | Notes                                                           |
|---------------------------------------|---------------|-----------------------------------------------------------------|
| Civilization Revolution               | Playable      | Untested on this fork                                           |
| Cro-Mag Rally                         | Playable      | Untested on this fork                                           |
| Dream Track Nation                    | Playable      | Untested on this fork                                           |
| Final Fantasy 3                       | Playable      | Untested on this fork                                           |
| I Love Katamari                       | Playable      | Untested on this fork                                           |
| ilomilo                               | Playable      | Untested on this fork                                           |
| Kinectimals                           | Playable      | Untested on this fork                                           |
| Max and the Magic Marker              | Playable      | Untested on this fork                                           |
| MonstaFish                            | Playable      | Untested on this fork                                           |
| More Brain Exercise                   | Playable      | Untested on this fork                                           |
| NFS Undercover                        | Playable      | Untested on this fork                                           |
| Pac-Man                               | Playable      | Untested on this fork                                           |
| Skulls of the Shogun                  | Playable      | Untested on this fork                                           |
| Star Wars Cantina                     | Playable      | Untested on this fork                                           |
| The Sims 3                            | Playable      | Untested on this fork                                           |
| The Sims Medieval                     | Playable      | Untested on this fork                                           |
| Tiki Towers                           | Playable      | Untested on this fork                                           |
| Tower Bloxx                           | Playable      | Untested on this fork                                           |
| Assassin's Creed: Altaïr's Chronicles | Partial       | Graphics bug on low-end / newer devices                         |
| DeBlob Revolution                     | Partial       | Can't progress past first set of stages — crashes on completion |
| Earthworm Jim                         | Partial       | Crashes intermittently                                          |
| Guitar Hero 5 Mobile                  | Partial       | Crashes before music starts                                     |
| Ragdoll Run                           | Partial       | Buggy                                                           |
| Star Wars: Battle of the Hoth         | Broken        | Menu cannot select anything                                     |
| Angry Birds                           | Broken        | Crashes on launch                                               |
| BBB: App-ocalypse                     | Broken        | Error screen on load                                            |
| Crimson Dragon: Side Story            | Broken        | Error screen on load                                            |

> If you re-test any of these and the status has changed (good or bad),
> please open an issue with the build hash + error so this table can be
> updated.


## Runtime types supported

The installer recognises three `.xap` flavours
([`ApplicationType.cs`](Src/Core/WPR/Models/ApplicationType.cs)):

| Type | Status | Notes |
| --- | --- | --- |
| `XNA` | Working | Main path; runs on FNA via the `WPR.XnaCompability` shim layer. |
| `Silverlight` | Experimental | Boots a small set of XAPs through the in-tree `WPR.SilverlightCompability` Avalonia re-impl. |
| `ModernNative` | Not supported | C++/CX + WinRT apps ship as native PE binaries — out of scope. |


## Known limitations & TODO

- Desktop game-launch regressions following the .NET 8 / Avalonia 11.3 upgrade.
- Android target sometimes shows a white screen instead of the app UI.
- Several patcher entries from legacy WPR are still missing — game-specific
  errors above (`IsolatedStorageSettings2.Contains`, `GamerProfile.GetGamerPicture`,
  `LeaderboardReader.TotalLeaderboardSize`, etc.) are usually missing shims, not
  bugs in the runner itself.
- Silverlight runtime: only a handful of controls + the Panorama/Pivot machine
  have been implemented; `LongListSelector`, `WrapPanel`, `PhoneTextBox`,
  `PerformanceProgressBar`, `GestureService` / `GestureListener` and several
  default styles (`ButtonStyleLight`, `DarkThemePanoramaStyle`,
  `PhoneApplicationPageStyle`) are still TODO.
- README + Wiki translation (RU / CN).
- Long-term: explore a port to MAUI for unified multi-platform.


## Reinstall vs. rebuild

A common gotcha — patcher changes do **not** affect already-installed games:

- **Shim implementation change** (any `.cs` under `WPR.*Compability`,
  `Microsoft.*`, `System.*`): rebuild only. Installed games pick up the new
  behaviour on next launch.
- **Patcher table change** (`ApplicationPatcher.cs` — new entries in
  `Patches` / `MemberPatches`): rebuild **and reinstall** the affected
  games. The IL was rewritten at install time; new redirects don't apply
  retroactively.


## Tech notes

- Newest Rider / VS 2022 (17.8+) recommended.
- Targets `net8.0-windows10.0.17763.0` — Windows 11 recommended; Windows 10
  may need the 17763 (1809) baseline or newer.
- Desktop runtime pulls in `FAudio.dll` / `FNA3D.dll` / `SDL2.dll` /
  `FNWP72.dll` / `ffmpeg.exe` (shipped next to the executable).
- Per-game install data lives under `%LocalAppData%\WPR\Apps\<ProductId>`,
  with a `<game>.dll.original` sibling kept for re-patching.

## Update History
### 22/05/2026
- Compat: Plants vs Zombies promoted from Broken → Playable (end-to-end after
  achievement-seed + SpriteBatch tolerance fixes below)
- Fix: PvZ launched to a black screen because `Lawn.AchievementsWidget.Draw`
  NRE'd inside `Achievements.GetAchievementItem` — the game's static
  `gAchievementList` was empty, so the lookup returned null and the NRE
  escaped mid-`SpriteBatch.Begin`, wedging every subsequent frame on
  `"Begin has been called before calling End"`. Root cause: PvZ stores its
  18 achievement keys in an inline `Achievements.ACHIEVEMENT_KEYS[]`
  static array (built in the cctor), so none of `XnaAchievementCodeExtractor`'s
  three sources — `Content/xml/socialnetworks.xml.xnb`, IL `ldstr` near
  `AwardAchievement*` callsites, or `Content/Achievements/*.xnb` filenames —
  recovered any keys at install time. The seeder wrote 0 rows;
  `BeginGetAchievements` returned 0 rows; the game's `GetAchievementsCallback`
  added 0 items to `gAchievementList`. Added a Source D
  (`KnownProductCatalogues`) keyed by ProductId that returns hardcoded keys
  recovered via decompilation, threaded `productId` through
  `XnaAchievementCodeExtractor.ExtractRich` and `XnaAchievementSeeder.SeedAsync`.
  **Install-time change — affected games need reinstalling.** Now seeds 18
  rows and `BeginGetAchievements: 18 rows` confirms it
- Fix: After the achievement seed worked the game flickered on main menu and
  in level — `Lawn.GameSelector.DrawOverlay` still NRE'd every frame on some
  other null reference, but the deeper problem was that PvZ's `Sexy.Graphics`
  layer tracks its own `spritebatchBegan` flag separately from FNA's
  `beginCalled`, and the two desync once any Draw exception leaves the game's
  flag stale. The result was a within-frame double-Begin via
  `SetupDrawMode → EndFrame → EndDrawImageTransformed → BeginFrame`, throwing
  `InvalidOperationException` and dropping every other frame. We can't reach
  the game's private flag from the shim. Made FNA's `SpriteBatch.Begin` /
  `End` tolerant of out-of-order calls: a second `Begin` without an
  intervening `End` soft-resets and starts a fresh batch (drops the queued
  sprites of the discarded batch); a stray `End` without a matching `Begin`
  no-ops. First 5 of each are logged so we can spot if it's firing in
  production. Shim-only — no reinstall
- Feat: Diagnostic `[wpr-trace] BeginGetAchievements: N rows for <ProductId>`
  log in `SignedInGamer.BeginGetAchievements` confirms whether the install-
  time seed populated for a given game on first launch
- Feat: Keyboard accelerometer simulator. New Controls page in the desktop
  sidebar binds four keys (defaults WASD) to tilt directions; readings flow
  through the existing `Microsoft.Devices.Sensors.Accelerometer` so games
  see them without any per-game shim work. Sensitivity slider, master
  toggle, in-game tilt-overlay (Avalonia for Silverlight host, FNA
  `DrawableGameComponent` for XNA), and a live-preview dial on the Controls
  page. Orientation-aware: the screen-relative key intent gets rotated into
  the device-portrait frame the WP7 sensor contract expects, so a landscape
  game (W = "tilt up the screen") produces the correct device-X tilt the
  game interprets as steer-left. Desktop orientation is inferred from the
  back-buffer aspect because FNA's `Window.CurrentOrientation` only updates
  from SDL display-rotation events that never fire on the desktop.
  `Accelerometer.CurrentValue` / `IsDataValid` / `TimeBetweenUpdates` were
  also added so games that poll instead of subscribing to `ReadingChanged`
  see live readings too
- Compat: Hydro Thunder GO promoted from Partial → Playable (steerable via
  the keyboard accelerometer)
- Compat: Uno confirmed Playable
- Fix: Sonic 4 Episode I self-paused on every Update tick. The game's
  `AppMain.isForeground` flag is flipped true only by its `Game.Activated`
  handler, and `Activated` never fired on WPR because `INTERNAL_isActive`
  was initialised `true` (suppressing the BeforeLoop setter's no-op
  transition to avoid Asphalt 5's pre-Initialize KeyNotFoundException).
  Net effect: `isForeground` stayed false, the game's pause condition
  (`!isForeground` ORed into the trigger) re-armed every frame, and pause
  reasserted itself immediately after dismissal. Fix: defer a one-shot
  `OnActivated` to the end of the first `Game.Tick`, where every game's
  per-Update state (including Asphalt 5's) is already populated
- Fix: Window background/foreground transitions now fire
  `Deactivated`/`Activated` correctly on desktop and Android. Restored the
  `SDL_WINDOWEVENT_FOCUS_LOST → IsActive=false` branch in
  `SDL2_FNAPlatform.PollEvents` that had been commented out under a
  `//RnD` marker. Symmetric `FOCUS_GAINED` was already wired
- Feat: Multi-touch input is now additive with the mouse-as-touch shim.
  `UpdateTouchPanelState` previously branched either-or — if
  `TouchPanel.MouseAsTouch` was on (the default for XNA games on WPR) the
  real-finger poll loop was skipped entirely, capping all input at a
  single touch even on multi-touch hardware. New shape: real fingers fill
  slots `0..MAX_TOUCHES-2` from `SDL_GetTouchFinger`; when `MouseAsTouch`
  is on the mouse takes the last slot with synthetic finger ID
  `int.MaxValue` so it can never collide with a real finger's ID.
  Sonic 4's "hold D-pad + tap jump" gameplay now works
- Fix: Asphalt 5 splash → menu tap-to-continue now registers. The
  per-`Game.Tick` `FrameworkDispatcher.Update()` call added on 21/05 was
  redundant — stock FNA's `Game.Update` already pumps the dispatcher at its
  end. Pumping twice per tick made `TouchPanel.Update` run twice in close
  succession; the second run promoted `touches[0]` from `Pressed` to `Moved`
  before `CGame1.g()` could read it, so `h2.b` (press handler) never
  recorded the finger and `h2.d`'s `if (i > 0)` release-guard always
  failed. Splash-state `lt.b()` waiting on `be.ey.fm != 0` was the surfaced
  case. Removed the redundant pump in FNA `Game.Tick`
- Compat: Asphalt 5 promoted from Partial → Playable
- Compat: Sonic 4 Episode I confirmed Playable (pause loop fixed,
  multi-touch routed)
- Compat: Tentacles promoted from Broken → Playable (end-to-end after the
  fixes below)
- Fix: FNA's `ContentTypeReaderManager` couldn't resolve
  `ReflectiveReader<PressPlay.FFWD.Scene>` because `Type.GetType()` doesn't
  see types in the user game's collectible ALC, and the existing
  `string.Split(',')` fallback broke on the generic argument
  `[[PressPlay.FFWD.Scene, PressPlay.FFWD, …]]` (the comma between the
  inner type and its assembly hint was indistinguishable from the outer
  delimiter). Replaced with `Type.GetType`'s resolver-callback overload
  that walks `AppDomain.CurrentDomain.GetAssemblies()` — which returns
  every loaded assembly across every ALC — so generic readers
  parameterised by user-ALC types resolve correctly. Tentacles' Preloader
  scene XNB now deserialises
- Fix: FFWD-based games call `Application.LoadLevel("X")` with bare scene
  names even though every level XNB ships under `Content/Scenes/`.
  ContentManager constructed `Content/X.xnb`, the file wasn't there,
  `AssetHelper.Load<T>` silently swallowed the failure and returned
  `default(T)`, and the loading screen hung forever waiting on
  `loadingProgress == 1.0f`. `TitleContainer.OpenStream` now retries
  `Content/<name>.xnb` as `Content/Scenes/<name>.xnb` when the original is
  missing and has no subdirectory hint. Safe for non-FFWD games (fallback
  file simply won't exist either)
- Fix: `Microsoft.Phone.Shell.StandardTileData.Count` was `int` in our
  shim but the WP7 SDK uses `int?`. Tentacles' live-tile updater calls
  `set_Count(int?)` every frame from a component Update and was tripping
  `MissingMethodException` ~once per tick
- Fix: `FNA.Game.RunLoop` now wraps the final `OnExiting(this, EventArgs.Empty)`
  in try/catch + log. Tentacles' `MetricsSender.CreateTearDownExtendedKeys`
  NREs on `GlobalManager.Instance.currentProfile` when the user closes
  during early boot (currentProfile is null until the preloader finishes
  loading it). Can't fix the game code, but the host no longer surfaces
  its "unexpected error" dialog on close
- Fix: `SignedInGamer.SignedIn` handler invocations now serialise behind a
  `SemaphoreSlim`. Tentacles' `Game1.Initialize` registers the same
  callback twice; both invocations were firing in parallel via separate
  `Task.Delay.ContinueWith`s, racing on the shared
  `AchievementContext.Current` and tripping EF Core's `ConcurrencyDetector`
  inside `Gamer.GetProfile`. Handlers still get their independent ~2 s
  delay; only the synchronous Invoke runs single-file
- Fix: Mouse-as-touch clicks now produce `GestureType.Tap` gestures on
  desktop. `SDL_MOUSEMOTION` was forwarded to `INTERNAL_onTouchEvent`
  unconditionally, including hover (no button held). Hover motion ran
  `GestureDetector.OnMoved` which set `activeFingerId = 1`; the next
  `MOUSEBUTTONDOWN`'s `OnPressed(1)` then saw `activeFingerId != NO_FINGER`
  and routed into pinch-init (state = PINCHING). The matching
  `MOUSEBUTTONUP` ran `OnReleased_Pinch` instead of Tap detection — so
  Tap-gated screens (Tentacles' `LemmyTravelScreen`, "tap to continue"
  prompts) never advanced from mouse but worked fine from real touch
  (no hover). Fix: `SDL2_FNAPlatform` skips the synthesised Moved event
  when `evt.motion.state == 0`. Drag (button-held motion) still goes
  through, so swipe/pinch/drag from mouse continue to work
- Feat: Periodic FFWD loading-state heartbeat in FNA `Game.Tick`
  (`[wpr-heartbeat]`, fires ~every 2 s past the first-30-ticks verbose
  trace cap). Reflects `PressPlay.FFWD.Application` and
  `PressPlay.Tentacles.Scripts.LevelHandler` static fields plus the
  active screen stack and the current level identity — turned a "stuck
  on loading screen" symptom into a precise gate-by-gate diagnosis.
  Best-effort, swallows all reflection errors, `[Conditional("DEBUG")]`-gated
- Feat: `ContentManager.Load<T>` now logs the underlying exception with
  type/HResult/inner/stack before rethrowing (`[wpr-content]`). FFWD's
  `AssetHelper.Load` silently catches ContentManager.Load failures and
  returns `default(T)`; without the trace, a missing ContentTypeReader or
  malformed XNB was invisible. Trace lets the caller's swallow stand —
  only adds visibility
- Feat: Assembly-resolver diagnostics in `ApplicationLaunch` —
  `[wpr-resolve-user]` / `[wpr-resolve-default]` lines log every Resolving
  probe with full exception details when `LoadFromAssemblyPath` fails

### 21/05/2026
- Fix: Asphalt 5 sat on a blank loading screen — `StartupMode` enum integer
  values now match the WP7 SDK (`Launch=1`, `Activate=2`), and FNA's
  `Game.IsActive` no longer fires a spurious `Activated` event at first frame
- Fix: Tentacles crashed at exit on a null `ApplicationCurrentMemoryUsage` —
  `Microsoft.Phone.Info.DeviceExtendedProperties` now returns the WP7 memory
  counters (`ApplicationCurrentMemoryUsage`, `ApplicationPeakMemoryUsage`,
  `ApplicationMemoryUsageLimit`)
- Fix: XNA games that wait on `MediaPlayer` / song-finished callbacks before
  advancing past their splash now progress — `Game.Tick` auto-pumps
  `FrameworkDispatcher.Update()` once per update, matching WP7 XNA 4.0 behaviour
- Feat: Per-game `wpr_game_debug.log` and the in-engine `[wpr-trace]` output
  are now `#if DEBUG`-gated via `WprDebugTrace` — Release builds elide the
  trace formatting and file listener entirely (no log spam, no per-frame cost)
- Feat: Richer assembly-resolver diagnostics in `ApplicationLaunch` —
  `Resolving` failures log the underlying exception type, HResult and inner
  exception instead of the opaque "Operation is not supported" surface error
- Compat: Asphalt 5 promoted from Broken → Partial (loads splash, gameplay TBD)
- Compat: Final Fantasy promoted to Playable
- Compat: Fruit Ninja confirmed Playable (no longer blocked on
  `GamerProfile.GetGamerPicture`)

### 17/05/2026
- Fix: Small bug on second launch of XNA games, where resources arent released fully
- Feat: Added Windows based notifications
- Feat: Added game icon as window icon for XNA

## Credits

- [mediaexplorer74/WPR](https://github.com/mediaexplorer74/WPR) — the fork this
  one is based on; foundational Avalonia port work, Android target groundwork,
  and the long-running RnD that made everything downstream possible
- [Tyler Jaacks](https://github.com/TylerJaacks) — net5/6 → net8 upgrade
- [Hector47](https://github.com/Hector47) — online services groundwork

### Related forks worth looking at

- [TylerJaacks/WPR](https://github.com/TylerJaacks/WPR) — branches
  `net8_upgrade` and `dotnet_upgrade` carry useful work
- [Hector47/WPR](https://github.com/Hector47/WPR) — `master` has GameServices ideas
- [yangzhongke/Windows-Phone-Emulator](https://github.com/yangzhongke/Windows-Phone-Emulator) —
  Silverlight 4 prior art for WP control reimplementations (defers to the
  Silverlight XAML parser, so not transplantable here, but the C# for
  Panorama/Pivot/Transitions is a useful reference)


## ::

AS IS. No support. Developers / geeks only — DIY mode.
