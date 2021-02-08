using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Types;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps
{
    /// <summary>
    /// Maps CSGString to System::String. Saves us having to generate wrappers for CSGString.
    /// </summary>
    [TypeMap("CSGString")]
    internal class CSGStringTypeMap : TypeMap
    {
        /// <summary>
        /// Tells CppSharp what is the type we want to use in the managed world for the CSGString type being mapped.
        /// </summary>
        /// <param name="ctx">Contains the AST type that represents CSGString.</param>
        /// <returns>Type to use in the managed world for CSGString. In this case we use System::String.</returns>
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        /// <summary>
        /// Required when we are wrapping functions that take a non const CSGString ref. Without this CppSharp marshals a managed parameter
        /// to void* when it sees a non const by ref CSGString.
        /// </summary>
        /// <param name="ctx">Contains the AST type that represents CSGString.</param>
        /// <returns>Type to use when marshalling a managed param to the native type.</returns>
        public override Type CppSignatureType(TypePrinterContext ctx)
        {
            // CSGString is a tag type.
            TagType tagType = ctx.Type as TagType;

            // Using CppTypePrinter from CppSharp to print the native type name of CSGString.
            CppTypePrinter typePrinter = new CppTypePrinter(Context);
            return new CustomType(tagType.Declaration.Visit(typePrinter));
        }

        /// <summary>
        /// Tells CppSharp how to marshal CSGString to System::String. This happens when we are calling native functions that return
        /// CSGString and we need to return a System::String result.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the native instance to map.</param>
        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            // marshal_as does not understand CSGString, so put it in a LPCSTR first.
            ctx.Before.WriteLine($"LPCSTR __{ctx.ArgName} = {ctx.ReturnVarName};");
            ctx.Return.Write($"msclr::interop::marshal_as<System::String^>(__{ctx.ArgName})");
        }

        /// <summary>
        /// Tells CppSharp how to marshal System::String to CSGString. This happens when we have a managed parameter that we want to pass
        /// to a native function.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the managed instance to map.</param>
        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            // marshal_as complains if we directly pass it a parameter marked as System::Runtime::InteropServices::In/Out in scenarios
            // where we are dealing with by ref parameter from the native world.
            // To make it happy, call ToString when marshalling to native type.
            ctx.Before.WriteLine(
                $"CSGString _{ctx.ArgName} = msclr::interop::marshal_as<std::string>({ctx.Parameter.Name}->ToString()).c_str();");

            ctx.Return.Write($"_{ctx.ArgName}");
        }
    }
}
