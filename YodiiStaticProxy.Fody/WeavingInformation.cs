using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Yodii.Model;
using YodiiStaticProxy.Fody.Service;

namespace YodiiStaticProxy.Fody
{
    public static class WeavingInformation
    {
        public static ModuleDefinition ModuleDefinition { get; private set; }
        public static Assembly LoadedAssembly { get; set; }

        public static void Initialize()
        {
            ModuleDefinition = ModuleWeaver.Instance.ModuleDefinition;
        }
    }
}