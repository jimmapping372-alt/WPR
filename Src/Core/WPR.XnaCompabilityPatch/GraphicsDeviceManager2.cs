using Microsoft.Xna.Framework;
using System;

namespace WPR.XnaCompability
{
    public class GraphicsDeviceManager2 : GraphicsDeviceManager
    {
        public static Action<DisplayOrientation>? RequestOrientation;

        // WP7 phones had a fixed 800x480 hardware surface. Games like Zuma's Revenge
        // request a larger preferred backbuffer (1066x640 in the Nokia build) but
        // hardcode their internal viewport to 800x480, so on a real phone the OS
        // clamped the surface and content filled the screen; on FNA's desktop SDL
        // window the larger backbuffer is honored literally and the game renders
        // into the upper-left 800x480 of an oversized window. Mirror the phone
        // clamp here so requested buffers never exceed the phone surface.
        private const int PhoneLongDim = 800;
        private const int PhoneShortDim = 480;

        public GraphicsDeviceManager2(Game game)
            : base(game)
        {

        }

#if !__MOBILE__
        public new bool IsFullScreen
        {
            get => false;
            set => base.IsFullScreen = false;
        }
#endif

        public new int PreferredBackBufferWidth
        {
            get => base.PreferredBackBufferWidth;
            set => base.PreferredBackBufferWidth = ClampToPhoneSurface(value, base.PreferredBackBufferHeight);
        }

        public new int PreferredBackBufferHeight
        {
            get => base.PreferredBackBufferHeight;
            set => base.PreferredBackBufferHeight = ClampToPhoneSurface(value, base.PreferredBackBufferWidth);
        }

        private static int ClampToPhoneSurface(int requested, int otherDim)
        {
            int max = requested >= otherDim ? PhoneLongDim : PhoneShortDim;
            return requested > max ? max : requested;
        }

        public new void ApplyChanges()
        {
            base.ApplyChanges();
            RequestOrientationChange(PreferredBackBufferWidth, PreferredBackBufferHeight);
        }

        public static void RequestOrientationChange(int width, int height)
        {
            DisplayOrientation device_orientation = default;

            if (width > height)
            {
                device_orientation = DisplayOrientation.LandscapeRight;
            }
            else
            {
                device_orientation = DisplayOrientation.Portrait;
            }
            RequestOrientation?.Invoke(device_orientation);
        }
    }
}
