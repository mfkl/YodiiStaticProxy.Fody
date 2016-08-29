using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Yodii.Model;
using YodiiStaticProxy.Fody.Service;

namespace YodiiStaticProxy.Fody
{
    public class ModuleWeaver
    {
        public ModuleWeaver()
        {
            LogInfo = m => { };
        }

        // Will log an informational message to MSBuild
        public Action<string> LogInfo { get; set; }
        
        public void Execute()
        {
            // loading assembly
            //            var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
            //            var assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");
            //            var assembly = Assembly.LoadFile(assemblyPath);
            //            var typesToProxy = assembly.DefinedTypes.Where(t => t.IsInterface && t.ImplementedInterfaces
            //                                    .Any(i => i.FullName == typeof (IYodiiService).FullName))
            //                                    .ToList();
            try
            {
                var currentAssembly = Assembly.GetExecutingAssembly();
                foreach (var assembly in GetDependentAssemblies(currentAssembly))
                {
                    var typesToProxy = assembly.DefinedTypes.Where(t => t.IsInterface && t.ImplementedInterfaces
                                                .Any(i => i.FullName == typeof (IYodiiService).FullName))
                                                .ToList();

                    foreach(var type in typesToProxy.Select(t => t.UnderlyingSystemType))
                    {
                        LogInfo("Creating static proxy for type " + type + " of assembly " + assembly.FullName);
                        var proxyDefinition = new DefaultProxyDefinition(type);
                        ProxyFactory.CreateStaticProxy(proxyDefinition);
                    }
                }
            }
            catch (Exception e)
            {
                LogInfo(e.Message);
            }
        }

        IEnumerable<Assembly> GetDependentAssemblies(Assembly analyzedAssembly)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetReferencedAssemblies()
                .Select(assemblyName => assemblyName.FullName)
                .Contains(analyzedAssembly.FullName));
        }
    }
}