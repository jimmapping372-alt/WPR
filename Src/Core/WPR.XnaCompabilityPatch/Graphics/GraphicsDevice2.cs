using Microsoft.Xna.Framework.Graphics;

namespace WPR.XnaCompability.Graphics
{
    public class GraphicsDevice2 : GraphicsDevice
    {
        DisplayMode _DisplayMode { get; set; }

        public GraphicsDevice2(GraphicsAdapter adapter, 
            GraphicsProfile graphicsProfile, PresentationParameters presentationParameters) 
            : base(adapter, graphicsProfile, presentationParameters)
        {
            _DisplayMode = new DisplayMode(800, 600, base.DisplayMode.Format); //RnD
        }

        
        public new DisplayMode DisplayMode
        {
            //RnD
            get
            {
                //return new DisplayMode(480, 800, base.DisplayMode.Format);
                //return new DisplayMode(800, 600, base.DisplayMode.Format);  //return new DisplayMode(640, 480, base.DisplayMode.Format);

                if (_DisplayMode == null)
                { 
                    return new DisplayMode(800, 600, base.DisplayMode.Format); 
                }
                return _DisplayMode;
            }

            set 
            {
                _DisplayMode = value;
            }
        }
    }
}
