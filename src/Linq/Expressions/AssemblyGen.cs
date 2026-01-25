using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Linq.Expressions
{
    internal sealed class AssemblyGen
    {
        private static AssemblyGen _assembly;

        private readonly AssemblyBuilder _myAssembly;

        private readonly ModuleBuilder _myModule;

        private int _index;

        private static AssemblyGen Assembly
        {
            get
            {
                if (AssemblyGen._assembly == null)
                {
                    Interlocked.CompareExchange<AssemblyGen>(ref AssemblyGen._assembly, new AssemblyGen(), null);
                }
                return AssemblyGen._assembly;
            }
        }

        private AssemblyGen()
        {
            AssemblyName assemblyName = new AssemblyName("Snippets");
            CustomAttributeBuilder[] customAttributeBuilder = new CustomAttributeBuilder[] { new CustomAttributeBuilder(typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes), new object[0]) };
            this._myAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run, customAttributeBuilder);
            this._myModule = this._myAssembly.DefineDynamicModule(assemblyName.Name, false);
            this._myAssembly.DefineVersionInfoResource();
        }

        internal static TypeBuilder DefineDelegateType(string name)
        {
            return AssemblyGen.Assembly.DefineType(name, typeof(MulticastDelegate), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass);
        }

        private TypeBuilder DefineType(string name, Type parent, TypeAttributes attr)
        {
            ContractUtils.RequiresNotNull(name, "name");
            ContractUtils.RequiresNotNull(parent, "parent");
            StringBuilder stringBuilder = new StringBuilder(name);
            int num = Interlocked.Increment(ref this._index);
            stringBuilder.Append("$");
            stringBuilder.Append(num);
            stringBuilder.Replace('+', '\u005F').Replace('[', '\u005F').Replace(']', '\u005F').Replace('*', '\u005F').Replace('&', '\u005F').Replace(',', '\u005F').Replace('\\', '\u005F');
            name = stringBuilder.ToString();
            return this._myModule.DefineType(name, attr, parent);
        }
    }
}