using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using WPR.Common;

namespace Microsoft.Xna.Framework.GamerServices
{
    public class LeaderboardReader : IDisposable
    {
        private ReadOnlyCollection<LeaderboardEntry>? _Entries;
        public ReadOnlyCollection<LeaderboardEntry>? Entries => this._Entries;

        public LeaderboardReader()
        {
            _Entries = new ReadOnlyCollection<LeaderboardEntry>(new List<LeaderboardEntry>());
        }

        // Empty-leaderboard stub surface. The game treats this reader as "no scores
        // available": no further pages, nothing to sync, an empty entries collection.
        public bool CanPageDown => false;
        public bool CanPageUp => false;
        public bool IsDisposed { get; private set; }
        public bool IsSynchronizedWithLiveServer => false;
        public LeaderboardIdentity Leaderboard { get; internal set; }
        public int PageSize => 0;
        public int PageStart => 0;
        public int TotalLeaderboardSize => 0;

        public IAsyncResult BeginPageDown(AsyncCallback callback, object asyncState)
        {
            return StubUtils.ForeverTask;
        }
        public IAsyncResult BeginPageUp(AsyncCallback callback, object asyncState)
        {
            return StubUtils.ForeverTask;
        }
        public static IAsyncResult BeginRead(LeaderboardIdentity leaderb,
            int pageStart, int pageSize, AsyncCallback callback, object asyncState)
        {
            return CompleteRead(callback, asyncState);
        }

        public static IAsyncResult BeginRead(
          LeaderboardIdentity leaderboardId,
          Gamer pivotGamer,
          int pageSize,
          AsyncCallback callback,
          object asyncState)
        {
            return CompleteRead(callback, asyncState);
        }

        public static IAsyncResult BeginRead(
          LeaderboardIdentity leaderboardId,
          IEnumerable<Gamer> gamers,
          Gamer pivotGamer,
          int pageSize,
          AsyncCallback callback,
          object asyncState)
        {
            return CompleteRead(callback, asyncState);
        }

        private static IAsyncResult CompleteRead(AsyncCallback? callback, object? asyncState)
        {
            var reader = new LeaderboardReader();
            var task = Task.FromResult(reader);
            callback?.Invoke(task);
            return task;
        }

        public static LeaderboardReader EndRead(IAsyncResult result)
        {
            return ((Task<LeaderboardReader>)result).GetAwaiter().GetResult();
        }

        public void EndPageDown(IAsyncResult result) { }
        public void EndPageUp(IAsyncResult result) { }

        public void PageDown() { }
        public void PageUp() { }

        public void Dispose() { IsDisposed = true; }
    }
}
