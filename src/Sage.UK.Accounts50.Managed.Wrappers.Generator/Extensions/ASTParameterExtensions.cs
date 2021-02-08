using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Types;
using System.Collections.Generic;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions
{
    /// <summary>
    /// Provides extended functionality on <see cref="Parameter"/>.
    /// </summary>
    internal static class ASTParameterExtensions
    {
        /// <summary>
        /// Determines if the first list of parameters are equal to the second one.
        /// </summary>
        /// <param name="p1">First list of parameters.</param>
        /// <param name="p2">Second list of parameters</param>
        /// <returns>true if both lists of parameters are sequentially equal.</returns>
        public static bool IsEqualTo(this IEnumerable<Parameter> p1, IEnumerable<Parameter> p2)
        {
            if (p1 == null || p2 == null)
            {
                return false;
            }

            return p1.SequenceEqual(p2, Comparers.ParameterTypeComparer.Instance);
        }

        /// <summary>
        /// Determines if parameter can be passed as an out parameter.
        /// </summary>
        /// <param name="parameter">Parameter to check.</param>
        /// <param name="typeMaps">Type map used to check if parameter type is mapped to another type.</param>
        /// <returns>true if parameter can be passed as out.</returns>
        public static bool ConvertibleToOutParam(this Parameter parameter, TypeMapDatabase typeMaps)
        {
            if (parameter == null || typeMaps == null)
            {
                return false;
            }

            Type paramType = parameter.Type.Desugar().GetDesugaredFinalPointeeElseType();

            Type mappedType = parameter.Type.GetMappedType(typeMaps, GeneratorKind.CLI);

            return (!parameter.Ignore || mappedType != null)
                && parameter.Type.Desugar().IsReference()
                && !parameter.IsConst
                && (paramType.IsPrimitiveType() || paramType.IsEnum() || mappedType is CILType);
        }

        /// <summary>
        /// Select all parameters that can be passed as out parameters.
        /// </summary>
        /// <param name="parameters">Parameters to check.</param>
        /// <param name="typeMaps">Type map used to check if parameter type is mapped to another type.</param>
        /// <returns>List of parameters that can be converted to out parameters.</returns>
        public static IEnumerable<Parameter> SelectConvertibleToOutParams(this IEnumerable<Parameter> parameters, TypeMapDatabase typeMaps)
        {
            return parameters.Where(x => x.ConvertibleToOutParam(typeMaps));
        }
    }
}
