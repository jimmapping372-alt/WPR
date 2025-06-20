using Microsoft.Xna.Framework.GamerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using WPR.Common;
using WPR.WindowsCompability;




namespace Microsoft.Xna.Framework.GamerServices
{
    public class LeaderboardReader
    {
        private ReadOnlyCollection<LeaderboardEntry>? _Entries;
        //private static LeaderboardReader _LeaderboardReader;

        public ReadOnlyCollection<LeaderboardEntry>? Entries
        {
            get
            {
                return this._Entries;
            }
        }

        public bool CanPageDown
        {
            get
            {
                return true;//false;
            }
        }
        public bool CanPageUp
        {
            get
            {
                return true;// false;
            }
        }

        public LeaderboardReader()
        {
            _Entries = new ReadOnlyCollection<LeaderboardEntry>(new List<LeaderboardEntry>());
        }

        public LeaderboardReader(
         LeaderboardIdentity leaderboardId,
         Gamer pivotGamer,
         int pageSize) : base()
        {
            //_Entries = new ReadOnlyCollection<LeaderboardEntry>(new List<LeaderboardEntry>());
            //_LeaderboardReader = Read(leaderboardId, pivotGamer, pageSize);
        }



        public void Dispose()
        {
            // RnD
            //_Entries.Dispose();
            return;
        }

        public IAsyncResult BeginPageDown(AsyncCallback callback, object asyncState)
        {
            return StubUtils.ForeverTask;
        }
        public IAsyncResult BeginPageUp(AsyncCallback callback, object asyncState)
        {
            return StubUtils.ForeverTask;
        }

       

        //RnD
        public static LeaderboardReader Read(
         LeaderboardIdentity leaderboardId,
         Gamer pivotGamer,
         int pageSize)
        {
            return new LeaderboardReader(); //StubUtils.ForeverTask;
        }



        public static IAsyncResult BeginRead(LeaderboardIdentity leaderb,
            int pageStart, int pageSize, AsyncCallback callback, object asyncState)
        {
            return StubUtils.ForeverTask;
        }

      
        public static IAsyncResult BeginRead(
          LeaderboardIdentity leaderboardId,
          Gamer pivotGamer,
          int pageSize,
          AsyncCallback callback,
          object asyncState)
        {
            return StubUtils.ForeverTask;
        }

        public static IAsyncResult BeginRead(
          LeaderboardIdentity leaderboardId,
          IEnumerable<Gamer> gamers,
          Gamer pivotGamer,
          int pageSize,
          AsyncCallback callback,
          object asyncState)
        {
            return StubUtils.ForeverTask;
        }
               
    }
}
