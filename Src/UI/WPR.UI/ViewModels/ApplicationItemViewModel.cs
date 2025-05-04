using Avalonia.Media.Imaging;
using System.IO;

using WPR.Models;
using WPR.Common;

namespace WPR.UI.ViewModels
{
    public class ApplicationItemViewModel : ViewModelBase
    {
        private Application _App;
        private Bitmap? _Icon;

        public int IconSize => 90;
        public int Height => 160;

        public ApplicationItemViewModel(Application app)
        {
            _App = app;
        }

        internal Application App => _App;

        public string? Name => _App.Name;
        public string? Tooltip
        {
            get
            {
                return (_App.Description.Length == 0) ? _App.Name : $"{_App.Name}\n\n{_App.Description}";
            }
        }

        public Bitmap Icon
        {
            get
            {
                _Icon = default;

                if (_Icon == null)
                {
                    try
                    {
                        var iconpath = Configuration.Current!.DataPath(_App.IconPath);
                        var fs = new FileStream(iconpath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        _Icon = Bitmap.DecodeToWidth(fs,
                            IconSize);
                    }
                    catch { }
                }

                return _Icon;
            }
        }
    }
}
