using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Mono.Cecil;
using NUnit.Framework;
using YodiiStaticProxy.Fody;
using YodiiStaticProxy.Fody.Service;

namespace Tests
{
    [TestFixture]
    public class WeaverTests
    {
        Assembly assembly;
        string newAssemblyPath;
        string assemblyPath;

        [TestFixtureSetUp]
        public void Setup()
        {
            var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
            assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");
#if (!DEBUG)
        assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

            newAssemblyPath = assemblyPath.Replace(".dll", "2.dll");
            File.Copy(assemblyPath, newAssemblyPath, true);

            var moduleDefinition = ModuleDefinition.ReadModule(newAssemblyPath);
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.Execute();
            moduleDefinition.Write(newAssemblyPath);

            assembly = Assembly.LoadFile(newAssemblyPath);
        }

        [Test]
        public void ValidateHelloWorldIsInjected()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yodiiproxy");

            var formatter = new BinaryFormatter();
            formatter.Binder = new ProxyTypeConverter();
//            ServiceProxyBase proxy;
            object proxy;
            using(var stream = new FileStream(files.First(), FileMode.Open, FileAccess.Read))
            {
                proxy = formatter.Deserialize(stream);
            }

            Assert.True(proxy != null);
        }

#if(DEBUG)
        [Test]
        public void PeVerify()
        {
            Verifier.Verify(assemblyPath,newAssemblyPath);
        }
#endif
    }
}