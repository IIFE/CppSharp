using CppSharp.AST;
using CppSharp.Passes;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;
using System.Collections.Generic;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Passes
{
    /// <summary>
    /// Goes through a classes methods and sets ignored methods as generated if their return type and parameters are not ignored.
    /// </summary>
    internal class SetNonGeneratedValidMethodsAsGeneratedPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.Ignore)
            {
                // Always ignore IRefCountObject and CRefCountObject methods.
                @class.SetNonGeneratedValidMethodsAsGenerated(new List<string> { "IRefCountObject", "CRefCountObject" }, Context.TypeMaps);
                return true;
            }

            return base.VisitClassDecl(@class);
        }
    }
}
