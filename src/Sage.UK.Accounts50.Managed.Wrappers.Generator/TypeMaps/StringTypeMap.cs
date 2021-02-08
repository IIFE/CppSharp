using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Types;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps
{
    /// <summary>
    /// Used to map native string types to managed System::String.
    /// </summary>
    [TypeMap("const char*")]
    [TypeMap("basic_string<char, char_traits<char>, allocator<char>>")]
    internal class StringTypeMap : TypeMap
    {
        /// <summary>
        /// Tells CppSharp what is the type we want to use in the managed world for the string type being mapped.
        /// </summary>
        /// <param name="ctx">Contains the AST type that represents the native string type.</param>
        /// <returns>Type to use in the managed world for native string type. In this case we use System::String.</returns>
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        /// <summary>
        /// Tells CppSharp how to marshal the native string type to System::String. This happens when we are calling native functions that return
        /// the native string type and we need to return a System::String result.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the native instance to map.</param>
        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write($"msclr::interop::marshal_as<System::String^>({ctx.ReturnVarName})");
        }

        /// <summary>
        /// Tells CppSharp how to marshal System::String to the native string type. This happens when we have a managed parameter that we want to pass
        /// to a native function.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the managed instance to map.</param>
        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Before.WriteLine($"std::string _{ctx.ArgName} = msclr::interop::marshal_as<std::string>({ctx.Parameter.Name}->ToString());");
            ctx.Return.Write($"_{ctx.ArgName}.c_str()");
        }
    }
}
