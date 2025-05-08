using System;
using System.Collections.ObjectModel;

namespace WPR.MonoGameCompability.GamerServices
{
    public class LeaderboardWriter
    {
        private LeaderboardEntry? _Entry;

        public LeaderboardWriter()
        {
            _Entry = new LeaderboardEntry();
        }

        public LeaderboardEntry GetLeaderboard(LeaderboardIdentity identity)
        {
            return _Entry;
        }
    }
}
