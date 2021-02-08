using CppSharp.AST;
using CppSharp.Types;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Comparers;
using System.Collections.Generic;
using System.Linq;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions
{
    /// <summary>
    /// Provides extended functionality on <see cref="Class"/>.
    /// </summary>
    internal static class ASTClassExtensions
    {
        /// <summary>
        /// Get all methods for the specified base classes.
        /// </summary>
        /// <param name="classes">Base classes to get all methods for.</param>
        /// <returns>List of methods from all the specified base classes.</returns>
        public static IEnumerable<Method> GetAllMethods(this IEnumerable<BaseClassSpecifier> classes)
        {
            if (@classes == null)
            {
                return Enumerable.Empty<Method>();
            }

            return classes.SelectMany(x => x.Class?.Methods ?? new List<Method>());
        }

        /// <summary>
        /// Get all ignored methods for the specified base classes.
        /// </summary>
        /// <param name="classes">Base classes to get all ignored methods for.</param>
        /// <returns>List of ignored methods from all the specified base classes.</returns>
        public static IEnumerable<Method> GetAllIgnoredMethods(this IEnumerable<BaseClassSpecifier> classes)
        {
            if (@classes == null)
            {
                return Enumerable.Empty<Method>();
            }

            return classes.GetAllMethods().Where(x => x.Ignore);
        }

        /// <summary>
        /// Recursively get all bases for the class, which includes bases of the class bases themselves.
        /// </summary>
        /// <param name="class">Class to get all bases for.</param>
        /// <returns>Combined list of all bases for the class.</returns>
        public static IEnumerable<BaseClassSpecifier> DeepGetAllBases(this Class @class)
        {
            if(@class == null)
			{
                return Enumerable.Empty<BaseClassSpecifier>();
			}

            return @class.Bases?.Flatten(x => x.Class?.Bases) ?? new List<BaseClassSpecifier>();
        }

        /// <summary>
        /// Gets a list of all ignored methods for the specified class as well as all methods from all of the class bases, including bases of the class
        /// bases themselves.
        /// </summary>
        /// <param name="class">Class to get all ignored methods for.</param>
        /// <returns>List of all ignored methods all the way to the last base.</returns>
        public static IEnumerable<Method> DeepGetAllBasesIgnoredMethods(this Class @class)
        {
            if (@class == null)
            {
                return Enumerable.Empty<Method>();
            }

            return @class.DeepGetAllBases().SelectMany(x => x.Class?.Methods ?? new List<Method>()).Where(x => x.Ignore);
        }

        /// <summary>
        /// Get a list of all private/protected methods for the specified class as well as all private/protected methods from all of the class bases,
        /// including bases of the class bases themselves.
        /// </summary>
        /// <param name="class">Class to get all private/protected methods for.</param>
        /// <returns>List of all private/protected methods, all the way to the last base.</returns>
        public static IEnumerable<Method> DeepGetAllBasesPrivateProtectedMethods(this Class @class)
        {
            if (@class == null)
            {
                return Enumerable.Empty<Method>();
            }

            return @class.DeepGetAllBases().SelectMany(x => x.Class?.Methods ?? new List<Method>()).Where(x => x.Access == AccessSpecifier.Private || x.Access == AccessSpecifier.Protected);
        }

        /// <summary>
        /// Recursively get all bases for the class, except the ones with names specified in <paramref name="basesNamesToExclude"/>.
        /// </summary>
        /// <param name="class">Class to get all bases for except specified ones.</param>
        /// <returns>Combined list of all bases for the class except the specified ones.</returns>
        public static IEnumerable<BaseClassSpecifier> DeepGetAllBasesExcept(this Class @class, IEnumerable<string> basesNamesToExclude)
        {
            if (@class == null)
            {
                return Enumerable.Empty<BaseClassSpecifier>();
            }

            if(basesNamesToExclude == null)
			{
                return @class.DeepGetAllBases();
			}

            return @class.DeepGetAllBases().Where(x => !basesNamesToExclude.Contains(x.Class?.Name));
        }

        /// <summary>
        /// Recursively get all ignored bases for the class, which includes ignored bases of the class bases themselves.
        /// </summary>
        /// <param name="class">Class to get all ignored bases for.</param>
        /// <returns>Combined list of all ignored bases for the class.</returns>
        public static IEnumerable<BaseClassSpecifier> DeepGetAllIgnoredBases(this Class @class)
        {
            if (@class == null)
            {
                return Enumerable.Empty<BaseClassSpecifier>();
            }

            return @class.DeepGetAllBases().Where(x => (x.Class?.Ignore).GetValueOrDefault());
        }

        /// <summary>
        /// Recursively get all ignored bases for the class, which includes ignored bases of the class bases themselves, except from the ones
        /// with names specified in <paramref name="basesNamesToExclude"/>.
        /// </summary>
        /// <param name="class">Class to get all ignored bases for, except for ones sepcified.</param>
        /// <returns>Combined list of all ignored bases for the class, except for ones specified.</returns>
        public static IEnumerable<BaseClassSpecifier> DeepGetAllIgnoredBasesExcept(this Class @class, IEnumerable<string> basesNamesToExclude)
        {
            if (@class == null)
            {
                return Enumerable.Empty<BaseClassSpecifier>();
            }

            if (basesNamesToExclude == null)
            {
                return @class.DeepGetAllBases();
            }

            return @class.DeepGetAllIgnoredBases()?.Where(x => !basesNamesToExclude.Contains(x.Class.Name));
        }

        /// <summary>
        /// Get all methods with override specified for the given class.
        /// </summary>
        /// <param name="class">Class to get override methods for.</param>
        /// <returns>List of methods from class with override specifier.</returns>
        public static IEnumerable<Method> GetAllOverrideMethods(this Class @class)
        {
            if (@class?.Methods == null)
            {
                return Enumerable.Empty<Method>();
            }

            return @class.Methods.Where(x => x.IsOverride && !x.IsConstructor && !x.IsDestructor && !x.IsCopyConstructor && !x.IsOperator);
        }

        /// <summary>
        /// Get the first base class for the given class.
        /// </summary>
        /// <param name="class">Class to get first base for.</param>
        /// <returns>First base for given class, null if there is no base for the class.</returns>
        public static Class GetFirstBase(this Class @class)
        {
            if (@class == null)
            {
                return null;
            }

            return @class.Bases.Count > 0 ? @class.Bases[0].Class : null;
        }

        /// <summary>
        /// Get every first base class, starting from the given class and walking towards each first base down the hierarchy.
        /// </summary>
        /// <param name="class">Class to start getting first base classes from.</param>
        /// <param name="firstBases">List to append each first base to.</param>
        public static void DeepGetFirstBases(this Class @class, IList<Class> firstBases)
        {
            if (firstBases == null)
            {
                return;
            }

            Class firstBase = @class?.GetFirstBase();

            if (firstBase != null) 
            {
                firstBases.Add(firstBase);

                firstBase.DeepGetFirstBases(firstBases);
            }
        }

        /// <summary>
        /// Get every first base class, starting from the given class and walking towards each first base down the hierarchy.
        /// </summary>
        /// <param name="class">Class to start getting first base classes from.</param>
        /// <returns>List of first base starting from the provided class.</returns>
        public static IEnumerable<Class> DeepGetFirstBases(this Class @class)
        {
            if (@class == null)
            {
                return Enumerable.Empty<Class>();
            }

            IList<Class> firstBases = new List<Class>();

            @class.DeepGetFirstBases(firstBases);

            return firstBases;
        }

        /// <summary>
        /// Get first non ignored bases and stops when it finds the first ignored base.
        /// </summary>
        /// <param name="class">Class to start getting first non ignored base classes from.</param>
        /// <returns>List of first base that is not ignored, starting from the provided class.</returns>
        public static IEnumerable<Class> DeepGetFirstNonIgnoredBases(this Class @class)
        {
            return @class.DeepGetFirstBases()?.TakeWhile(x => !x.Ignore);
        }

        /// <summary>
        /// Gets all methods from the first non ignored bases, stopping when it finds the first ignored base.
        /// </summary>
        /// <param name="class">Class to start getting methods of first non ignored base classes from.</param>
        /// <returns>List of methods from first non ignored bases, starting from the provided class.</returns>
        public static IEnumerable<Method> DeepGetAllMethodsFromFirstNonIgnoredBases(this Class @class)
        {
            return @class.DeepGetFirstNonIgnoredBases().SelectMany(x => x.Methods ?? new List<Method>());
        }

        /// <summary>
        /// First gets all methods from first non ignored bases, stopping when it finds the first ignored base. It then gets a list of non ignored
        /// methods from those bases that are marked as not ignored.
        /// </summary>
        /// <param name="class">Class to start getting methods of first non ignored base classes from, to then filter for only non ignored methods.</param>
        /// <returns>List of non ignored methods from first non ignored bases, starting from the provided class.</returns>
        public static IEnumerable<Method> DeepGetNonIgnoredMethodsFromFirstNonIgnoredBases(this Class @class)
        {
            return @class.DeepGetAllMethodsFromFirstNonIgnoredBases().Where(x => !x.Ignore);
        }

        /// <summary>
        /// First gets all non ignored methods from first non ignored bases, stopping when it finds the first ignored base. 
        /// It then gets a list of virtual methods from the result.
        /// </summary>
        /// <param name="class">Class to start getting non ignored methods of first non ignored base classes from, to then filter for only virtual methods.</param>
        /// <returns>List of non ignored virtual methods from first non ignored bases, starting from the provided class.</returns>
        public static IEnumerable<Method> DeepGetNonIgnoredVirtualMethodsFromFirstNonIgnoredBases(this Class @class)
        {
            return @class.DeepGetNonIgnoredMethodsFromFirstNonIgnoredBases().Where(x => x.IsVirtual);
        }

        /// <summary>
        /// Selects methods from a class that have an override modifier that shouldn't be there in the managed world.
        /// </summary>
        /// <param name="class">Class to select methods for with invalid override modifier.</param>
        /// <returns>List of methods with invalid override modifier.</returns>
        public static IEnumerable<Method> SelectMethodsWithInvalidOverride(this Class @class)
        {
            IEnumerable<Method> allBasesVirtualMethods = @class.DeepGetNonIgnoredVirtualMethodsFromFirstNonIgnoredBases();

            IEnumerable<Method> allIgnoredMethods = @class.DeepGetAllBasesIgnoredMethods();

            IEnumerable<Method> allPrivateProtectedMethods = @class.DeepGetAllBasesPrivateProtectedMethods();

            IEnumerable<Method> allOverrideMethods = @class.GetAllOverrideMethods();

            IEnumerable<Method> empty = new List<Method>();

            // Get all override methods that don't have a virtual equivalent in a non ignored base.
            IEnumerable<Method> notInBaseAsVirtual = allBasesVirtualMethods != null
                ? allOverrideMethods.Except(allBasesVirtualMethods, MethodComparer.Instance) : empty;

            // Get all override methods that exist in the ignored list of methods from all of the class bases.
            IEnumerable<Method> inIgnored = allIgnoredMethods != null
                ? allOverrideMethods.Intersect(allIgnoredMethods, MethodComparer.Instance) : empty;

            IEnumerable<Method> inPrivateProtected = allPrivateProtectedMethods != null
                ? allOverrideMethods.Intersect(allPrivateProtectedMethods, MethodComparer.Instance) : empty;

            return notInBaseAsVirtual.Concat(inIgnored).Concat(inPrivateProtected);
        }

        /// <summary>
        /// Remove override flag from methods that should not be specified as overside anymore.
        /// </summary>
        /// <param name="class">Class to modify methods for.</param>
        public static void RemoveInvalidOverrideFromMethods(this Class @class)
        {
            IEnumerable<Method> methodsWithInvalidOverride = @class.SelectMethodsWithInvalidOverride();

            foreach (Method method in methodsWithInvalidOverride)
            {
                method.IsOverride = false;
            }
        }

        /// <summary>
        /// Gets all methods that are not ignored for the specified class.
        /// </summary>
        /// <param name="class">Class to get non ignored methods for.</param>
        /// <returns>List of non ignored methods.</returns>
        public static IEnumerable<Method> GetNonIgnoredMethods(this Class @class)
        {
            if (@class == null)
            {
                return Enumerable.Empty<Method>();
            }

            return @class.Methods.Where(x => !x.Ignore
            && !x.IsConstructionRelatedMethod()
            && !x.IsOperator);
        }

        /// <summary>
        /// Add a prefix to methods that will cause ambiguity with other bases methods.
        /// </summary>
        /// <param name="class">Class to modify methods for.</param>
        public static void PreappendAmbiguousDerivedMethodsWithUnderscore(this Class @class)
        {
            IEnumerable<Method> allBasesMethods = @class.DeepGetNonIgnoredMethodsFromFirstNonIgnoredBases();

            IEnumerable<Method> classUnignoredPublicMethods = @class.GetNonIgnoredMethods();

            IEnumerable<Method> ambiguousMethods = classUnignoredPublicMethods.Where(x 
                => allBasesMethods.Any(y => x.IsAmbiguousWithRegardsTo(y))
                || x.IsAmbiguousWithRegardsToSystemObjectMethod());

            foreach (Method method in ambiguousMethods)
            {
                method.Name = "_" + method.Name;
            }
        }

        /// <summary>
        /// Adds methods to class that are actually valid but happen to have ignored classes that are not generated.
        /// </summary>
        /// <param name="class">Class to add methods to.</param>
        /// <param name="basesNamesToExclude">Bases names to exclude from check.</param>
        /// <param name="typeMaps">Type map used to check if a type is mapped to another type.</param>
        public static void AddValidMethodsFromIgnoredBases(this Class @class, IEnumerable<string> basesNamesToExclude, TypeMapDatabase typeMaps)
        {
            bool hasRefBase = @class.HasRefBase();

            int basesCount = @class.Bases.Count;

            if (basesCount == 0 || (hasRefBase && basesCount == 1))
            {
                return;
            }

            if (hasRefBase)
            {
                IEnumerable<string> firstBasesNames = @class.DeepGetFirstBases().Select(x => x.Name);
                basesNamesToExclude = basesNamesToExclude == null ? firstBasesNames : basesNamesToExclude.Concat(firstBasesNames);
            }

            IEnumerable<BaseClassSpecifier> allBases = @class.DeepGetAllBasesExcept(basesNamesToExclude);

            IEnumerable<Method> allMethods = allBases.GetAllMethods();

            IEnumerable<Method> validMethods = allMethods.Where(x =>
            !x.IsConstructionRelatedMethod()
            && !x.IsOperator
            && x.AreIgnoredParamsMapped(typeMaps)
            && x.IsReturnTypeNotIgnored(typeMaps));

            IEnumerable<Method> validMethodsNotInClass = validMethods.Except(@class.Methods, MethodComparer.Instance);

            foreach (Method method in validMethodsNotInClass)
            {
                method.Ignore = false;
                method.GenerationKind = GenerationKind.Generate;
                method.IsOverride = false;
                method.IsVirtual = false;

                @class.Methods.Add(method);
            }
        }

        /// <summary>
        /// Goes through a class methods and checks if any of the ignored methods on the class should actually be generated because they look valid.
        /// </summary>
        /// <param name="class">Class to unignore methods for</param>
        /// <param name="basesNamesToExclude">Any of the class bases to not unignore methods for.</param>
        /// <param name="typeMaps">Used to detect if we have a type mapped to an ignored type.</param>        
        public static void SetNonGeneratedValidMethodsAsGenerated(this Class @class, IEnumerable<string> basesNamesToExclude, TypeMapDatabase typeMaps)
        {
            IEnumerable<BaseClassSpecifier> ignoredBases = @class.DeepGetAllIgnoredBasesExcept(basesNamesToExclude);

            IEnumerable<Method> basesIgnoredMethods = ignoredBases.GetAllIgnoredMethods();

            // Get all methods for the class that have mapped parameter types or return type is not ignored.
            IEnumerable<Method> validMethods = @class.Methods.Where(x =>
            !x.IsOperator
            && x.AreIgnoredParamsMapped(typeMaps)
            && x.IsReturnTypeNotIgnored(typeMaps));           

            foreach (Method method in validMethods)
            {
                method.GenerationKind = GenerationKind.Generate;
                method.Ignore = false;
            }

            // If methods are ignored because the base they came from is ignored, but they are valid methods and should be used in our non ignored class,
            // then we should allow them to be generated.
            IEnumerable<Method> validMethodsDerivedFromIgnoredBases = validMethods
                .Where(x => x.IsVirtual)
                .Intersect(basesIgnoredMethods, MethodComparer.Instance);

            foreach (Method method in validMethodsDerivedFromIgnoredBases)
            {
                method.GenerationKind = GenerationKind.Generate;
                method.Ignore = false;
                method.IsVirtual = false;
                method.IsOverride = false;
            }
        }
    }
}
