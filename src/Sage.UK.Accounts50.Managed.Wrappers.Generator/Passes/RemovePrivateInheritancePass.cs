using CppSharp.AST;
using CppSharp.Passes;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Passes
{
    /// <summary>
    /// Removes bases from generated classes that are inherited as private. We cannot access instances of privately inherited base, so no point
    /// in generating them in the wrappers as the .NET environment won't be able to use them.
    /// </summary>
    internal class RemovePrivateInheritancePass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.Ignore)
            {
                @class.Bases.RemoveAll(x => x.Access == AccessSpecifier.Private);

                return true;
            }

            return base.VisitClassDecl(@class);
        }

    }
}
