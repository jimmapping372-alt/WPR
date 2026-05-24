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

            // If the type is assembly-qualified, search every loaded ALC for an assembly
            // matching the simple name and look the type up via Assembly.GetType — a pure
            // managed lookup that doesn't trigger the CLR's cross-ALC collectibility
            // check. Falling through to Type.GetType has the binder treat *some* assembly
            // up the stack as the "requesting assembly":
            //
            //  - When the caller is the main user DLL (collectible userAlc), Type.GetType
            //    sees this method's assembly (WPR.WindowsCompability — non-collectible,
            //    Default ALC) and rejects loading the user assembly back into Default ALC.
            //  - When the caller is a SIBLING library DLL (e.g. Krome.dll, loaded into
            //    Default ALC by design — see ApplicationLaunch.cs static ctor), and the
            //    target type lives in the main collectible user DLL (e.g. AsteroidsDeluxe),
            //    Type.GetType again routes through Default ALC's resolver, which returns
            //    the userAlc-loaded assembly, and the CLR rejects the resulting Default→
            //    userAlc reference. The Krome→AsteroidsDeluxe crash is this case.
            //
            // Searching AssemblyLoadContext.All catches both: we hand back the right
            // Assembly object and call .GetType on it directly, bypassing the binder.
            int commaIdx = typeName.IndexOf(',');
            if (commaIdx >= 0)
            {
                string typeOnly = typeName.Substring(0, commaIdx).Trim();
                string asmSimpleName = typeName.Substring(commaIdx + 1).Trim().Split(',')[0].Trim();

                foreach (var alc in AssemblyLoadContext.All)
                {
                    foreach (var asm in alc.Assemblies)
                    {
                        if (string.Equals(asm.GetName().Name, asmSimpleName, StringComparison.OrdinalIgnoreCase))
                        {
                            var t = asm.GetType(typeOnly, throwOnError: false);
                            if (t != null) return t;
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
