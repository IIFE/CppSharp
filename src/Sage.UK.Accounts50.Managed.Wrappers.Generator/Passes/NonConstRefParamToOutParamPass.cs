using CppSharp.AST;
using CppSharp.Passes;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Passes
{
    /// <summary>
    /// Ensures that if a non-const parameter is passed by reference, then the managed equivalent is passed as an out parameter.
    /// </summary>
    /// <remarks>
    /// Targets the following parameter types:
    /// - Primitive types
    /// - Common Intermediate Language (CIL) types
    /// 
    /// Wrapped types (i.e. Sage 50 types wrapped in managed class) don't need to be targeted because they wrap a pointer to the type, and that pointer
    /// can be passed as reference (by dereferencing), and any changes made to the reference are reflected on the data being pointed to.
    /// </remarks>
    internal class NonConstRefParamToOutParamPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            if (!method.Ignore && !method.IsConstructorMethod() && method.Parameters.Count > 0)
            {
                method.SetNonConstRefParamsAsOut(Context.TypeMaps);
                return true;
            }

            return base.VisitMethodDecl(method);
        }
    }
}
