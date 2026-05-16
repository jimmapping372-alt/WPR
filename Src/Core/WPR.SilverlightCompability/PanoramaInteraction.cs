using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WPR.SilverlightCompability
{
    /// <summary>
    /// Per-Panorama interaction state attached via a weak conditional map so we
    /// don't have to subclass <c>Microsoft.Phone.Controls.Panorama</c> (we can't —
    /// it's in the patched user-side DLL). Renderer reads <see cref="CurrentIndex"/>
    /// to decide which PanoramaItem to draw; the pointer pipeline updates it via
    /// <see cref="Advance"/> when a horizontal flick lands.
    /// </summary>
    internal sealed class PanoramaState
    {
        /// <summary>Currently-visible item index. Clamped to [0, count-1] in Advance.</summary>
        public int CurrentIndex;

        /// <summary>Horizontal drag offset in pixels — non-zero while the user
        /// is mid-drag. Renderer applies this as a translation so the swipe
        /// feels live; reset to 0 when the gesture ends.</summary>
        public double DragOffset;

        /// <summary>True while a pointer is down and the drag has crossed the
        /// tap-slop threshold horizontally — locks subsequent moves into the
        /// pan path rather than letting them dispatch as taps to children.</summary>
        public bool IsDragging;

        public void Advance(int delta, int childCount)
        {
            if (childCount <= 0) return;
            int next = CurrentIndex + delta;
            if (next < 0) next = 0;
            if (next >= childCount) next = childCount - 1;
            CurrentIndex = next;
            DragOffset = 0;
            IsDragging = false;
        }
    }

    /// <summary>
    /// Helper to keep the toolkit's <c>Panorama.SelectedItem</c> in sync with our
    /// own <see cref="PanoramaState.CurrentIndex"/>. WP7 user code routinely checks
    /// <c>if (panorama.SelectedItem == myItem)</c> to gate per-page actions
    /// (Minesweeper's leaderboard / achievement / help tap handlers all do this).
    /// Our renderer drives the visible item from PanoramaState alone, so without
    /// this bridge the toolkit's SelectedItem stays stuck on the first item
    /// regardless of swipes — and those gate checks all fail.
    /// </summary>
    internal static class PanoramaSelectedItemSync
    {
        // Cache per-runtime-type to handle Panorama and Pivot in the same process.
        private static readonly System.Collections.Generic.Dictionary<Type, FieldInfo?> _fieldByType = new();

        // One-shot diagnostic — log on the first successful Set per type so we can
        // see whether the sync actually ran and what got written.
        private static readonly System.Collections.Generic.HashSet<Type> _loggedTypes = new();

        /// <summary>
        /// Write <paramref name="item"/> to the panorama's SelectedItem dependency
        /// property via reflection (we can't reference Microsoft.Phone.Controls from
        /// the SilverlightCompability project). No-op if the field can't be located
        /// — leaves the toolkit's internal default in place.
        /// Also keeps the corresponding <c>SelectedIndex</c> DP in sync if present,
        /// because some user code gates on the index rather than the item.
        /// </summary>
        public static void Set(DependencyObject panorama, object? item)
        {
            if (panorama == null) return;
            Type panoType = panorama.GetType();
            FieldInfo? f = ResolveDpField(panoType, "SelectedItemProperty");
            if (f == null)
            {
                if (_loggedTypes.Add(panoType))
                {
                    Console.WriteLine(
                        $"[PanoramaSync] No SelectedItemProperty field found on '{panoType.FullName}' " +
                        "or any base. SelectedItem-gated handlers will mis-fire.");
                }
                return;
            }
            try
            {
                if (f.GetValue(null) is DependencyProperty dp)
                {
                    object? previous = panorama.GetValue(dp);
                    if (!Equals(previous, item))
                    {
                        panorama.SetValue(dp, item);
                        if (_loggedTypes.Add(panoType))
                        {
                            object? readback = panorama.GetValue(dp);
                            Console.WriteLine(
                                $"[PanoramaSync] Set SelectedItem on {panoType.Name} -> " +
                                $"{item?.GetType().Name ?? "null"} (readback={readback?.GetType().Name ?? "null"}, " +
                                $"refEq={ReferenceEquals(readback, item)})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PanoramaSync] SetValue threw: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static FieldInfo? ResolveDpField(Type panoramaType, string fieldName)
        {
            if (_fieldByType.TryGetValue(panoramaType, out var cached)) return cached;

            // Walk the type chain; SelectedItemProperty may live on a base
            // (TemplatedItemsControl<T>) rather than Panorama itself.
            FieldInfo? found = null;
            for (Type? t = panoramaType; t != null; t = t.BaseType)
            {
                FieldInfo? f = t.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (f != null) { found = f; break; }
            }
            _fieldByType[panoramaType] = found;
            return found;
        }
    }

    /// <summary>Static side-table: <c>Panorama instance → its PanoramaState</c>.
    /// Lives until the Panorama is GC'd.</summary>
    internal static class PanoramaStateTable
    {
        private static readonly ConditionalWeakTable<object, PanoramaState> _map = new();

        public static PanoramaState GetOrCreate(object panorama)
            => _map.GetValue(panorama, _ => new PanoramaState());

        public static PanoramaState? TryGet(object panorama)
            => _map.TryGetValue(panorama, out var s) ? s : null;
    }
}
