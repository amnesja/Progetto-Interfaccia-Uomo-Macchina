using System;

namespace Ordo.Web.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
    public sealed class TypeScriptModuleAttribute : Attribute
    {
        private readonly string moduleName;
        public TypeScriptModuleAttribute(string moduleName)
        {
            this.moduleName = moduleName;
        }
    }
}
