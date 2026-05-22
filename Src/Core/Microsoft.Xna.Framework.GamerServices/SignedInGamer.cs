using WPR.WindowsCompability;
using WPR.Common;

using Microsoft.EntityFrameworkCore;

using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Diagnostics;

namespace Microsoft.Xna.Framework.GamerServices
{
    public sealed class SignedInGamer : Gamer
    {
        private const int DelaySignedInMillis = 2000;
        private static bool FirstSignInSessionDone = false;

        /// <summary>
        /// Serialises invocation of SignedIn handlers. Games like Tentacles register
        /// the same callback twice from <c>Game1.Initialize</c>; without this gate
        /// the two <see cref="Task.Delay"/> continuations fire on parallel threadpool
        /// threads and both call into <see cref="Gamer.GetProfile"/>, which races on
        /// the shared <see cref="AchievementContext"/> DbContext singleton and trips
        /// EF Core's ConcurrencyDetector. Serialising the handler invocations keeps
        /// the user-visible semantics (handlers fire ~2s after the first <c>+=</c>)
        /// without requiring a per-call DbContext rewrite.
        /// </summary>
        private static readonly SemaphoreSlim _SignInGate = new SemaphoreSlim(1, 1);

        private PlayerIndex _PlayerIndex;

        private GamerPrivileges _GamerPrivileges = new GamerPrivileges();

        private GamerPresence _GamerPresence = new GamerPresence();

        public event EventHandler<EventArgs> AvatarChanged;

        public static void Reset()
        {
            FirstSignInSessionDone = false;
        }

        public static event EventHandler<SignedInEventArgs> SignedIn
        {
            add
            {
#if DEBUG
                Trace.WriteLine($"[wpr-trace] SignedInGamer.SignedIn += handler (FirstSignInSessionDone={FirstSignInSessionDone}, value={(value == null ? "null" : "set")})");
#endif
                if (value == null) return;

                if (FirstSignInSessionDone)
                {
#if DEBUG
                    Trace.WriteLine("[wpr-trace] SignedInGamer.SignedIn: firing immediately (already signed in)");
#endif
                    value.Invoke(null, new SignedInEventArgs(_SignedInGamers[0]));
                    return;
                }

#if DEBUG
                Trace.WriteLine($"[wpr-trace] SignedInGamer.SignedIn: scheduling Task.Delay({DelaySignedInMillis}ms) → serialised invoke");
#endif
                // Both halves of the work go on a Task.Run so we never block the caller
                // of `+=`. The semaphore (acquired AFTER the delay) ensures that if N
                // handlers register, they each get their ~2s delay in parallel but their
                // synchronous Invoke runs serially — preventing the GetProfile→DbContext
                // race described on _SignInGate above.
                _ = Task.Run(async () =>
                {
                    await Task.Delay(DelaySignedInMillis).ConfigureAwait(false);
                    await _SignInGate.WaitAsync().ConfigureAwait(false);
                    try
                    {
#if DEBUG
                        Trace.WriteLine("[wpr-trace] SignedInGamer.SignedIn: gate acquired, firing handler");
#endif
                        FirstSignInSessionDone = true;
                        //TODO: handle multiple signed in gamers
                        value.Invoke(null, new SignedInEventArgs(_SignedInGamers[0]));
#if DEBUG
                        Trace.WriteLine("[wpr-trace] SignedInGamer.SignedIn: handler returned normally");
#endif
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Trace.WriteLine("[wpr-ex] SignedInGamer.SignedIn handler threw: " + ex);
#endif
                        Debug.WriteLine("[ex] SignedInGamer exception: " + ex.Message);
                    }
                    finally
                    {
                        _SignInGate.Release();
                    }
                });
            }
            remove
            {
            }
        }

        public static event EventHandler<SignedOutEventArgs> SignedOut;

        internal SignedInGamer()
        {
        }

        public IAsyncResult BeginGetAchievements(AsyncCallback? callback, Object? asyncState)
        {
            return Task.Run(async () =>
            {
                List<Achievement> achievementStored = await AchievementContext.Current!.Achievements!
                    .Where(x => x.OwnProductId == Application.Current.ProductId)
                    .ToListAsync();

                Trace.WriteLine($"[wpr-trace] BeginGetAchievements: {achievementStored.Count} rows for {Application.Current.ProductId}");

                if (achievementStored.Count == 0)
                {
                    AchievementCollection collection = await TrueAchievements.Scraper.QueryAchievements(Application.Current!.ProductId!);
                    if (collection.Count != 0)
                    {
                        await AchievementContext.Current!.Achievements!.AddRangeAsync(collection.ToArray());
                        await AchievementContext.Current!.SaveChangesAsync();
                    }

                    if (callback != null)
                    {
                        var compSource = new TaskCompletionSource<AchievementCollection>(asyncState);
                        compSource.SetResult(collection);

                        callback(compSource.Task);
                    }

                    return collection;
                }

                AchievementCollection coll = new AchievementCollection();
                foreach (Achievement achiQueried in achievementStored)
                {
                    coll.Add(achiQueried);
                }

                var completeSource = new TaskCompletionSource<AchievementCollection>(asyncState);
                completeSource.SetResult(coll);

                if (callback != null)
                {
                    callback(completeSource.Task);
                }

                return coll;
            });
        }

        public AchievementCollection EndGetAchievements(IAsyncResult result)
        {
            Log.Error(LogCategory.GamerServices, "Result!");
            Task<AchievementCollection>? collectResult = result as Task<AchievementCollection>;
            return collectResult!.GetAwaiter().GetResult();
        }

        public AchievementCollection GetAchievements() => this.EndGetAchievements(this.BeginGetAchievements(null, null));

        public IAsyncResult BeginAwardAchievement(string achievementKey, AsyncCallback callback,
            object state)
        {
            return Task.Run(async () =>
            {
                string productId = Application.Current.ProductId;
                List<Achievement> achievements = await AchievementContext.Current!.Achievements!
                    .Where(x => (x.OwnProductId == productId) && (x.Key == achievementKey))
                    .ToListAsync();

                if (achievements.Count > 1)
                {
                    Log.Warn(LogCategory.GamerServices, $"More then two achievements with key {achievementKey} exists!");
                }

                if (achievements.Count == 0)
                {
                    /* Diagnostic: AwardAchievement fired but we have no matching row
                     * to flip. Two common reasons:
                     *   1) The install-time XnaAchievementSeeder couldn't reach
                     *      TrueAchievements (no internet) or that game has no
                     *      mapping in Database/TrueAchievements/ProductIdUrl.json,
                     *      so the catalogue was never seeded.
                     *   2) The scraper persisted the achievement under its
                     *      TrueAchievements DISPLAY NAME, but the game is
                     *      calling AwardAchievement with the INTERNAL KEY (the
                     *      game's own constant). The display→internal map lives
                     *      in Database/TrueAchievements/AchievementsNameToKey.json
                     *      and may be missing an entry for this product.
                     * Either way: log enough to debug. The user gets no notification
                     * (there's nothing to look up an icon/name for), but the call
                     * doesn't crash either.
                     */
                    int rowsForProduct = await AchievementContext.Current.Achievements!
                        .CountAsync(x => x.OwnProductId == productId);
                    Log.Warn(LogCategory.GamerServices,
                        $"AwardAchievement: no DB row for product '{productId}' key '{achievementKey}'. " +
                        $"{rowsForProduct} achievement(s) seeded for this product. " +
                        "Check that the game's internal key matches the seeded Key column " +
                        "(see Database/TrueAchievements/AchievementsNameToKey.json).");
                }

                if (achievements.Count != 0)
                {
                    foreach (Achievement achievement in achievements)
                    {
                        if (achievement.IsEarned)
                        {
                            continue;
                        }

                        achievement.IsEarned = true;
                        achievement.EarnedOnline = true;
                        achievement.EarnedDateTime = DateTime.Now;
                    }

                    try
                    {
                        await NativeUI.NotificationManager.ShowNotification(new DesktopNotifications.Notification()
                        {
                            Title = Properties.Resources.AchievementUnlocked,
                            Body = $"{achievements[0].GamerScore}G - {achievements[0].Name}",
                            ImagePath = Configuration.Current!.DataPath(achievements[0]._IconPath),
                            SoundUri = "AchievementUnlocked"
                        }, DateTime.Now + TimeSpan.FromDays(1));
                    } catch (Exception ex)
                    {
                        Log.Error(LogCategory.GamerServices, $"Fail to display Achievement notification with exception:\n {ex}");
                    }
                }

                await AchievementContext.Current!.SaveChangesAsync();

                if (callback != null)
                {
                    TaskCompletionSource source = new TaskCompletionSource(state);
                    source.SetResult();

                    callback(source.Task);
                }

                return Task.CompletedTask;
            });
        }

        public void EndAwardAchievement(IAsyncResult result)
        {
        }

        public void AwardAchievement(string achievementKey) => EndAwardAchievement(BeginAwardAchievement(achievementKey, null, null));

        public FriendCollection GetFriends()
        {
            throw new NotImplementedException();
        }

        public bool IsFriend(Gamer gamer)
        {
            throw new NotImplementedException();
        }
        public AvatarDescription Avatar
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public GameDefaults GameDefaults
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsGuest => false;

        public bool IsSignedInToLive
        {
            get
            {
                return true;
            }
        }

        public int PartySize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public PlayerIndex PlayerIndex
        {
            get => _PlayerIndex;
            set => _PlayerIndex = value;
        }

        public GamerPresence Presence
        {
            get => _GamerPresence;
            set => _GamerPresence = value;
        }

        public GamerPrivileges Privileges
        {            
            get => _GamerPrivileges;
            set => _GamerPrivileges = value;
        }
    }
   
}
