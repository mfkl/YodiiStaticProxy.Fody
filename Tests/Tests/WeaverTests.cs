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
        const string AssemblyDllFile = "ServiceAssemblyOne.YodiiStaticProxy.dll";
        const string AssemblyPdbFile = "ServiceAssemblyOne.YodiiStaticProxy.pdb";
        readonly string _dllFilePath = Path.Combine(LibFolderPath, AssemblyDllFile);
        readonly string _pdbFilePath = Path.Combine(LibFolderPath, AssemblyPdbFile);

        [TestFixtureSetUp]
        public void Setup()
        {
            CleanUp();

            var serviceAssemblyOneProjectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\ServiceAssemblyOne\bin\Debug\ServiceAssemblyOne.dll"));
            var serviceAssemblyTwoProjectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\ServiceAssemblyTwo\bin\Debug\ServiceAssemblyTwo.dll"));

            var moduleDefinition = ModuleDefinition.ReadModule(serviceAssemblyOneProjectPath);
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.Execute();

            moduleDefinition = ModuleDefinition.ReadModule(serviceAssemblyTwoProjectPath);
            weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.Execute();
        }

        [Test]
        public void ValidateDynamicProxyTypesAreCreated()
        {
            var assembly = Assembly.LoadFile(_dllFilePath);
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