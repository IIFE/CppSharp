using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Types;
using System.Collections.Generic;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions
{
    /// <summary>
    /// Provides extended functionality on <see cref="Type"/>.
    /// </summary>
    internal static class ASTTypeExtensions
    {
        /// <summary>
        /// Gets a list of AST types that represent the types of a class template.
        /// </summary>
        /// <param name="type">The class template type to get the template types from.</param>
        /// <returns>A list of types from the class template.</returns>
        public static IEnumerable<Type> GetTemplateTypeArgs(this Type type)
        {
            // Get the actual type in case the provided type is a typedef.
            Type actualType = type.Desugar();

            // If the actual type is a pointer, then get the actual type it's pointing to, and desugar in case that too is a typedef.
            if (actualType is PointerType)
            {
                actualType = actualType.GetFinalPointee().Desugar();
            }

            TemplateSpecializationType templateType = actualType as TemplateSpecializationType;

            return templateType != null 
                ? templateType.Arguments.Select(x => x.Type.Type)
                : new List<Type> { actualType };
        }

        /// <summary>
        /// Determines if the type is equal to another, using the type map database to check in case the types are mapped to another type.
        /// </summary>
        /// <param name="t1">Type to check if equal to another.</param>
        /// <param name="t2">Another type to check against the first one.</param>
        /// <param name="typeMaps">Type maps to use and check if each type's mapped type is equal to the other's mapped type.</param>
        /// <returns>true if both types' map to the same type.</returns>
        public static bool IsEqualTo(this Type t1, Type t2, TypeMapDatabase typeMaps)
        {
            if (t1 == null || t2 == null || typeMaps == null)
            {
                if (t1 == null && t2 == null)
                {
                    return true;
                }

                return false;
            }

            return t1.GetMappedType(typeMaps, GeneratorKind.CLI)
                .ToString()
                .Equals(t2.GetMappedType(typeMaps, GeneratorKind.CLI)
                .ToString());
        }

        /// <summary>
        /// Gets the final type that the given type points too, removing any typedef.
        /// </summary>
        /// <param name="t">Type to get the final pointee for.</param>
        /// <returns>Final pointee of given type.</returns>
        public static Type GetDesugaredFinalPointeeElseType(this Type t)
        {
            Type finalPointee = t.GetFinalPointee();

            return finalPointee != null ? finalPointee.Desugar() : t;
        }

        /// <summary>
        /// Determines if type is not ignored.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <param name="typeMaps">Type map used to check if parameter type is mapped to another type.</param>
        /// <returns>true if type is not ignored.</returns>
        public static bool IsNotIgnoredType(this Type type, TypeMapDatabase typeMaps)
        {
            Type mappedType = type.GetMappedType(typeMaps, GeneratorKind.CLI);

            Declaration decl = null;
            if (mappedType.TryGetDeclaration(out decl))
            {
                return !decl.Ignore;
            }
            else if (mappedType is CustomType)
            {
                return true;
            }

            return mappedType.IsPrimitiveType();
        }
    }
}
