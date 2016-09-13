using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Mono.Cecil;
using Yodii.Model;
using YodiiStaticProxy.Fody.Finders;
using YodiiStaticProxy.Fody.Service;

namespace YodiiStaticProxy.Fody
{
    public class ModuleWeaver
    {
        public ModuleWeaver()
        {
            LogInfo = m => { };
        }
        public Action<string> LogInfo { get; set; }
        public ModuleDefinition ModuleDefinition { get; set; }

        public void Execute()
        {
            // need to check if ProxyAssembly already created.
            // if it is, load it using Mono.Cecil and add generated types to it then save.
            // if it's not, create it using DefineDynamicAssembly, add generated types and then save (once!). 
//            var assemblyPath = Util.GetProxyAssemblyFilePath();
//            var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath);

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