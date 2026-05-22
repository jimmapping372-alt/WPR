using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace WPR.WindowsCompability
{

    public abstract class Type2
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type? GetType(string typeName, bool throwOnError)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException("Type name is null!");
            }

            var stuffs = typeName.Split(',');
            if (stuffs.Length >= 2)
            {
                bool patched = false;
                for (int i = 1; i < stuffs.Length; i += 4)
                {
                    if (stuffs[i].Contains("Microsoft.Xna.Framework"))
                    {
                        if (!stuffs[i].Equals("Microsoft.Xna.Framework.GamerServices"))
                        {
                            stuffs[i] = "FNA";
                            patched = true;
                        }
                    }
                }
                if (patched)
                {
                    typeName = stuffs[0];
                    for (int i = 1; i < stuffs.Length; i += 4)
                    {
                        typeName += $", {stuffs[i]}";
                    }
                }
            }

            // If the caller is in a collectible ALC (the per-launch user ALC set up by
            // ApplicationLaunch.Start) and the type is assembly-qualified, resolve the
            // assembly through that ALC directly. Falling through to Type.GetType makes
            // the CLR use this method's assembly (WPR.WindowsCompability — non-collectible,
            // Default ALC) as the requesting assembly, and the binder then rejects loading
            // the user assembly with "A non-collectible assembly may not reference a
            // collectible assembly" (FileLoadException 0x80131515). Asteroids Deluxe hits
            // this from Krome.GameRoom.UI.Screens.InGameScreenData.
            int commaIdx = typeName.IndexOf(',');
            if (commaIdx >= 0)
            {
                AssemblyLoadContext? callerAlc =
                    AssemblyLoadContext.GetLoadContext(Assembly.GetCallingAssembly());
                if (callerAlc != null && callerAlc != AssemblyLoadContext.Default)
                {
                    string typeOnly = typeName.Substring(0, commaIdx).Trim();
                    string asmSimpleName = typeName.Substring(commaIdx + 1).Trim().Split(',')[0].Trim();

                    foreach (var asm in callerAlc.Assemblies)
                    {
                        if (string.Equals(asm.GetName().Name, asmSimpleName, StringComparison.OrdinalIgnoreCase))
                        {
                            var t = asm.GetType(typeOnly, throwOnError: false);
                            if (t != null) return t;
                            break;
                        }
                    }
                }
            }

            return Type.GetType(typeName, throwOnError);
        }
    }

    //RnD
    /*
    public abstract class WritableBitmap
    {
        public static Type? GetType(string typeName, bool throwOnError)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException("Type name is null!");
            }

            var stuffs = typeName.Split(',');
            if (stuffs.Length >= 2)
            {
                bool patched = false;
                for (int i = 1; i < stuffs.Length; i += 4)
                {
                    if (stuffs[i].Contains("Microsoft.Xna.Framework"))
                    {
                        if (!stuffs[i].Equals("Microsoft.Xna.Framework.GamerServices"))
                        {
                            stuffs[i] = "FNA";
                            patched = true;
                        }
                    }
                }
                if (patched)
                {
                    typeName = stuffs[0];
                    for (int i = 1; i < stuffs.Length; i += 4)
                    {
                        typeName += $", {stuffs[i]}";
                    }
                }
            }

            return Type.GetType(typeName);
        }
    }
    */
}
