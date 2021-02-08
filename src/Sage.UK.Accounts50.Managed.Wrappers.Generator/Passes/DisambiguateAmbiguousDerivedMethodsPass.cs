using CppSharp.AST;
using CppSharp.Passes;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Passes
{
    /// <summary>
    /// Ensures that a class's method that is ambiguous with one of the base classes methods is preappended with an undercore to distinguish it.
    /// </summary>
    /// <remarks>
    /// Ambiguity can be caused if in the native world two methods exist in the class hierarchy that share the same name, but one of them has
    /// a different const qualifier. Such scenario doesn't exist in .NET, and will cause an invalid overload. Methods can also be ambigious if they
    /// have the same name as System::Object methods, but are const or have different returns.
    /// 
    /// Must be run before <see cref="RemoveOverridePass"/>.
    /// </remarks>
    internal class DisambiguateAmbiguousDerivedMethodsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.Ignore)
            {
                @class.PreappendAmbiguousDerivedMethodsWithUnderscore();
                return true;
            }

            return base.VisitClassDecl(@class);
        }
    }
}
