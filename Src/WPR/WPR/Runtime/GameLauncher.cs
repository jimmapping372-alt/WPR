using System;
using System.IO;
using System.Reflection;
using WPR.Models;

namespace WPR.Runtime
{
    public sealed class GameLauncher
    {
        private readonly string _appsRoot;

        public GameLauncher(string baseFolder)
        {
            _appsRoot = Path.Combine(baseFolder, "apps");
        }

        public void Launch(WprApplication app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (string.IsNullOrWhiteSpace(app.Assembly) || string.IsNullOrWhiteSpace(app.EntryPoint))
                throw new InvalidOperationException("Application entry point is not specified.");

            var productFolder = Path.Combine(_appsRoot, app.ProductId);
            var assemblyPath = Path.Combine(productFolder, app.Assembly);

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("Entry assembly not found", assemblyPath);
            }

            var assembly = Assembly.LoadFrom(assemblyPath);
            var entryType = assembly.GetType(app.EntryPoint, throwOnError: true, ignoreCase: false);

            // Предполагаем, что у игры есть конструктор без параметров и метод Run/Start
            var instance = Activator.CreateInstance(entryType);
            var runMethod = entryType.GetMethod("Run") ?? entryType.GetMethod("Start");

            if (runMethod == null)
            {
                throw new MissingMethodException(entryType.FullName, "Run/Start");
            }

            runMethod.Invoke(instance, null);
        }
    }
}
