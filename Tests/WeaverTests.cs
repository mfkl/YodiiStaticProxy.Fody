using System;
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
        [TestFixtureSetUp]
        public void Setup()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolverFix.HandleAssemblyResolve;

            var weavingTask = new ModuleWeaver();

            weavingTask.Execute();
        }

        [Test]
        public void ValidateDynamicProxyTypesAreCreated()
        {
            var folderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\lib"));
            var assemblyName = "YodiiStaticProxy.dll";
            var finalPath = Path.Combine(folderPath, assemblyName);

            var assembly = Assembly.LoadFile(finalPath);

            var types = assembly.DefinedTypes.ToList();
            Assert.AreEqual(types[0].Name, "IService_Proxy_1");
            Assert.AreEqual(types[1].Name, "IService_Proxy_1_UN");
        }

        public static class AssemblyResolverFix
        {
            public static Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
            {
                return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.FullName == args.Name);
            }
        }

//#if(DEBUG)
//        [Test]
//        public void PeVerify()
//        {
//            Verifier.Verify(assemblyPath,newAssemblyPath);
//        }
//#endif
    }
}