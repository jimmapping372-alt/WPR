using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Phone.Shell
{
    public class ShellTileData
    {
        public static List<ShellTileData> _ShellTileData;
        public static ShellTileData data = new ShellTileData();

        public string Name;
        public bool IsEnabled;

        /*public string Title 
        {
            get 
            {
                if (_ShellTileData == null)
                {
                    _ShellTileData = new List<ShellTileData>()
                    {
                        new ShellTileData()
                        {
                            Name = "Test",
                            IsEnabled = true
                        }
                    };
                }

                return _ShellTileData[0].Title; 
            }
            //set 
            //{
            //    //_ShellTileData[0].Title = value;
            //    ShellTile.tile.Title = data.Title;
            //} 
        }*/

        static ShellTileData()
        {
            _ShellTileData = new List<ShellTileData>()
            {
                new ShellTileData() 
                {
                    Name = "Test",
                    IsEnabled = true
                }
            };

            data = _ShellTileData[0];
        }

        //RnD
        public void set_Title(string text)
        {
            ShellTile.tile.Title = text;
            Debug.WriteLine("[i] ShellTileData set_Title text : " + text);
        }


        //RnD
        /*public void set_Title(ShellTileData data)
        {
            //RnD
            //ShellTile.tile.Title = data;
            Debug.WriteLine("[i] ShellTileData set_Title data : " + data);
        }*/
    }
}
