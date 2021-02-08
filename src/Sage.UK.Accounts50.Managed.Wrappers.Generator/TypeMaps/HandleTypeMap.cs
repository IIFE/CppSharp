using CppSharp.Types;
using CppSharp.AST;
using CppSharp.Generators;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps
{
    /// <summary>
    /// Base class to allow mapping a native handle type (e.g. HWND, HINSTANCE) to managed System::IntPtr. As the mapping functionality
    /// is the same across handle types, this is an abstract class that needs to be implemented by providing the name of the native
    /// handle type so that we can marshal the managed System::IntPtr back to the native type by casting it.
    /// </summary>
    internal abstract class HandleTypeMap : TypeMap
    {
        protected abstract string HandleTypeName { get; }

        /// <summary>
        /// Tells CppSharp what is the type we want to use in the managed world for the handle type being mapped.
        /// </summary>
        /// <param name="ctx">Contains the AST type that represents the handle type.</param>
        /// <returns>Type to use in the managed world for the native handle type. In this case we use System::IntPtr.</returns>
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(System.IntPtr));
        }

        /// <summary>
        /// Tells CppSharp how to marshal the native handle type to System::IntPtr. This happens when we are calling native functions that return
        /// a native handle type and we need to return a System::IntPtr result.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the native instance to map.</param>
        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write($"System::IntPtr({ctx.ReturnVarName})");
        }

        /// <summary>
        /// Tells CppSharp how to marshal System::IntPtr to the native handle type. This happens when we have a managed parameter that we want to pass
        /// to a native function.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the managed instance to map.</param>
        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write($"({HandleTypeName}) {ctx.Parameter.Name}.ToPointer()");
        }
    }

    /// <summary>
    /// Map HWND to System::IntPtr.
    /// </summary>
    [TypeMap("HWND__")]
    internal class HwndTypeMap : HandleTypeMap
    {
        protected override string HandleTypeName
        {
            get
            {
                return "HWND";
            }
        }
    }

    /// <summary>
    /// Map HINSTANCE to System::IntPtr.
    /// </summary>
    [TypeMap("HINSTANCE__")]
    internal class HinstanceTypeMap : HandleTypeMap
    {
        protected override string HandleTypeName
        {
            get
            {
                return "HINSTANCE";
            }
        }
    }

    /// <summary>
    /// Map HMENU to System::IntPtr.
    /// </summary>
    [TypeMap("HMENU__")]
    internal class HmenuTypeMap : HandleTypeMap
    {
        protected override string HandleTypeName
        {
            get
            {
                return "HMENU";
            }
        }
    }
}
