using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using YodiiStaticProxy.Fody;
using YodiiStaticProxy.Fody.Finders;

namespace Tests
{
    [TestFixture]
    public class WeaverTests
    {
        static readonly string LibFolderPath = Util.GetProxyAssemblyFolderPath();
        const string AssemblyDllFile = "YodiiStaticProxy.dll";
        const string AssemblyPdbFile = "YodiiStaticProxy.pdb";
        readonly string _dllFilePath = Path.Combine(LibFolderPath, AssemblyDllFile);
        readonly string _pdbFilePath = Path.Combine(LibFolderPath, AssemblyPdbFile);

        [TestFixtureSetUp]
        public void Setup()
        {
            CleanUp();

            var weavingTask = new ModuleWeaver();
            
            weavingTask.Execute();
        }

        [Test]
        public void ValidateDynamicProxyTypesAreCreated()
        {
            var assembly = Assembly.LoadFile(_dllFilePath);
            
            var types = assembly.DefinedTypes.ToList();
            
            Assert.AreEqual(types[0].Name, "IService_Proxy_1");
            Assert.AreEqual(types[1].Name, "IService_Proxy_1_UN");
        }

        void CleanUp()
        {
            File.Delete(_dllFilePath);
            File.Delete(_pdbFilePath);
        }
    }
}