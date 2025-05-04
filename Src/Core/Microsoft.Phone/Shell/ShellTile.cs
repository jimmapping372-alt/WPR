using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Phone.Shell
{
    public class ShellTile
    {
        public static List<ShellTile> _ActiveTiles;
        public static ShellTile tile = new ShellTile();
        public string Name;
        public string Title;
        public bool IsEnabled;

        static ShellTile()
        {
            // _ActiveTiles = new List<ShellTile>()
            // {
            //
            // };
            _ActiveTiles = new List<ShellTile>()
            {
                new ShellTile { Name = "HomeTile", Title = "123", IsEnabled = true },
                new ShellTile { Name = "WeatherTile", Title = "123", IsEnabled = false },
                new ShellTile { Name = "NewsTile", Title = "123", IsEnabled = true }
            };
        }

        public static IEnumerable<ShellTile> ActiveTiles
        {
            get
            {
                return _ActiveTiles;
            }
        }

        public Uri NavigationUri { get; private set; }

      
        public void Update(ShellTileData data)
        {
            tile =  ShellTile.ActiveTiles.FirstOrDefault();
            //if (tile != null)
            //{
            //    tile.Update(data);
            //}
        }
    }
}
