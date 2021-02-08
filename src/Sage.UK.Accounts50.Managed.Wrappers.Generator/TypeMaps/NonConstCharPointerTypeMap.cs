using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Types;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps
{
    /// <summary>
    /// Type map to map non const char* to System::String.
    /// </summary>
    [TypeMap("char*")]
    internal class NonConstCharPointerTypeMap : TypeMap
    {
        /// <summary>
        /// Tells CppSharp what is the type we want to use in the managed world for the char* type being mapped.
        /// </summary>
        /// <param name="ctx">Contains the AST type that represents char*.</param>
        /// <returns>Type to use in the managed world for char*. In this case we use System::String.</returns>
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        /// <summary>
        /// Tells CppSharp how to marshal char* to System::String. This happens when we are calling native functions that return
        /// char* and we need to return a System::String result.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the native instance to map.</param>
        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write($"msclr::interop::marshal_as<System::String^>({ctx.ReturnVarName})");
        }

        /// <summary>
        /// Tells CppSharp how to marshal System::String to char*. This happens when we have a managed parameter that we want to pass
        /// to a native function.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the managed instance to map.</param>
        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            // Make use of CSGString to allow us to convert to a non const char*
            ctx.Before.WriteLine(
                $"CSGString _{ctx.ArgName} = msclr::interop::marshal_as<std::string>({ctx.Parameter.Name}).c_str();");

            ctx.Before.WriteLine($"auto __{ctx.ArgName} = _{ctx.ArgName}.GetBuffer(0);");
            ctx.Before.WriteLine($"_{ctx.ArgName}.ReleaseBuffer();");
            ctx.Return.Write($"__{ctx.ArgName}");
        }
    }
}
