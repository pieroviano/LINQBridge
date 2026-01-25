#nullable disable
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Linq.Expressions.Compiler;

internal sealed class AssemblyGen
{
    private static AssemblyGen _assembly;
    private readonly AssemblyBuilder _myAssembly;
    private readonly ModuleBuilder _myModule;
    private int _index;

    private AssemblyGen()
    {
        var name = new AssemblyName("Snippets");
        var assemblyAttributes = new CustomAttributeBuilder[1]
        {
            new(typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes), new object[0])
        };
        _myAssembly =
            AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run, assemblyAttributes);
        _myModule = _myAssembly.DefineDynamicModule(name.Name, false);
        _myAssembly.DefineVersionInfoResource();
    }

    private static AssemblyGen Assembly
    {
        get
        {
            if (_assembly == null)
            {
                Interlocked.CompareExchange(ref _assembly, new AssemblyGen(), null);
            }

            return _assembly;
        }
    }

    internal static TypeBuilder DefineDelegateType(string name)
    {
        return Assembly.DefineType(name, typeof(MulticastDelegate),
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass);
    }

    private TypeBuilder DefineType(string name, Type parent, TypeAttributes attr)
    {
        ContractUtils.RequiresNotNull(name, nameof(name));
        ContractUtils.RequiresNotNull(parent, nameof(parent));
        var stringBuilder = new StringBuilder(name);
        var num = Interlocked.Increment(ref _index);
        stringBuilder.Append("$");
        stringBuilder.Append(num);
        stringBuilder.Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('*', '_').Replace('&', '_')
            .Replace(',', '_').Replace('\\', '_');
        name = stringBuilder.ToString();
        return _myModule.DefineType(name, attr, parent);
    }
}