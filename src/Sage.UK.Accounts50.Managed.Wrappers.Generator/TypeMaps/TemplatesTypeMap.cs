using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Types;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps
{
    /// <summary>
    /// Base class to generating mapping between native class templates and their equivalent managed ones. Must be implemented for each native class template 
    /// to be mapped and specify the name of the managed generic class to wrap the native template, and the name of the native class template to be wrapped.
    /// 
    /// This relies on the managed generic type to already exist as we don't automatically generate managed wrappers for native templates. The reason
    /// is that it's quite complex trying to automatically map native templates to managed generics. I think it can be done, it's just not trivial.
    /// 
    /// Instead of automatically generating a managed wrapper for a native template, we need to manually define the wrapper. This is not actually as bad
    /// as it sounds because the whole point of generics/tempaltes is that they generalise code, so we're not manually wrapping a lot of code.
    /// 
    /// The managed wrapper needs to have two parts, an interface and an implementation. Consider this example to wrap the Enumerator template from Sage 50.
    /// The interface:
    /// generic<typename T> public interface class IEnumerator : System::IDisposable
    /// {
    /// public:
    ///     void Reset();
    /// 
    ///     bool MoveNext();
    /// 
    ///     T Current();
    /// };
    /// 
    /// The interface essentially provides a contract that is the same as the native template. The most important thing is that the interface has no
    /// native template arguments exposed through it (i.e. typename does not refer to a native type). This is crucial because otherwise a .NET assembly 
    /// will not be able to understand the generic class if it has native templates arguments. The typename T must be a managed one, which in this scenario
    /// T will be whatever generated managed type that represents a native enumerable business object.
    /// 
    /// The implementation:
    /// template<typename T, typename TNative> ref class IEnumeratorImpl : IEnumerator<T>
    /// {
    /// public:
    ///     ::IEnumerator<TNative>* NativePtr;
    /// 
    /// public:
    ///     IEnumeratorImpl(::IEnumerator<TNative>* nativePtr) { Omitted for brevity... }
    /// 
    ///     ~IEnumeratorImpl() { Omitted for brevity... }
    /// 
    ///     !IEnumeratorImpl() { Omitted for brevity... }
    /// 
    ///     virtual void Reset() { NativePtr->Reset(); }
    /// 
    ///     virtual bool MoveNext() { return NativePtr->MoveNext(); }
    /// 
    ///     virtual T Current()
    ///     {
    ///         ::COPtr < TNative > current = NativePtr->Current();
    ///         // Return a managed wrapper for the TNative type.
    ///     }
    /// };
    /// 
    /// You'll notice that the implementation now takes a native template argument. This is fine because we don't expose the implementation to the
    /// managed world (this type map ensures that), we only expose the managed generic interface.
    /// As the implementation takes a native template argument, it is here where we do the wrapping of the native template.
    /// 
    /// The class is abstract as it needs to be implemented for each pair of native template and managed generic interface to provide the names
    /// for each item of the pair.
    /// </summary>
    internal abstract class TemplatesTypeMap : TypeMap
    {
        protected abstract string ManagedInterfaceTypeName { get; }

        protected abstract string NativeTemplateName { get; }

        /// <summary>
        /// Tells CppSharp what is the type we want to use in the managed world for the native class template type being mapped.
        /// We extract all the template arguments from the class template, and assume that there is an auto generated wrapper
        /// for each template argument. Based on this assumption the type we specify for the managed world is a managed generic
        /// that takes the template argument names.
        /// </summary>
        /// <param name="ctx">Contains the AST type that represents the native class template.</param>
        /// <returns>Type to use in the managed world for native class template.</returns>
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            string managedGenericArgs = string.Join(",", ctx.Type.GetTemplateTypeArgs().Select(x => x.ToString()));
            return new CustomType($"{ManagedInterfaceTypeName}<{managedGenericArgs}>^");
        }

        /// <summary>
        /// Tells CppSharp how to marshal the managed generic interface to the native class template. This happens when we have a managed 
        /// parameter that we want to pass to a native function.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the managed instance to map.</param>
        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            IEnumerable<Type> typeArgs = ctx.Parameter.Type.GetTemplateTypeArgs();

            // Get the type implementing the generic managed interface and containing a pointer to the native class template.
            string implType = GetImplType(typeArgs);

            // Get the native class template native arguments in order to construct the native class template.
            string nativeTemplateArgs = GetNativeTemplateArgs(typeArgs);

            // Cast the received generic interface instance to the actual impl type so that we can get the pointer to the native class template.
            ctx.Before.WriteLine($"auto _{ctx.ArgName} = dynamic_cast<{implType}^>({ctx.Parameter.Name});");

            // Cast the pointer of the native class template to the expected class template.
            ctx.Before.WriteLine($"auto __{ctx.ArgName} = dynamic_cast<::{NativeTemplateName}<{nativeTemplateArgs}>*>(_{ctx.ArgName}->NativePtr);");

            ctx.Return.Write($"__{ctx.ArgName}");
        }

        /// <summary>
        /// Tells CppSharp how to marshal the native class template to the managed generic interface. This happens when we are calling native 
        /// functions that return the native class template and we need to return an instance of the managed generic interface.
        /// </summary>
        /// <param name="ctx">Contains text generator to write marshalling code to and information about the native instance to map.</param>
        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            IEnumerable<Type> typeArgs = ctx.ReturnType.Type.GetTemplateTypeArgs();

            // Get the type implementing the generic managed interface to construct it and pass it a pointer to the native class template.
            string implType = GetImplType(typeArgs);

            ctx.Return.Write($"gcnew {implType}({ctx.ReturnVarName}.get())");
        }

        private string GetImplType(IEnumerable<Type> managedTypes)
        {
            string managedGenericArgs = string.Join(",", managedTypes.Select(x => x.ToString()));

            string nativeTemplateArgs = GetNativeTemplateArgs(managedTypes);

            // We use a convention where the implementation ends with the word Impl.
            return $"{ManagedInterfaceTypeName}Impl<{managedGenericArgs}, {nativeTemplateArgs}>";
        }

        private string GetNativeTemplateArgs(IEnumerable<Type> managedTypes)
        {
            IEnumerable<string> typeArgsDecls = managedTypes.Select(x =>
            {
                Declaration decl;
                if (!x.TryGetDeclaration(out decl))
                {
                    throw new System.InvalidOperationException("Decl required");
                }

                return decl;
            })            
            .Select(x => $"::{x.QualifiedOriginalName}");

            return string.Join(",", typeArgsDecls);
        }
    }

    [TypeMap("ISequence")]
    [TypeMap("IPtrSequence")]
    internal class ISequenceTypeMap : TemplatesTypeMap
    {
        protected override string ManagedInterfaceTypeName
        {
            get
            {
                return "SageUKAccounts50BusinessObjectsManagedWrappers::ISequence";
            }
        }

        protected override string NativeTemplateName
        {
            get
            {
                return "ISequence";
            }
        }
    }

    [TypeMap("IPtrEnumerator")]
    internal class IEnumeratorTypeMap : TemplatesTypeMap
    {
        protected override string ManagedInterfaceTypeName
        {
            get
            {
                return "SageUKAccounts50BusinessObjectsManagedWrappers::IEnumerator";
            }
        }

        protected override string NativeTemplateName
        {
            get
            {
                return "IEnumerator";
            }
        }
    }

    [TypeMap("IPersistentOrdFiltSequence")]
    [TypeMap("IPtrPersistentOrdFiltSequence")]
    internal class IPersistentOrdFiltSequenceTypeMap : TemplatesTypeMap
    {
        protected override string ManagedInterfaceTypeName
        {
            get
            {
                return "SageUKAccounts50BusinessObjectsManagedWrappers::IPersistentOrdFiltSequence";
            }
        }

        protected override string NativeTemplateName
        {
            get
            {
                return "IPersistentOrdFiltSequence";
            }
        }
    }

    [TypeMap("IPtrEnumerable")]
    internal class IEnumerableTypeMap : TemplatesTypeMap
    {
        protected override string ManagedInterfaceTypeName
        {
            get
            {
                return "SageUKAccounts50BusinessObjectsManagedWrappers::IEnumerable";
            }
        }

        protected override string NativeTemplateName
        {
            get
            {
                return "IEnumerable";
            }
        }
    }

    [TypeMap("IList")]
    internal class IListTypeMap : TemplatesTypeMap
    {
        protected override string ManagedInterfaceTypeName
        {
            get
            {
                return "SageUKAccounts50BusinessObjectsManagedWrappers::IList";
            }
        }

        protected override string NativeTemplateName
        {
            get
            {
                return "IList";
            }
        }
    }

    [TypeMap("IOrdFiltSequence")]
    internal class IOrdFiltSequenceTypeMap : TemplatesTypeMap
    {
        protected override string ManagedInterfaceTypeName
        {
            get
            {
                return "SageUKAccounts50BusinessObjectsManagedWrappers::IOrdFiltSequence";
            }
        }

        protected override string NativeTemplateName
        {
            get
            {
                return "IOrdFiltSequence";
            }
        }
    }
}
