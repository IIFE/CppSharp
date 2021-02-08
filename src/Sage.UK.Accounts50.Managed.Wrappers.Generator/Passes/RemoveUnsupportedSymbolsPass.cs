using CppSharp.AST;
using CppSharp.Passes;
using System.Collections.Generic;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Passes
{
    /// <summary>
    /// Removes any symbols (fields, properties, functions, methods, classes) that are invalid and should not be generated.
    /// 
    /// Reasons can be:
    /// Class is not exported. If we are taking the class from a static lib, add a filter to this pass to not consider such classes.
    /// Free function is not exported. Same as above.
    /// 
    /// Field has no name (anonymous unions, urgh).
    /// 
    /// Class method is default constructor/destructor/copy constructor.
    /// 
    /// Operator methods.
    /// </summary>
    internal class RemoveUnsupportedSymbolsPass : TranslationUnitPass
    {
        public override bool VisitDeclarationContext(DeclarationContext context)
        {
            if (context is Namespace)
            {
                foreach (Function func in context.Functions)
                {
                    if (!func.Ignore && !func.IsInline)
                    {
                        IEnumerable<MacroExpansion> exported = func.PreprocessedEntities
                            ?.Select(x => x as MacroExpansion)
                            ?.Where(x => x != null && x.MacroLocation == MacroLocation.FunctionHead
                            && ((x.Definition?.Expression?.Contains("__declspec")).GetValueOrDefault()
                            || (x.Definition?.Expression?.Contains("_API")).GetValueOrDefault()));

                        if (exported == null || !exported.Any())
                        {
                            func.Ignore = true;
                        }
                    }
                }
            }

            return base.VisitDeclarationContext(context);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.Ignore)
            {
                @class.Fields.RemoveAll(x => string.IsNullOrEmpty(x.Name));
                @class.Layout.Fields.RemoveAll(x => string.IsNullOrEmpty(x.Name));

                @class?.Methods.RemoveAll(x => !x.Ignore && !x.IsDestructor && x.IsDefaulted);
                @class?.Methods.RemoveAll(x => (x.IsConstructor || x.IsCopyConstructor) && x.IsDeleted);
                @class?.Methods.RemoveAll(x => x.Access == AccessSpecifier.Private || x.Access == AccessSpecifier.Protected);

                IEnumerable<Method> noIgnoredInstanceMethods = @class.Methods.Where(x => !x.Ignore && !x.IsDestructor);

                if (noIgnoredInstanceMethods.Any())
                {
                    IEnumerable<MacroExpansion> exported = @class.PreprocessedEntities
                        ?.Select(x => x as MacroExpansion)
                        ?.Where(x => x != null && x.MacroLocation == MacroLocation.ClassHead
                        && (x.Definition?.Expression?.Contains("__declspec")).GetValueOrDefault());

                    if (exported == null || !exported.Any())
                    {
                        IEnumerable<Method> validMethods = @class.Methods.Where(x => !x.Ignore && !x.IsDestructor && (x.IsInline || x.IsPure));

                        if (!validMethods.Any() && !@class.Fields.Where(x => x.Access == AccessSpecifier.Public).Any())
                        {
                            @class.Ignore = true;
                        }
                        else
                        {
                            @class.Methods.RemoveAll(x => !x.IsInline && !x.IsConstructor && !x.IsCopyConstructor && !x.IsDestructor && !x.IsOperator
                            && !x.IsPure);
                        }
                    }
                }
            }

            return base.VisitClassDecl(@class);
        }
    }
}
