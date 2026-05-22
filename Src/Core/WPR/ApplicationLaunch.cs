using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Runtime.Loader;
using WPR.Models;
using WPR.Common;

using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.GamerServices;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WPR.XnaCompability;

namespace WPR
{
    public static class ApplicationLaunch
    {
        /// <summary>
        /// <c>[Conditional("DEBUG")]</c> wrapper around <see cref="Trace.WriteLine(string)"/>.
        /// In Release builds the C# compiler elides every call site (including argument
        /// expressions), so an interpolated string like <c>WprTrace($"x={x}")</c> costs
        /// nothing at all — no string allocation, no listener dispatch.
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private static void WprTrace(string msg) => Trace.WriteLine(msg);

        /// <summary>
        /// The game currently being driven by <see cref="Start"/>, or null if no game is running.
        /// Used by the host so it can request a clean shutdown when the main window closes.
        /// </summary>
        public static Game? CurrentGame { get; private set; }

        /// <summary>
        /// Asks the active game (if any) to exit. Returns true if a game was running.
        /// Safe to call from any thread.
        /// </summary>
        public static bool RequestExit()
        {
            Game? g = CurrentGame;
            if (g == null) return false;
            try { g.Exit(); }
            catch { /* best-effort: process may be tearing down */ }
            return true;
        }

        private static string CurrentProductFolder => Path.Combine(Configuration.Current.DataPath(Application.DataStoreFolder),
            WindowsCompability.Application.Current!.ProductId!);

        /// <summary>
        /// The collectible ALC for the currently-running user game, or null between launches.
        /// Set by <see cref="Start"/> right after the ALC is created and cleared after unload.
        /// </summary>
        private static AssemblyLoadContext? _CurrentUserAlc;

        static ApplicationLaunch()
        {
            // When default-ALC code (e.g. FNA's ContentTypeReaderManager doing Type.GetType
            // for a user content reader) asks to resolve a user assembly, route the load
            // into the CURRENT collectible ALC. Returning the foreign-ALC assembly from this
            // handler is valid — the default ALC treats it as a reference but the assembly
            // and its statics remain in the user ALC, so they go away cleanly on unload.
            //
            // The previous implementation called `loadContext.LoadFromAssemblyPath(...)`,
            // which loaded the user dll INTO the default ALC (loadContext == Default). That
            // permanently contaminated the default ALC with launch-1 user types — their
            // statics persisted across launches and FNA's static
            // ContentTypeReaderManager.contentReadersCache pinned launch-1 reader instances
            // forever. Symptom: launch-2 SexyFramework code ended up cross-wired with
            // launch-1 driver singletons, manifesting as NREs deep in the content load path
            // (e.g. Music.RegisterCallBack NRE because mApp.mMusicInterface was never set
            // on the launch-2 SexyAppBase).
            AssemblyLoadContext.Default.Resolving += (loadContext, name) =>
            {
                var userAlc = _CurrentUserAlc;
                if (userAlc == null) return null;
                string candidate = Path.Combine(CurrentProductFolder, name.Name + ".dll");
                if (!File.Exists(candidate)) return null;
                try { return userAlc.LoadFromAssemblyPath(candidate); }
                catch (Exception ex)
                {
                    WprTrace($"[wpr-resolve-default] FAIL {name.FullName} via {candidate}: {ex.GetType().FullName} hr=0x{ex.HResult:X8} msg=\"{ex.Message}\"");
                    return null;
                }
            };
        }

        public static async Task Start(Application app, Action<DisplayOrientation>? requestOrientation = null, Action<Game>? onGameCreated = null)
        {
            if (app.ApplicationType == ApplicationType.Silverlight)
            {
                throw new NotSupportedException(
                    "Silverlight UI runtime is not yet implemented. " +
                    "This XAP installs successfully but cannot be launched yet.");
            }

            if (app.ApplicationType == ApplicationType.ModernNative)
            {
                throw new NotSupportedException(
                    "Modern Native (C++/CX, WinRT) apps are not supported. " +
                    "These ship as native ARM/x86 PE binaries against WinRT — they cannot run on " +
                    "this CLR-based runner without a native loader and a WinRT reimplementation.");
            }

            if (app.ApplicationType != ApplicationType.XNA)
            {
                throw new NotSupportedException(
                    $"Application runtime type '{app.ApplicationType}' is not supported.");
            }

            // Setting game folder path
            WindowsCompability.Application.Current.ProductId = app.ProductId;
            string folderPath = CurrentProductFolder;

            FNAPlatform.TitleLocation = folderPath;
            string curDir = Directory.GetCurrentDirectory();

            // Mirror Debug/Trace output to a per-game log file so we can see exceptions that
            // Game.Tick swallows (Update/Draw/EndDraw catches that only Debug.WriteLine the
            // failure). Without this, a game whose Draw throws every frame appears as a silent
            // black screen with no diagnostic file (since Run() itself doesn't throw).
            //
            // Gated on DEBUG so end users on Release builds don't get a 300 KB-per-launch log
            // file (and don't pay for the per-frame Trace traffic that fills it). To debug a
            // game locally, build the solution in Debug — the per-game wpr_game_debug.log will
            // reappear next to the install directory.
            TextWriterTraceListener? debugListener = null;
#if DEBUG
            try
            {
                string debugLogPath = Path.Combine(folderPath, "wpr_game_debug.log");
                var fs = new FileStream(debugLogPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                var sw = new StreamWriter(fs) { AutoFlush = true };
                sw.WriteLine($"=== WPR game debug log ===");
                sw.WriteLine($"App: {app.Name}  ProductId: {app.ProductId}");
                sw.WriteLine($"Started: {DateTime.UtcNow:o}");
                sw.WriteLine();
                debugListener = new TextWriterTraceListener(sw, "wpr_game_debug");
                Trace.Listeners.Add(debugListener);
                WprTrace("[wpr-trace] ApplicationLaunch: trace listener attached (smoke test)");
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppList, $"Could not attach debug log listener: {ex.Message}");
            }
#endif

            // Use a collectible ALC so closing the game can unload the user assembly — otherwise
            // the .dll stays locked on disk for the life of WPR (blocking re-install) and any
            // statics in the user assembly (timers, threads, caches) leak forever.
            string asmPath = Path.Combine(folderPath, AssemblyNameStandardization.Process(app.Assembly));
            var alc = new AssemblyLoadContext(
                $"WPR.XnaApp[{Path.GetFileNameWithoutExtension(asmPath)}]",
                isCollectible: true);
            alc.Resolving += (ctx, name) =>
            {
                string candidate = Path.Combine(folderPath, name.Name + ".dll");
                if (File.Exists(candidate))
                {
                    try
                    {
                        var loaded = ctx.LoadFromAssemblyPath(candidate);
                        WprTrace($"[wpr-resolve-user] OK   {name.FullName} -> {candidate} -> {loaded.FullName}");
                        return loaded;
                    }
                    catch (Exception ex)
                    {
                        // The runtime will re-raise FileLoadException for this name after we
                        // return null / let it propagate; log the actual underlying failure
                        // here so we can see HResult and exception type that the surface error
                        // hides ("Operation is not supported (0x80131515)" being the typical
                        // opaque case).
                        WprTrace($"[wpr-resolve-user] FAIL {name.FullName} via {candidate}: {ex.GetType().FullName} hr=0x{ex.HResult:X8} msg=\"{ex.Message}\"");
                        if (ex.InnerException != null)
                            WprTrace($"[wpr-resolve-user] FAIL inner: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                        WprTrace($"[wpr-resolve-user] FAIL stack: {ex.StackTrace}");
                        throw;
                    }
                }

                // Fall back to name-only load — handles WinMD-projected refs like
                // `mscorlib v=255.255.255.255` that the strict version binder rejects.
                try
                {
                    var loaded = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(name.Name!));
                    WprTrace($"[wpr-resolve-user] DEF  {name.FullName} -> {loaded.FullName}");
                    return loaded;
                }
                catch (Exception ex)
                {
                    WprTrace($"[wpr-resolve-user] MISS {name.FullName}: {ex.GetType().FullName} hr=0x{ex.HResult:X8} msg=\"{ex.Message}\"");
                    return null;
                }
            };
            // Publish the ALC so the Default.Resolving handler routes user-assembly loads
            // here instead of contaminating the default ALC. Must be set BEFORE LoadFromAssemblyPath
            // because the main assembly's load can trigger dependency resolution that hits the
            // default ALC handler.
            _CurrentUserAlc = alc;
            Assembly assem = alc.LoadFromAssemblyPath(asmPath);

            Directory.SetCurrentDirectory(folderPath);

            // Instatiate
            Type? mainType = assem.GetType(app.EntryPoint);

            // Run on separate thread to not affect the UI
            await Task.Run(() =>
            {
                // Set this BEFORE the Game subclass is instantiated. Many WP7 games probe
                // TouchPanel.GetCapabilities().IsConnected from their constructor or static
                // init paths and cache the result for the rest of the session. If MouseAsTouch
                // is still false at ctor time, GetCapabilities returns IsConnected=false,
                // the cached "no touch" decision sticks, and the game stops handling taps —
                // which exactly matches the "Angry Birds buttons never respond" and
                // "Zombies touch dies after scene change" reports. The setter on FNA's
                // TouchPanel (with our WPR-side patch) eagerly flips TouchDeviceExists=true
                // when MouseAsTouch goes true, so GetCapabilities reports a real device on
                // the very first read.
#if !__MOBILE__
                TouchPanel.MouseAsTouch = true;
#endif
                TouchPanel.EnabledGestures = GestureType.DoubleTap | GestureType.Tap | GestureType.Hold |
                    GestureType.HorizontalDrag | GestureType.VerticalDrag | GestureType.FreeDrag |
                    GestureType.Pinch | GestureType.Flick | GestureType.DragComplete | GestureType.PinchComplete;

                // Manage Game lifetime explicitly (no using) so we can interleave teardown:
                // Game.Dispose has to run BEFORE FAudio is destroyed (Game.Content.Dispose
                // disposes SoundEffectInstances which destroy their FAudio source voices —
                // those calls are invalid once the FAudio engine handle is released), but
                // ALC unload and WPR-side singleton reset have to run AFTER Game.Dispose
                // (the game's Dispose may still touch our shims).
                // Reset gamer-services state BEFORE the Game subclass is instantiated.
                // Game ctors typically register a SignedIn handler from inside the ctor;
                // if a prior launch left FirstSignInSessionDone == true, the handler would
                // fire synchronously during `+=` (before the ctor finishes initialising
                // its fields), which NREs e.g. Assassin's Creed XNAGame.
                SignedInGamer.Reset();

                Game? obj = Activator.CreateInstance(mainType!) as Game;
                try
                {
                    CurrentGame = obj;
                    obj!.IsMouseVisible = true;
                    obj!.Window.Title = $"{app.Name} - {app.Author} (Publisher: {app.Publisher})";

                    // Hook for the host to decorate the just-created SDL window (e.g. set the
                    // game's icon via SDL_SetWindowIcon). Best-effort: a host that throws here
                    // shouldn't prevent the game from running.
                    if (onGameCreated != null)
                    {
                        try { onGameCreated(obj); }
                        catch (Exception ex) { Log.Warn(LogCategory.AppList, $"onGameCreated hook threw: {ex.Message}"); }
                    }

                    // Re-affirm in case the user game's ctor reset these (it shouldn't, but
                    // the cost is one static field write).
#if !__MOBILE__
                    TouchPanel.MouseAsTouch = true;
#endif

                    GraphicsDeviceManager2.RequestOrientation = requestOrientation;

                    WprTrace("[wpr-trace] ApplicationLaunch: subscribing to obj.Activated");
                    obj.Activated += (obj, args) =>
                    {
                        WprTrace("[wpr-trace] ApplicationLaunch: obj.Activated fired -> HandleApplicationStart(true)");
                        PhoneApplicationService.Current!.HandleApplicationStart(true);
                    };

                    // FNA only raises Game.Activated when the SDL window receives a focus event.
                    // On some setups (the Avalonia host already had focus, or SDL didn't deliver
                    // the focus-gained event for a freshly-created game window) the event never
                    // fires and the WP7 lifecycle callbacks (Launching / Activated) never run —
                    // the game then sits on a "not initialised" black screen because its
                    // Application_Launching handler is where it builds its scene graph.
                    // Fire the lifecycle once, unconditionally, right at startup as a safety net.
                    WprTrace("[wpr-trace] ApplicationLaunch: priming PhoneApplicationService.HandleApplicationStart(true) before Game.Run");
                    try
                    {
                        PhoneApplicationService.Current!.HandleApplicationStart(true);
                    }
                    catch (Exception ex)
                    {
                        WprTrace("[wpr-ex] HandleApplicationStart priming threw: " + ex);
                    }

                    GraphicsDeviceManager? manager = obj.Services.GetService(typeof(IGraphicsDeviceManager)) as GraphicsDeviceManager;
                    if (manager != null)
                    {
                        manager.PreparingDeviceSettings += (obj, args) =>
                        {
                            GraphicsDeviceManager2.RequestOrientationChange(
                                args.GraphicsDeviceInformation.PresentationParameters.BackBufferWidth,
                                args.GraphicsDeviceInformation.PresentationParameters.BackBufferHeight
                            );
                        };
                    }

                    try
                    {
                        WprTrace("[wpr-trace] ApplicationLaunch: about to call Game.Run()");
                        // Run the game and capture any exceptions to produce richer diagnostics.
                        try
                        {
                            obj.Run();
                            WprTrace("[wpr-trace] ApplicationLaunch: Game.Run() returned normally");
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                string diag = BuildDiagnostics(obj, ex);

                                // Log to the configured logger
                                Log.Error(LogCategory.AppList, $"Game threw during Run: {ex}");

                                // Also try to write diagnostics to a file next to the game's folder so it survives process exits
                                try
                                {
                                    string diagFile = Path.Combine(folderPath, "wpr_game_diagnostic.txt");
                                    File.WriteAllText(diagFile, diag);
                                }
                                catch (Exception fileEx)
                                {
                                    Log.Warn(LogCategory.AppList, $"Failed to write diagnostic file: {fileEx}");
                                }
                            }
                            catch (Exception logEx)
                            {
                                // Avoid swallowing the original exception but at least log that diagnostics failed
                                Log.Warn(LogCategory.AppList, $"Diagnostics failed: {logEx}");
                            }

                            // Rethrow so outer code still receives the original failure
                            throw;
                        }

                        try
                        {
                            PhoneApplicationService.Current!.HandleApplicationExit();
                        }
                        catch (Exception ex)
                        {
                            Log.Warn(LogCategory.AppList, $"Ignored clean-up exception:\n {ex}");
                        }

                        obj.Exit();
                    }
                    finally
                    {
                        CurrentGame = null;
                        // Ensure current directory is restored to previous value to avoid surprising callers
                        try
                        {
                            Directory.SetCurrentDirectory(curDir);
                        }
                        catch { }

                        // MediaPlayer.Stop has to happen while FAudio is still alive — it sends
                        // an FAudio call. The rest of audio teardown waits until after Game.Dispose.
                        try { Microsoft.Xna.Framework.Media.MediaPlayer.Stop(); }
                        catch (Exception ex) { Log.Warn(LogCategory.AppList, $"MediaPlayer.Stop on teardown threw: {ex.Message}"); }
                    }
                }
                finally
                {
                    // Game.Dispose runs here. It walks Components, Content (disposes loaded
                    // SoundEffects → SoundEffectInstances → destroys their FAudio source voices),
                    // GraphicsDevice, and the SDL window. All of that requires FAudio to still
                    // be alive, which is why TeardownAudioState is below.
                    try { obj?.Dispose(); }
                    catch (Exception ex) { Log.Warn(LogCategory.AppList, $"Game.Dispose threw: {ex.Message}"); }
                }

                // Now safe to tear down the audio engine itself: every SoundEffectInstance has
                // released its source voice via Content.Dispose above.
                TeardownAudioState();

                // Drop user-assembly references then unload the ALC so the .dll is freed and any
                // statics inside the user assembly (timers, caches) can be reclaimed.
                try
                {
                    PhoneApplicationService.Current?.HandleApplicationExit();
                    ResetWprSingletons();
                    alc.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    Log.Warn(LogCategory.AppList, $"ALC unload best-effort failed: {ex}");
                }

                if (debugListener != null)
                {
                    try
                    {
                        Trace.Listeners.Remove(debugListener);
                        debugListener.Flush();
                        debugListener.Close();
                        debugListener.Dispose();
                    }
                    catch { /* best-effort: we're on the teardown path */ }
                }
            });
        }

        /// <summary>
        /// Tear down FNA's audio state so a song started via <c>MediaPlayer.Play</c> doesn't
        /// keep playing after <see cref="Game.Dispose"/> and so the audio subsystems aren't
        /// left bound to a destroyed FAudio engine when the next game launches. Three layers,
        /// in this order:
        /// <list type="number">
        ///   <item><description><see cref="Microsoft.Xna.Framework.Media.MediaPlayer.DisposeIfNecessary"/>
        ///     calls <c>FAudio.XNA_SongQuit</c> and clears MediaPlayer's <c>initialized</c>
        ///     static — without this, MediaPlayer skips re-init on the next launch because
        ///     <c>initialized</c> is still true, but the song subsystem it's pointing at was
        ///     bound to a now-destroyed FAudio engine, and subsequent <c>XNA_PlaySong</c>
        ///     calls hit dangling internal state.</description></item>
        ///   <item><description>Reflective dispose of FNA's internal <c>FAudioContext.Context</c>
        ///     singleton releases the master voice and the FAudio engine handle. FNA only ever
        ///     nulls this from inside its own Dispose() so we have to drive that path. Caller
        ///     must have already disposed every live <c>SoundEffectInstance</c>: those hold
        ///     FAudio source voices that need to be destroyed via the still-live engine.</description></item>
        /// </list>
        /// Caller is responsible for stopping <c>MediaPlayer</c> while FAudio is still alive
        /// (it issues an FAudio call). All steps here are best-effort: any failure is logged
        /// and swallowed because we're already on the teardown path.
        /// </summary>
        private static void TeardownAudioState()
        {
            try
            {
                // FNA exposes DisposeIfNecessary as internal — reflection. Without this the
                // 'initialized' static stays true across launches and the new FAudio engine
                // ends up with no song subsystem registered, manifesting as silent audio or
                // NREs deep inside the next game's resource loader.
                Type? mp = typeof(Microsoft.Xna.Framework.Media.MediaPlayer);
                MethodInfo? disposeIfNecessary = mp.GetMethod(
                    "DisposeIfNecessary",
                    BindingFlags.NonPublic | BindingFlags.Static);
                disposeIfNecessary?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppList, $"MediaPlayer.DisposeIfNecessary on teardown threw: {ex.Message}");
            }

            try
            {
                // FNA's FAudioContext is a private nested class of SoundEffect with a static
                // 'Context' field. Reach in by reflection — FNA only ever sets it to null inside
                // its own Dispose(), so we have to drive that path here.
                Type? sfx = typeof(Microsoft.Xna.Framework.Audio.SoundEffect);
                Type? ctxType = sfx.GetNestedType("FAudioContext", BindingFlags.NonPublic);
                FieldInfo? ctxField = ctxType?.GetField("Context", BindingFlags.Public | BindingFlags.Static);
                object? ctx = ctxField?.GetValue(null);
                if (ctx != null)
                {
                    MethodInfo? dispose = ctxType!.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance);
                    dispose?.Invoke(ctx, null);
                }
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppList, $"FAudioContext dispose on teardown threw: {ex.Message}");
            }
        }

        /// <summary>
        /// Mirror of the singleton reset that <c>SilverlightLauncher</c> performs on window
        /// close. The XNA path historically skipped this, so the second launch of an XNA game
        /// inherited launch-1's <c>Application.Current</c>, <c>PhoneApplicationService.Current</c>,
        /// and <c>HostContext</c> state — including event subscribers pinned to types in the
        /// previous (unloaded) ALC. Best-effort; any single failure is logged and skipped.
        /// </summary>
        private static void ResetWprSingletons()
        {
            // Application.Current lazy-creates a fresh instance on the next access, so it's
            // safe to clear to null here.
            try { WindowsCompability.Application.ResetCurrent(); }
            catch (Exception ex) { Log.Warn(LogCategory.AppList, $"Application.ResetCurrent threw: {ex.Message}"); }

            try
            {
                // PhoneApplicationService.Current is a plain `=> _Current` getter (no lazy
                // init — the cctor seeds it once per process), so we must REPLACE the
                // singleton with a fresh instance, not null. The new instance's public ctor
                // sets _Current = this internally; we still SetValue afterwards to be
                // explicit. Without this swap, event subscribers added by launch 1 stay
                // wired to types in the unloaded ALC, and any cross-launch handler invoke
                // would touch dead types.
                Type t = typeof(WPR.SilverlightCompability.PhoneApplicationService);
                FieldInfo? f = t.GetField("_Current", BindingFlags.NonPublic | BindingFlags.Static);
                object? fresh = Activator.CreateInstance(t);
                f?.SetValue(null, fresh);
            }
            catch (Exception ex) { Log.Warn(LogCategory.AppList, $"PhoneApplicationService reset threw: {ex.Message}"); }

            try
            {
                WPR.SilverlightCompability.HostContext.UserAssembly = null;
                WPR.SilverlightCompability.HostContext.CurrentProductId = null;
                WPR.SilverlightCompability.HostContext.CurrentInstallFolder = null;
            }
            catch (Exception ex) { Log.Warn(LogCategory.AppList, $"HostContext reset threw: {ex.Message}"); }

            try
            {
                // FNA's ContentTypeReaderManager.contentReadersCache is a private static
                // Dictionary<Type, ContentTypeReader> that's never cleared. It caches reader
                // instances keyed by user-assembly Type objects, which pins the previous
                // launch's user ALC alive forever and (if AppDomain.GetAssemblies fallback
                // is hit) lets launch-2's content path see launch-1's reader. FNA exposes
                // ClearTypeCreators which only clears the *creator-func* dict, not this one.
                // Reach in by reflection.
                Type? ctrm = typeof(Microsoft.Xna.Framework.Content.ContentTypeReaderManager);
                FieldInfo? cacheField = ctrm.GetField(
                    "contentReadersCache",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (cacheField?.GetValue(null) is System.Collections.IDictionary cacheDict)
                {
                    cacheDict.Clear();
                }
            }
            catch (Exception ex) { Log.Warn(LogCategory.AppList, $"ContentTypeReaderManager.contentReadersCache reset threw: {ex.Message}"); }

            // Stop routing default-ALC resolves to this (now-unloading) user ALC.
            _CurrentUserAlc = null;
        }

        private static string BuildDiagnostics(Game? obj, Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== WPR Game Diagnostic ===");
            sb.AppendLine("Timestamp: " + DateTime.UtcNow.ToString("o"));
            sb.AppendLine();

            sb.AppendLine("Exception:");
            sb.AppendLine(ex.ToString());
            sb.AppendLine();

            if (obj == null)
            {
                sb.AppendLine("Game instance is null.");
                return sb.ToString();
            }

            try
            {
                Type t = obj.GetType();
                sb.AppendLine($"Game type: {t.FullName}");

                // Common Game properties
                try { sb.AppendLine($"Window: {(obj.Window == null ? "<null>" : obj.Window.GetType().FullName)}"); } catch { sb.AppendLine("Window: <error>"); }
                try { sb.AppendLine($"Window.Title: {(obj.Window?.Title ?? "<null>")}"); } catch { sb.AppendLine("Window.Title: <error>"); }
                try { sb.AppendLine($"GraphicsDevice: {(obj.GraphicsDevice == null ? "<null>" : obj.GraphicsDevice.GetType().FullName)}"); } catch { sb.AppendLine("GraphicsDevice: <error>"); }
                try { sb.AppendLine($"Content: {(obj.Content == null ? "<null>" : obj.Content.GetType().FullName)}"); } catch { sb.AppendLine("Content: <error>"); }
                try { sb.AppendLine($"Services: {(obj.Services == null ? "<null>" : obj.Services.GetType().FullName)}"); } catch { sb.AppendLine("Services: <error>"); }

                sb.AppendLine();

                // Inspect instance fields and properties (best-effort, do not throw)
                sb.AppendLine("Instance fields:");
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    try
                    {
                        object? val = field.GetValue(obj);
                        sb.AppendLine($"- {field.Name} ({field.FieldType.FullName}): {(val == null ? "<null>" : val.GetType().FullName)}");
                    }
                    catch (Exception fex)
                    {
                        sb.AppendLine($"- {field.Name}: <error reading> {fex.Message}");
                    }
                }

                sb.AppendLine();
                sb.AppendLine("Instance properties:");
                foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!prop.CanRead) continue;
                    try
                    {
                        object? val = prop.GetValue(obj);
                        sb.AppendLine($"- {prop.Name} ({prop.PropertyType.FullName}): {(val == null ? "<null>" : val.GetType().FullName)}");
                    }
                    catch (Exception pex)
                    {
                        sb.AppendLine($"- {prop.Name}: <error reading> {pex.Message}");
                    }
                }
            }
            catch (Exception outer)
            {
                sb.AppendLine("Failed to build full diagnostics: " + outer);
            }

            return sb.ToString();
        }
    }
}
