using System.Linq;
using Mono.Cecil;
using Yodii.Model;
using YodiiStaticProxy.Fody.Service;

namespace YodiiStaticProxy.Fody
{
    public static class WeavingInformation
    {
        public static ModuleDefinition ModuleDefinition { get; private set; }
        public static TypeReference ObjectTypeReference { get; private set; }
        public static TypeDefinition YodiiServiceTypeDef { get; private set; }
        public static TypeDefinition ServiceProxyBaseTypeDef { get; private set; }


        public static void Initialize()
        {
            ModuleDefinition = ModuleWeaver.Instance.ModuleDefinition;

            ObjectTypeReference = ModuleDefinition.ImportReference(typeof (object));
            YodiiServiceTypeDef = ModuleDefinition.Types.First(t => t.FullName == typeof(IYodiiService).FullName);
            ServiceProxyBaseTypeDef = ModuleDefinition.Types.First(t => t.FullName == typeof(ServiceProxyBase).FullName);
        }
    }
}