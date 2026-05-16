using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

using WPR.Common;

namespace WPR
{
    /// <summary>
    /// Some of the user's app DLLs reference WinRT types from system assemblies (e.g.
    /// <c>Windows.Phone.Graphics.Interop.IDrawingSurfaceBackgroundContentProvider</c> is in
    /// <c>Windows.dll</c> on a phone but absent on regular .NET 8). Without those types the JIT
    /// can't load methods that reference them — even if we never actually invoke anything on
    /// the type.
    ///
    /// This synthesizer scans every .dll in the install folder for unresolvable external
    /// TypeRefs whose namespace is <c>Windows.*</c> and emits a tiny stub assembly per missing
    /// scope, containing empty type shells so the loader has *something* to bind against.
    ///
    /// Shape inference: we walk every DLL's <see cref="InterfaceImplementation"/>s and treat
    /// any TypeRef listed there as an interface. Other types get the default class shape. The
    /// "I"+UpperCase naming convention is used as a tiebreaker for ambiguous types we never see
    /// in interface-impl position.
    /// </summary>
    public static class WindowsTypeSynthesizer
    {
        public static void SynthesizeIfNeeded(string installFolder)
        {
            string[] dlls;
            try { dlls = Directory.GetFiles(installFolder, "*.dll"); }
            catch (Exception ex)
            {
                Log.Warn(LogCategory.AppInstall, $"WindowsTypeSynthesizer: enumerate failed: {ex}");
                return;
            }

            // Group missing types by their declaring assembly (Cecil "Scope" name).
            // scope name → set of (namespace, name) tuples
            var missingByScope = new Dictionary<string, HashSet<(string Ns, string Name)>>(StringComparer.Ordinal);

            // FullName ("Ns.Name") of every Windows.* type used as an interface impl anywhere.
            var interfaceTypes = new HashSet<string>(StringComparer.Ordinal);

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(installFolder);
            var rp = new ReaderParameters { AssemblyResolver = resolver };

            foreach (var dllPath in dlls)
            {
                AssemblyDefinition? asm = null;
                try { asm = AssemblyDefinition.ReadAssembly(dllPath, rp); }
                catch { continue; /* unreadable / native */ }

                using (asm)
                {
                    // Skip the dll that we ourselves emit — reading our own stub here would just
                    // pollute interfaceTypes with our own previously-emitted (possibly wrong)
                    // shapes from an earlier install. We always regenerate it from scratch below.
                    bool isOwnSynth = string.Equals(
                        Path.GetFileNameWithoutExtension(dllPath),
                        "Windows",
                        StringComparison.OrdinalIgnoreCase);

                    if (!isOwnSynth)
                    {
                        foreach (var typeRef in asm.MainModule.GetTypeReferences())
                        {
                            if (typeRef.Scope == null) continue;
                            if (!IsWindowsNs(typeRef.Namespace)) continue;

                            string scopeName = typeRef.Scope.Name;

                            if (!missingByScope.TryGetValue(scopeName, out var set))
                            {
                                set = new HashSet<(string, string)>();
                                missingByScope[scopeName] = set;
                            }
                            set.Add((typeRef.Namespace, typeRef.Name));
                        }

                        // Collect interface-impl hints from every type (incl. nested) in this dll.
                        foreach (var t in asm.MainModule.GetAllTypes())
                        {
                            foreach (var iface in t.Interfaces)
                            {
                                if (IsWindowsNs(iface.InterfaceType.Namespace))
                                    interfaceTypes.Add(iface.InterfaceType.FullName);
                            }
                        }
                    }
                }
            }

            if (missingByScope.Count == 0)
            {
                Log.Info(LogCategory.AppInstall, "WindowsTypeSynthesizer: no missing Windows.* types — nothing to synthesize");
                return;
            }

            foreach (var kv in missingByScope)
            {
                string scopeName = kv.Key;
                var types = kv.Value;
                try { SynthesizeAssembly(installFolder, scopeName, types, interfaceTypes); }
                catch (Exception ex)
                {
                    Log.Warn(LogCategory.AppInstall, $"WindowsTypeSynthesizer: synth failed for {scopeName}: {ex}");
                }
            }
        }

        private static bool IsWindowsNs(string? ns)
        {
            if (string.IsNullOrEmpty(ns)) return false;
            return ns == "Windows" || ns!.StartsWith("Windows.", StringComparison.Ordinal);
        }

        /// <summary>
        /// Heuristic for "is this likely an interface, given no metadata about it?"
        /// True when the FullName appears in <paramref name="interfaceTypes"/> (collected from
        /// real interface impls), or when the type follows the I+UpperCase convention.
        /// </summary>
        private static bool LooksLikeInterface(string ns, string name, HashSet<string> interfaceTypes)
        {
            string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            if (interfaceTypes.Contains(fullName)) return true;
            return name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]);
        }

        private static void SynthesizeAssembly(
            string installFolder,
            string scopeName,
            HashSet<(string Ns, string Name)> types,
            HashSet<string> interfaceTypes)
        {
            Log.Info(LogCategory.AppInstall, $"WindowsTypeSynthesizer: emitting stub assembly '{scopeName}.dll' with {types.Count} type(s)");

            using AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition(scopeName, new Version(8, 0, 0, 0)),
                scopeName,
                ModuleKind.Dll);
            ModuleDefinition mod = asm.MainModule;

            foreach (var (ns, name) in types)
            {
                bool isInterface = LooksLikeInterface(ns, name, interfaceTypes);

                TypeDefinition t;
                if (isInterface)
                {
                    // Empty interface — just the type identity. Members aren't required because
                    // the user code never calls through this type directly (it goes through the
                    // user's own stubbed accessor types).
                    t = new TypeDefinition(
                        ns,
                        name,
                        TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract |
                            TypeAttributes.AutoLayout | TypeAttributes.AnsiClass,
                        baseType: null);
                }
                else
                {
                    t = new TypeDefinition(
                        ns,
                        name,
                        TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoLayout |
                            TypeAttributes.AnsiClass | TypeAttributes.Sealed,
                        mod.TypeSystem.Object);

                    // Family default ctor so accidental newobj from user code at least has *some*
                    // ctor to bind against; body just calls object..ctor.
                    var ctor = new MethodDefinition(".ctor",
                        Mono.Cecil.MethodAttributes.Family | Mono.Cecil.MethodAttributes.HideBySig |
                        Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName,
                        mod.TypeSystem.Void)
                    { ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed };
                    ctor.Body = new Mono.Cecil.Cil.MethodBody(ctor);
                    var il = ctor.Body.GetILProcessor();
                    il.Append(il.Create(Mono.Cecil.Cil.OpCodes.Ldarg_0));
                    il.Append(il.Create(Mono.Cecil.Cil.OpCodes.Call,
                        mod.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes)!)));
                    il.Append(il.Create(Mono.Cecil.Cil.OpCodes.Ret));
                    t.Methods.Add(ctor);
                }

                mod.Types.Add(t);
            }

            // Sanity: clear ContentType.WindowsRuntime from any asm refs that snuck in. We added
            // none ourselves, but be defensive about Cecil's defaults.
            const AssemblyAttributes WinRtFlag = (AssemblyAttributes)0x0200;
            foreach (var r in mod.AssemblyReferences) r.Attributes &= ~WinRtFlag;

            string destPath = Path.Combine(installFolder, scopeName + ".dll");
            asm.Write(destPath);
        }
    }

    internal static class CecilModuleExtensions
    {
        /// <summary>Yields all top-level + nested types in <paramref name="module"/>.</summary>
        public static IEnumerable<TypeDefinition> GetAllTypes(this ModuleDefinition module)
        {
            var stack = new Stack<TypeDefinition>(module.Types);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                yield return t;
                foreach (var nt in t.NestedTypes) stack.Push(nt);
            }
        }
    }
}
