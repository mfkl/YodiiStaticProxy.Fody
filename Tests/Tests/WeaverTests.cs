using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;
using YodiiStaticProxy.Fody;
using YodiiStaticProxy.Fody.Finders;

namespace Tests
{
    [TestFixture]
    public class WeaverTests
    {
        static readonly string LibFolderPath = Util.ProxyAssemblyFolderPath;
        const string ServiceOneProxyAssembly = "ServiceAssemblyOne.YodiiStaticProxy.dll";
        readonly string _serviceOneProxyDllFilePath = Path.Combine(LibFolderPath, ServiceOneProxyAssembly);

        [TestFixtureSetUp]
        public void Setup()
        {
            CleanUp();

            var serviceAssemblyOneProjectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\ServiceAssemblyOne\bin\Debug\ServiceAssemblyOne.dll"));

            var moduleDefinition = ModuleDefinition.ReadModule(serviceAssemblyOneProjectPath);
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.Execute();
        }

        [Test]
        public void ValidateDynamicProxyTypesAreCreated()
        {
            var assembly = Assembly.LoadFile(_serviceOneProxyDllFilePath);
            var types = assembly.DefinedTypes.ToList();

            Assert.AreEqual("IServiceOne_Proxy_1", types[0].Name);
            Assert.AreEqual("IServiceOne_Proxy_1_UN", types[1].Name);
        }
        
        void CleanUp()
        {
            var generatedFiles = Directory
                .EnumerateFiles(Util.ProxyAssemblyFolderPath)
                .Where(file => file.EndsWith(".YodiiStaticProxy.dll") || file.EndsWith(".YodiiStaticProxy.pdb"))
                .ToList();

            foreach (var path in generatedFiles)
            {
                File.Delete(path);
            }
        }
    }
}