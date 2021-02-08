using CppSharp.AST;
using CppSharp.Passes;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;
using System.Collections.Generic;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Passes
{
    /// <summary>
    /// In some instances we may have base classes that are themsleves ignored, but some of their methods are valid and should be generated. This pass
    /// handles that case.
    /// </summary>
    /// <remarks>
    /// Must be run before <see cref="RemoveOverridePass"/>.
    /// </remarks>
    internal class FlattenIgnoredBaseClassesMethods : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.Ignore)
            {
                // Don't care about IRefCountObject and CRefCountObject methods.
                @class.AddValidMethodsFromIgnoredBases(new List<string> { "IRefCountObject", "CRefCountObject" }, Context.TypeMaps);
                return true;
            }

            return base.VisitClassDecl(@class);
        }
    }
}
