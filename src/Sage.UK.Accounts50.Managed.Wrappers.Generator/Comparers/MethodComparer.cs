using CppSharp.AST;
using CppSharp.Types;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Comparers
{
    /// <summary>
    /// Compares two methods based on their name, return type, const modifier, and parameters.
    /// </summary>
    internal class MethodComparer : IEqualityComparer<Method>
    {
        public static readonly MethodComparer Instance = new MethodComparer();

        /// <summary>
        /// Must be set at startup to the type maps from CppSharp. Used to cater for scenario where the types we are comparing for equality are
        /// mapped to another type.
        /// </summary>
        public static TypeMapDatabase TypeMaps { get; set; }

        private MethodComparer()
        {
        }

        /// <summary>
        /// Determine if both methods are equal.
        /// </summary>
        /// <param name="x">First method.</param>
        /// <param name="y">Second method.</param>
        /// <returns>true if both methods are equal.</returns>
        public bool Equals(Method x, Method y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null && y != null)
            {
                return false;
            }

            if (x != null && y == null)
            {
                return false;
            }

            QualifiedType m1RetType = x.ReturnType;
            QualifiedType m2RetType = y.ReturnType;

            return x.Name.Equals(y.Name)
                && m1RetType.Type.IsEqualTo(m2RetType.Type, TypeMaps)
                && x.IsConst == y.IsConst
                && x.Parameters.IsEqualTo(y.Parameters);
        }

        /// <summary>
        /// Get hash code for method using the return type name, hash code of params, and const modifier.
        /// </summary>
        /// <param name="obj">Method to get hash code for.</param>
        /// <returns>Hash code of method</returns>
        public int GetHashCode(Method obj)
        {
            if (obj == null)
            {
                return 0;
            }

            int hashValue = obj.Name.GetHashCode() ^ obj.ReturnType.Type.ToString().GetHashCode();

			hashValue = obj.Parameters.Aggregate(hashValue, (p1, p2) => p1 ^ ParameterTypeComparer.Instance.GetHashCode(p2));

            return obj.IsConst.GetHashCode() ^ hashValue;
        }
    }
}
