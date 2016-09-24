using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
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
        public Action<string> LogInfo { get; set; }
        public ModuleDefinition ModuleDefinition { get; set; }

        public void Execute()
        {
            DefineNewProxyAssembly();

            GenerateProxies();

            SaveGeneratedProxyAssemblyToDisk();
        }
        
        void DefineNewProxyAssembly()
        {       
            ProxyFactory.DefineNewProxyAssembly(ModuleDefinition.Assembly.Name.Name + ".YodiiStaticProxy");
        }
        
        void GenerateProxies()
        {
            try
            {
                var assemblyFullName = ModuleDefinition.FullyQualifiedName;
                var assembly = Assembly.LoadFile(assemblyFullName);

                var typesToProxy = assembly.DefinedTypes.Where(t => t.IsInterface && t.ImplementedInterfaces
                    .Any(i => i.FullName == typeof(IYodiiService).FullName))
                    .ToList();

                foreach(var type in typesToProxy.Select(t => t.UnderlyingSystemType))
                {
                    LogInfo("Creating a Yodii proxy for the type " + type.FullName + " contained in assembly " + assembly.FullName);
                    var proxyDefinition = new DefaultProxyDefinition(type);

                    ProxyFactory.CreateStaticProxy(proxyDefinition);
                }
            }
            catch(Exception e)
            {
                LogInfo(e.Message);
            }
        }

        void SaveGeneratedProxyAssemblyToDisk()
        {
            try
            {
                ProxyFactory.WriteProxyAssemblyToDisk();
            }
            catch (Exception e)
            {
                LogInfo(e.Message);
            }
        }
    }
}