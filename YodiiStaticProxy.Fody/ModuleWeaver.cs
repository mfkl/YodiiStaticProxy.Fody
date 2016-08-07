using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Yodii.Model;
using YodiiStaticProxy.Fody.Service;

namespace YodiiStaticProxy.Fody
{
    public class ModuleWeaver
    {
        // Will log an informational message to MSBuild
        public Action<string> LogInfo { get; set; }

        // An instance of Mono.Cecil.ModuleDefinition for processing
        public ModuleDefinition ModuleDefinition { get; set; }

        // Init logging delegates to make testing easier

        public static ModuleWeaver Instance { get; private set; }

        public ModuleWeaver()
        {
            LogInfo = m => { };
            Instance = this;
        }

        public void Execute()
        {
            WeavingInformation.Initialize();

            // loading assembly
            var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
            var assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");
            var assembly = Assembly.LoadFile(assemblyPath);
            WeavingInformation.LoadedAssembly = assembly;
//            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
//            var typesToProxy = new HashSet<TypeInfo>();
//            foreach (var assembly in assemblies)
//            {
//                var types = assembly.DefinedTypes.Where(t => t.IsInterface && t.ImplementedInterfaces.Any(i => i.FullName == typeof(IYodiiService).FullName)).ToList();
//                if(types.Any()) typesToProxy.AddRange(types);
//            }
            var typesToProxy = assembly.DefinedTypes.Where(t => t.IsInterface && t.ImplementedInterfaces.Any(i => i.FullName == typeof(IYodiiService).FullName)).ToList();

            try
            {
                foreach (var type in typesToProxy.Select(t => t.UnderlyingSystemType))
                {
                    var proxyDefinition = new DefaultProxyDefinition(type);
                    ProxyFactory.CreateStaticProxy(proxyDefinition);
                }
            }
            catch (Exception e)
            {
                LogInfo.Invoke(e.Message);
            }
        }
    }
}