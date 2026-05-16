using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace WPR.SilverlightCompability
{
    /// <summary>Shim for <c>System.Windows.Deployment</c>. Singleton describing the
    /// installed XAP. <c>Current</c> is exposed so user code can access entry-point
    /// info; we leave Parts empty.</summary>
    public class Deployment
    {
        public static Deployment Current { get; } = new Deployment();
        public DeploymentPartCollection Parts { get; } = new DeploymentPartCollection();
        public string? EntryPointAssembly { get; set; }
        public string? EntryPointType { get; set; }
    }
}
