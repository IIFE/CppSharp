using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.AST;
using CppSharp.Generators.CLI;
using CppSharp.Types;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps
{
    /// <summary>
    /// We don't not generate a wrapper for COPtr as that is not really neccessary because the types we use COPtr for will have wrappers generated
    /// for them wich will themselves be managed, so no need for a "smart" pointer wrapper.
    /// What we need to do is extract the type that is used in the COPtr class template, and given that we know COPtr template types 
    /// have wrappers generated for them because they are just normal classes, then we use that knowledge to use those generated wrappers to
    /// marshall between native and managed worlds.
    /// </summary>
    [TypeMap("COPtr")]
    [TypeMap("shared_ptr")]
    [TypeMap("unique_ptr")]
    internal class COPtrTypeMap : TypeMap
    {
        /// <summary>
        /// Tells CppSharp what is the type we want to use in the managed world for the COPtr type being mapped.
        /// </summary>
        /// <param name="ctx">Contains the AST type that represents _GUID.</param>
        /// <returns>Type to use in the managed world for COPtr. In this case we use the template type from COPtr that we have
        /// a wrapper generated for in the managed world.</returns>
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            // We know we only have a single template type per COPtr.
            Type type = ctx.Type.GetTemplateTypeArgs().First();

            // Some template types from COPtr may have a dedicated type map for them, therefore ensure we use that type map if it exists.
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(type, out typeMap))
            {
                return typeMap.CLISignatureType(new TypePrinterContext { Type = type });
            }

            return type;
        }

        /// <summary>
        /// Used to create a forward declaration and potentially an include for a COPtr template type. This is neccessary
        /// if CppSharp does not generate a forward decl/include because it things the COPtr type is ignored, but in fact we are
        /// generating marshalling code for the template type itself.
        /// </summary>
        /// <param name="collector">Instance allowing us to generate type references for generated wrappers.</param>
        /// <param name="record">AST record representing the COPtr type reference.</param>
        public override void CLITypeReference(CLITypeReferenceCollector collector, ASTRecord<Declaration> record)
        {
            // Get the COPtr template type and its declration. We don't want the COPtr one, instead we need the one for the COPtr template type.
            ITypedDecl typedDecl = record.Value as ITypedDecl;
            if (typedDecl != null)
            {
                Type templateType = typedDecl.Type.GetTemplateTypeArgs().First();
                Declaration templateTypeDecl;
                if (templateType.TryGetDeclaration(out templateTypeDecl))
                {
                    CLITypeReference typeRef = collector.GetTypeReference(templateTypeDecl);
                    if (typeRef == null)
                    {
                        typeRef = collector.GetTypeReference(record.Value);
                    }

                    if (typeRef != null)
                    {
                        if (string.IsNullOrEmpty(typeRef.FowardReference))
                        {
                            typeRef.FowardReference = $"ref class {templateTypeDecl.Name};";
                        }

                        typeRef.Include.InHeader |= collector.IsIncludeInHeader(record);
                    }
                }
            }
        }

        /// <summary>
        /// Tells CppSharp how to marshal the template type from COPtr to the generated wrapper for that template type.
        /// This happens when we are calling native functions that return COPtr and we need to return the generated wrapper to the managed world.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the native instance to map.</param>
        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            Type type = ctx.ReturnType.Type.GetTemplateTypeArgs().First();

            // Some template types from COPtr may have a dedicated type map for them, therefore ensure we use that type map if it exists.
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(type, out typeMap))
            {
                ctx.ReturnType = new QualifiedType(typeMap.Type);
                typeMap.CLIMarshalToManaged(ctx);
                return;
            }

            // Otherwise create an instance of the generated wrapper for the template type from COPtr.
            string typeName = type.ToString().TrimEnd('^');
            ctx.Return.Write($"{ctx.ReturnVarName}.IsNull() ? nullptr : gcnew {typeName}({ctx.ReturnVarName}.get())");
        }

        /// <summary>
        /// Tells CppSharp how to marshal the generated wrapper for the COPtr template back to COPtr. This happens when we have a 
        /// managed parameter that we want to pass to a native function.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the managed instance to map.</param>
        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            Type type = ctx.Parameter.Type.GetTemplateTypeArgs().First();

            string nativePtr;

            // For each generated wrapper CppSharp generates a property called NativePtr which represents the native type being
            // wrapped. If there is inheritance involved, CppSharp will generate a single NativePtr in the most base class
            // and the pointer points to that base class.
            // As a result, we need to ensure that if there is an inheritance hierarchy for the managed wrapper of the COPtr template
            // type then we cast NativePtr to the type we are dealing with, and pass the casted result to the native function call.
            // We can't use NativePtr directly because that won't automatically get assigned to the type we are dealing with.
            Class @class;
            bool gotClass = type.TryGetClass(out @class);            
            if (gotClass && @class.HasBase)
            {
                nativePtr = $"static_cast<::{@class.QualifiedOriginalName}*>({ctx.Parameter.Name}->NativePtr)";
            }
            else
            {
                nativePtr = $"{ctx.Parameter.Name}->NativePtr";
            }

            // If the native function call takes a COPtr by reference, then we need to create a local variable that is of the
            // COPtr type and pass that to the native function. The reason is that we cannot pass a raw pointer to a by ref COPtr
            // parameter. However, COPtr can be constructed from a raw pointer, so we use that ability to allow us to pass
            // a COPtr by ref.
            if (ctx.Parameter.Type.IsReference())
            {
                ctx.Before.WriteLine($"::COPtr<::{@class.QualifiedOriginalName}> _{ctx.ArgName} = {nativePtr};");
                ctx?.Return?.Write($"_{ctx.ArgName}");
            }
            else if (ctx.Parameter.Type.IsPointer())
            {
                // Pass an address to the COPtr if the native function call takes a raw pointer to a COPtr instance.
                ctx.Before.WriteLine($"::COPtr<::{@class?.QualifiedOriginalName}> _{ctx.ArgName} = {nativePtr};");
                ctx?.Return?.Write($"&_{ctx.ArgName}");
            }
            else
            {
                ctx?.Return?.Write(nativePtr);
            }
        }
    }
}
