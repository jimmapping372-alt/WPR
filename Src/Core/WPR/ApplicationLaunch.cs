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

        static ApplicationLaunch()
        {
            AssemblyLoadContext.Default.Resolving += (loadContext, name) =>
            {
                return loadContext.LoadFromAssemblyPath(Path.Combine(CurrentProductFolder, name.Name + ".dll"));
            };
        }

        public static async Task Start(Application app, Action<DisplayOrientation>? requestOrientation = null)
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
            TextWriterTraceListener? debugListener = null;
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
                Trace.WriteLine("[wpr-trace] ApplicationLaunch: trace listener attached (smoke test)");
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppList, $"Could not attach debug log listener: {ex.Message}");
            }

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
                if (File.Exists(candidate)) return ctx.LoadFromAssemblyPath(candidate);

                // Fall back to name-only load — handles WinMD-projected refs like
                // `mscorlib v=255.255.255.255` that the strict version binder rejects.
                try
                {
                    return AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(name.Name!));
                }
                catch
                {
                    return null;
                }
            };
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

                using (Game? obj = Activator.CreateInstance(mainType!) as Game)
                {
                    CurrentGame = obj;
                    obj!.IsMouseVisible = true;
                    obj!.Window.Title = $"{app.Name} - {app.Author} (Publisher: {app.Publisher})";

                    // Re-affirm in case the user game's ctor reset these (it shouldn't, but
                    // the cost is one static field write).
#if !__MOBILE__
                    TouchPanel.MouseAsTouch = true;
#endif

                    GraphicsDeviceManager2.RequestOrientation = requestOrientation;
                    SignedInGamer.Reset();

                    Trace.WriteLine("[wpr-trace] ApplicationLaunch: subscribing to obj.Activated");
                    obj.Activated += (obj, args) =>
                    {
                        Trace.WriteLine("[wpr-trace] ApplicationLaunch: obj.Activated fired -> HandleApplicationStart(true)");
                        PhoneApplicationService.Current!.HandleApplicationStart(true);
                    };

                    // FNA only raises Game.Activated when the SDL window receives a focus event.
                    // On some setups (the Avalonia host already had focus, or SDL didn't deliver
                    // the focus-gained event for a freshly-created game window) the event never
                    // fires and the WP7 lifecycle callbacks (Launching / Activated) never run —
                    // the game then sits on a "not initialised" black screen because its
                    // Application_Launching handler is where it builds its scene graph.
                    // Fire the lifecycle once, unconditionally, right at startup as a safety net.
                    Trace.WriteLine("[wpr-trace] ApplicationLaunch: priming PhoneApplicationService.HandleApplicationStart(true) before Game.Run");
                    try
                    {
                        PhoneApplicationService.Current!.HandleApplicationStart(true);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("[wpr-ex] HandleApplicationStart priming threw: " + ex);
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
                        Trace.WriteLine("[wpr-trace] ApplicationLaunch: about to call Game.Run()");
                        // Run the game and capture any exceptions to produce richer diagnostics.
                        try
                        {
                            obj.Run();
                            Trace.WriteLine("[wpr-trace] ApplicationLaunch: Game.Run() returned normally");
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

                        // Tear down audio so songs don't keep playing after the game window
                        // closes. Game.Dispose only disposes Components / Content / GraphicsDevice
                        // / Window — it never stops MediaPlayer (a static singleton) or releases
                        // FAudio's master voice. Without this, MediaPlayer.Play(Song) leaves
                        // FAudio's audio thread alive after the game exits and music continues.
                        TeardownAudioState();
                    }
                }

                // Drop user-assembly references then unload the ALC so the .dll is freed and any
                // statics inside the user assembly (timers, caches) can be reclaimed.
                try
                {
                    PhoneApplicationService.Current?.HandleApplicationExit();
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
        /// keep playing after <see cref="Game.Dispose"/>. Two layers:
        /// <list type="bullet">
        ///   <item><description><see cref="Microsoft.Xna.Framework.Media.MediaPlayer.Stop"/> halts
        ///     the active song's FAudio source voice (XNA_StopSong).</description></item>
        ///   <item><description>Reflective dispose of FNA's internal <c>FAudioContext.Context</c>
        ///     singleton releases the master voice and the FAudio engine. FNA never disposes
        ///     this on its own — it's a static and games don't own it. Without releasing it,
        ///     FAudio's native audio thread keeps the audio device open even after the game
        ///     exits, and any future game session creates a fresh context instead of reusing.
        ///     Reflection avoids touching the vendored FNA copy.</description></item>
        /// </list>
        /// All best-effort: any failure is logged and swallowed because we're already on the
        /// teardown path.
        /// </summary>
        private static void TeardownAudioState()
        {
            try
            {
                Microsoft.Xna.Framework.Media.MediaPlayer.Stop();
            }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppList, $"MediaPlayer.Stop on teardown threw: {ex.Message}");
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
