using CppSharp.AST;
using CppSharp.Passes;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Passes
{
    /// <summary>
    /// Removes the override keyword from methods that were originally specified as override in the native type, but are no longer valid to be set
    /// as override because either the base method has been removed or ignored from the generated base class.
    /// </summary>
    /// <remarks>
    /// This pass should be run after <see cref="FlattenIgnoredBaseClassesMethods"/> and <see cref="DisambiguateAmbiguousDerivedMethodsPass"/> as they
    /// may add virtual methods from ignored base classes, or rename virtual methods because their name causes an overload conflict in .NET world.
    /// </remarks>
    internal class RemoveOverridePass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.Ignore)
            {
                @class.RemoveInvalidOverrideFromMethods();
                return true;
            }

            return base.VisitClassDecl(@class);
        }

    }
}
