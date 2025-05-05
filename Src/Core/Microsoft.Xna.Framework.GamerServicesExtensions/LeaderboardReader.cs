using Microsoft.Xna.Framework.GamerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using WPR.Common;

namespace Microsoft.Xna.Framework.GamerServicesExt
{
    public class LeaderboardReader
    {
        private ReadOnlyCollection<LeaderboardEntry>? _Entries;
        public ReadOnlyCollection<LeaderboardEntry>? Entries
        {
            get
            {
                return this._Entries;
            }
        }

        public LeaderboardReader()
        {
            _Entries = new ReadOnlyCollection<LeaderboardEntry>(new List<LeaderboardEntry>());
        }

        public void Dispose()
        {
            // RnD
            //_Entries.Dispose();
            return;
        }

        public static IAsyncResult BeginPageDown(AsyncCallback callback, object asyncState)
        {
            return StubUtils.ForeverTask;
        }
        
        public static IAsyncResult BeginPageUp(AsyncCallback callback, object asyncState)
        {
            return StubUtils.ForeverTask;
        }

        //RnD
        public static IAsyncResult Read(
         LeaderboardIdentity leaderboardId,
         Gamer1 pivotGamer,
         int pageSize)
        {
            return StubUtils.ForeverTask;
        }

        public static IAsyncResult BeginRead(LeaderboardIdentity leaderb,
            int pageStart, int pageSize, AsyncCallback callback, object asyncState)
        {
            return StubUtils.ForeverTask;
        }

        public static IAsyncResult BeginRead(
          LeaderboardIdentity leaderboardId,
          Gamer1 pivotGamer,
          int pageSize,
          AsyncCallback callback,
          object asyncState)
        {
            return StubUtils.ForeverTask;
        }

        public static IAsyncResult BeginRead(
          LeaderboardIdentity leaderboardId,
          IEnumerable<Gamer1> gamers,
          Gamer1 pivotGamer,
          int pageSize,
          AsyncCallback callback,
          object asyncState)
        {
            return StubUtils.ForeverTask;
        }
    }
}
