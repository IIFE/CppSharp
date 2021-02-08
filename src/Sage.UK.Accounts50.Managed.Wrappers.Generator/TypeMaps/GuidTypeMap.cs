using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Types;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps
{
    /// <summary>
    /// Maps _GUID to System::Guid.
    /// </summary>
    [TypeMap("_GUID")]
    internal class GuidTypeMap : TypeMap
    {
        /// <summary>
        /// Tells CppSharp what is the type we want to use in the managed world for the _GUID type being mapped.
        /// </summary>
        /// <param name="ctx">Contains the AST type that represents _GUID.</param>
        /// <returns>Type to use in the managed world for _GUID. In this case we use System::Guid.</returns>
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(System.Guid));
        }

        /// <summary>
        /// Tells CppSharp how to marshal _GUID to System::Guid. This happens when we are calling native functions that return
        /// _GUID and we need to return a System::Guid result.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the native instance to map.</param>
        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write($"System::Guid({ctx.ReturnVarName}.Data1, {ctx.ReturnVarName}.Data2, {ctx.ReturnVarName}.Data3, "
                + $"{ctx.ReturnVarName}.Data4[0], {ctx.ReturnVarName}.Data4[1],"
                + $"{ctx.ReturnVarName}.Data4[2], {ctx.ReturnVarName}.Data4[3], "
                + $"{ctx.ReturnVarName}.Data4[4], {ctx.ReturnVarName}.Data4[5], "
                + $"{ctx.ReturnVarName}.Data4[6], {ctx.ReturnVarName}.Data4[7])");
        }

        /// <summary>
        /// Tells CppSharp how to marshal System::Guid to _GUID. This happens when we have a managed parameter that we want to pass
        /// to a native function.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the managed instance to map.</param>
        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Before.WriteLine($"cli::array<Byte>^ guidData = {ctx.Parameter.Name}.ToByteArray();");
            ctx.Before.WriteLine($"cli::pin_ptr<Byte> data = &(guidData[0]);");
            ctx.Return.Write("*(_GUID*)data");
        }
    }
}
