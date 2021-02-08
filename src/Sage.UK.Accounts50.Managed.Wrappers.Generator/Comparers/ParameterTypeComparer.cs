using CppSharp.AST;
using CppSharp.Types;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;
using System.Collections.Generic;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Comparers
{
    /// <summary>
    /// Compares two parameters based on their types.
    /// </summary>
    internal class ParameterTypeComparer : IEqualityComparer<Parameter>
    {
        public static readonly ParameterTypeComparer Instance = new ParameterTypeComparer();

        /// <summary>
        /// Must be set at startup to the type maps from CppSharp. Used to cater for scenario where the types we are comparing for equality are
        /// mapped to another type.
        /// </summary>
        public static TypeMapDatabase TypeMaps { get; set; }

        private ParameterTypeComparer()
        {
        }

        /// <summary>
        /// Determine if both parameters are equal.
        /// </summary>
        /// <param name="x">First parameter.</param>
        /// <param name="y">Second parameter.</param>
        /// <returns>true if both parameters are equal.</returns>
        public bool Equals(Parameter x, Parameter y)
        {
            return x.Type.IsEqualTo(y.Type, TypeMaps);                
        }

        /// <summary>
        /// Get hash code for parameter based on its type string.
        /// </summary>
        /// <param name="obj">Parameter to get hash code for.</param>
        /// <returns>Hash code of parameter</returns>
        public int GetHashCode(Parameter obj)
        {
            return obj.Type.ToString().GetHashCode();
        }
    }
}
