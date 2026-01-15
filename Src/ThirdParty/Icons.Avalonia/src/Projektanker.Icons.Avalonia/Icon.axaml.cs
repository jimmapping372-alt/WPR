using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;

namespace Projektanker.Icons.Avalonia
{
    public partial class Icon : TemplatedControl
    {
        public static readonly DirectProperty<Icon, DrawingImage> DrawingImageProperty =
            AvaloniaProperty.RegisterDirect<Icon, DrawingImage>(nameof(DrawingImage), o => o.DrawingImage);

        public static readonly StyledProperty<string> ValueProperty =
            AvaloniaProperty.Register<Icon, string>(nameof(Value));

        public static readonly StyledProperty<IconAnimation> AnimationProperty =
            AvaloniaProperty.Register<Icon, IconAnimation>(nameof(Animation));

        private DrawingImage _drawingImage = default!;

        static Icon()
        {
            // Use AddClassHandler to respond to property changes in a version-safe way
            ValueProperty.Changed.AddClassHandler<Icon>((icon, e) => icon.OnValueChanged());
            ForegroundProperty.Changed.AddClassHandler<Icon>((icon, e) => icon.OnForegroundChanged());
        }

        public DrawingImage DrawingImage
        {
            get => _drawingImage;
            private set => SetAndRaise(DrawingImageProperty, ref _drawingImage, value);
        }

        public string Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public IconAnimation Animation
        {
            get => GetValue(AnimationProperty);
            set => SetValue(AnimationProperty, value);
        }

        private void OnValueChanged()
        {
            object? iconProviderObj = null;

            // Try several locator access patterns via reflection for compatibility across Avalonia versions
            try
            {
                var avaloniaLocatorType = typeof(AvaloniaLocator);
                var currentProp = avaloniaLocatorType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (currentProp == null)
                    currentProp = avaloniaLocatorType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (currentProp != null)
                {
                    var locator = currentProp.GetValue(null);
                    if (locator != null)
                    {
                        var getServiceMethod = locator.GetType().GetMethod("GetService", new Type[] { typeof(Type) });
                        if (getServiceMethod != null)
                        {
                            iconProviderObj = getServiceMethod.Invoke(locator, new object[] { typeof(IIconReader) });
                        }
                    }
                }
            }
            catch
            {
                // swallow - fallback handled below
            }

            if (iconProviderObj is not IIconReader iconProvider)
            {
                // No provider available; nothing to do
                return;
            }

            string path = iconProvider.GetIconPath(Value);
            GeometryDrawing drawing = new GeometryDrawing()
            {
                Geometry = Geometry.Parse(path),
                Brush = Foreground ?? Brushes.Black,
            };

            DrawingImage = new DrawingImage { Drawing = drawing };
        }

        private void OnForegroundChanged()
        {
            if (DrawingImage?.Drawing is GeometryDrawing geometryDrawing)
            {
                DrawingImage.Drawing = new GeometryDrawing
                {
                    Geometry = geometryDrawing.Geometry,
                    Brush = Foreground,
                };
            }
        }
    }
}