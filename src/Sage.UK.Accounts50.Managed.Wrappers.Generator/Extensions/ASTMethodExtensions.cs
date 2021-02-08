using CppSharp.AST;
using CppSharp.Types;
using System.Collections.Generic;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions
{
    /// <summary>
    /// Provides extended functionality on <see cref="Method"/>.
    /// </summary>
    internal static class ASTMethodExtensions
    {
        /// <summary>
        /// Determines if method is a constructor one.
        /// </summary>
        /// <param name="method">Method to check.</param>
        /// <returns>true if method is a constructor one.</returns>
        public static bool IsConstructorMethod(this Method method)
        {
            if (method == null)
            {
                return false;
            }

            return method.IsConstructor || method.IsCopyConstructor;
        }

        /// <summary>
        /// Determines if two methods will cause ambiguity in a managed environment if they are in the same context.
        /// </summary>
        /// <param name="m1">First method.</param>
        /// <param name="m2">Second method.</param>
        /// <returns>true if both methods will cause ambiguity.</returns>
        public static bool IsAmbiguousWithRegardsTo(this Method m1, Method m2)
        {
            if (m1 == null || m2 == null)
            {
                return false;
            }

            return m1.Name.Equals(m2.Name) 
                && (m1.IsConst != m2.IsConst || (!m1.IsVirtual && !m2.IsVirtual) || m1.IsVirtual != m2.IsVirtual)
                && m1.Parameters.IsEqualTo(m2.Parameters);
        }

        /// <summary>
        /// Determines if two methods will cause ambiguity in a managed environment if they match the name and parameters of a method from
        /// System::Object.
        /// </summary>
        /// <param name="m1">Method to check.</param>
        /// <returns>true if method will cause ambiguity by matching one of the methods from System::Object.</returns>
        public static bool IsAmbiguousWithRegardsToSystemObjectMethod(this Method method)
        {
            if (method == null)
            {
                return false;
            }

            return (method.Name.Equals(nameof(object.ToString)) || method.Name.Equals(nameof(object.GetType)))
                && (method.Parameters == null || method.Parameters.Count == 0);
        }

        /// <summary>
        /// Sets all non const parameters of a method to be an out parameter in managed world.
        /// </summary>
        /// <param name="method">Method to set parameters for.</param>
        /// <param name="typeMaps">Type map used to check if parameter type is mapped to another type.</param>
        public static void SetNonConstRefParamsAsOut(this Method method, TypeMapDatabase typeMaps)
        {
            IEnumerable<Parameter> parameters = method.Parameters.SelectConvertibleToOutParams(typeMaps);

            foreach (Parameter parameter in parameters)
            {
                parameter.Usage = ParameterUsage.InOut;
            }
        }

        /// <summary>
        /// Determines if ignored parameters of method are in fact mapped to another type that is not ignored.
        /// </summary>
        /// <param name="method">Method to check parameters for.</param>
        /// <param name="typeMaps">Type map used to check if parameter type is mapped to another type.</param>
        /// <returns>true if ignored parameters are mapped.</returns>
        public static bool AreIgnoredParamsMapped(this Method method, TypeMapDatabase typeMaps)
        {
            if (method == null || typeMaps == null)
            {
                return false;
            }

            IList<Parameter> ignoredParams = method.Parameters.Where(x => x.Ignore).ToList();

            if (ignoredParams != null && ignoredParams.Any())
            {
                return ignoredParams.All(x => x.Type.IsNotIgnoredType(typeMaps));
            }

            return true;
        }

        /// <summary>
        /// Checks if the method return type is ignored.
        /// </summary>
        /// <param name="method">Method to check return type for.</param>
        /// <param name="typeMaps">Type map used to check if method return type is mapped to another type.</param>
        /// <returns>true if return type is ignored.</returns>
        public static bool IsReturnTypeNotIgnored(this Method method, TypeMapDatabase typeMaps)
        {
            return method.ReturnType.Type.IsNotIgnoredType(typeMaps);
        }

        /// <summary>
        /// Checks if the method is a construction related one. This included a check for destructor.
        /// </summary>
        /// <param name="method">Method to check.</param>
        /// <returns>true if method is construction related.</returns>
        public static bool IsConstructionRelatedMethod(this Method method)
        {
            if (method == null)
            {
                return false;
            }

            return method.IsConstructorMethod()                
                || method.IsDestructor;
        }
    }
}
