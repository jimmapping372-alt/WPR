using Microsoft.Xna.Framework.Graphics;

namespace WPR.XnaCompability.Graphics
{
    public class GraphicsAdapter2 : GraphicsAdapter
    {
        DisplayMode _CurrentDisplayMode { get; set; }
        public GraphicsAdapter2(
            DisplayModeCollection modes,
            string name,
            string description
        )
            : base(modes, name, description)
        {
            _CurrentDisplayMode = new DisplayMode(800, 600, CurrentDisplayMode.Format); //RnD
        }

        //RnD
        /*public DisplayMode get_CurrentDisplayMode()
        {
            //return new DisplayMode(480, 800, CurrentDisplayMode.Format);
            return new DisplayMode(800, 600, CurrentDisplayMode.Format);
            //return new DisplayMode(640, 480, CurrentDisplayMode.Format);
        }*/
        public new DisplayMode CurrentDisplayMode
        {
            //RnD
            get
            {
                //return new DisplayMode(480, 800, base.DisplayMode.Format);
                //return new DisplayMode(800, 600, base.DisplayMode.Format);  //return new DisplayMode(640, 480, base.DisplayMode.Format);

                if (_CurrentDisplayMode == null)
                {
                    return new DisplayMode(800, 600, CurrentDisplayMode.Format);
                }
                return _CurrentDisplayMode;
            }

            set
            {
                _CurrentDisplayMode = value;
            }
        }
    }
}
