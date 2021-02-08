using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Generators.CLI;
using CppSharp.Types;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps
{
    /// <summary>
    /// Type map to map std::set to .NET HashSet.
    /// </summary>
    [TypeMap("std::set", GeneratorKind = GeneratorKind.CLI)]
    public class Set : TypeMap
    {
        public override bool IsIgnored
        {
            get
            {
                Type finalType = Type.GetFinalPointee() ?? Type;
                TemplateSpecializationType type = finalType as TemplateSpecializationType;
                if (type == null)
                {
                    InjectedClassNameType injectedClassNameType = (InjectedClassNameType)finalType;
                    type = (TemplateSpecializationType)injectedClassNameType.InjectedSpecializationType.Type;
                }
                TypeIgnoreChecker checker = new TypeIgnoreChecker(TypeMapDatabase);
                type.Arguments[0].Type.Visit(checker);

                return checker.IsIgnored;
            }
        }

        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            var tempParams = ctx.GetTemplateParameterList();
            var protoParams = ctx.GetProtoTemplateParameterList();

            return new CustomType(
                $"System::Collections::Generic::HashSet<{tempParams}>^", protoParams == "char" ? "repeated sfixed32" : $"repeated {protoParams}");
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            TemplateSpecializationType templateType = Type as TemplateSpecializationType;
            QualifiedType type = templateType.Arguments[0].Type;
            bool isPointerToPrimitive = type.Type.IsPointerToPrimitiveType();
            Type managedType = isPointerToPrimitive
                ? new CILType(typeof(System.IntPtr))
                : type.Type;

            string entryString = (ctx.Parameter != null) ? ctx.Parameter.Name
                : ctx.ArgName;

            string tmpVarName = "_tmp" + entryString;

            CppTypePrinter cppTypePrinter = new CppTypePrinter(ctx.Context);
            TypePrinterResult nativeType = type.Type.Visit(cppTypePrinter);

            ctx.Before.WriteLine("auto {0} = std::set<{1}>();",
                tmpVarName, nativeType);
            ctx.Before.WriteLine("for each({0} _element in {1})",
                managedType, entryString);
            ctx.Before.WriteOpenBraceAndIndent();
            {
                Parameter param = new Parameter
                {
                    Name = "_element",
                    QualifiedType = type
                };

                MarshalContext elementCtx = new MarshalContext(ctx.Context, ctx.Indentation)
                {
                    Parameter = param,
                    ArgName = param.Name,
                };

                CLIMarshalManagedToNativePrinter marshal = new CLIMarshalManagedToNativePrinter(elementCtx);
                type.Type.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    ctx.Before.Write(marshal.Context.Before);

                if (isPointerToPrimitive)
                    ctx.Before.WriteLine("auto _marshalElement = {0}.ToPointer();",
                        marshal.Context.Return);
                else
                    ctx.Before.WriteLine("auto _marshalElement = {0};",
                    marshal.Context.Return);

                ctx.Before.WriteLine("{0}.insert(_marshalElement);",
                    tmpVarName);
            }

            ctx.Before.UnindentAndWriteCloseBrace();

            ctx.Return.Write(tmpVarName);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            Type t = Type is TypedefType ? (Type as TypedefType).Declaration.Type : Type;
            TemplateSpecializationType templateType = t as TemplateSpecializationType;
            QualifiedType type = templateType.Arguments[0].Type;
            bool isPointerToPrimitive = type.Type.IsPointerToPrimitiveType();
            Type managedType = isPointerToPrimitive
                ? new CILType(typeof(System.IntPtr))
                : type.Type;
            string tmpVarName = "_tmp" + ctx.ArgName;

            ctx.Before.WriteLine(
                "auto {0} = gcnew System::Collections::Generic::HashSet<{1}>();",
                tmpVarName, managedType);
            ctx.Before.WriteLine("for(auto _element : {0})",
                ctx.ReturnVarName);
            ctx.Before.WriteOpenBraceAndIndent();
            {
                MarshalContext elementCtx = new MarshalContext(ctx.Context, ctx.Indentation)
                {
                    ReturnVarName = "_element",
                    ReturnType = type
                };

                CLIMarshalNativeToManagedPrinter marshal = new CLIMarshalNativeToManagedPrinter(elementCtx);
                type.Type.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.Before))
                    ctx.Before.Write(marshal.Context.Before);

                ctx.Before.WriteLine("auto _marshalElement = {0};",
                    marshal.Context.Return);

                if (isPointerToPrimitive)
                    ctx.Before.WriteLine("{0}->Add({1}(_marshalElement));",
                        tmpVarName, managedType);
                else
                    ctx.Before.WriteLine("{0}->Add(_marshalElement);",
                        tmpVarName);
            }
            ctx.Before.UnindentAndWriteCloseBrace();

            ctx.Return.Write(tmpVarName);
        }
    }
}
