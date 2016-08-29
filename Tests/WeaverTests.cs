using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using YodiiStaticProxy.Fody;

namespace Tests
{
    [TestFixture]
    public class WeaverTests
    {
        static readonly string LibFolderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\lib"));
        const string AssemblyDllFile = "YodiiStaticProxy.dll";
        const string AssemblyPdbFile = "YodiiStaticProxy.pdb";
        readonly string _dllFilePath = Path.Combine(LibFolderPath, AssemblyDllFile);
        readonly string _pdbFilePath = Path.Combine(LibFolderPath, AssemblyPdbFile);

        [TestFixtureSetUp]
        public void Setup()
        {
            CleanUp();
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolverFix.HandleAssemblyResolve;

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

        public static class AssemblyResolverFix
        {
            public static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
            {
                return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.FullName == args.Name);
            }
        }
    }
}