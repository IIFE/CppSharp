using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Passes;
using CppSharp.Types;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Comparers;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Extensions;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.Passes;
using Sage.UK.Accounts50.Managed.Wrappers.Generator.TypeMaps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sage.UK.Accounts50.Managed.Wrappers.Generator.Generators
{
    internal class BusObjsManagedWrapperGenerator : ILibrary
    {
        private const string ModuleName = "Sage.UK.Accounts50.BusinessObjects.Managed.Wrappers";

        private const string ManagedWrapperNamespace = "SageUKAccounts50BusinessObjectsManagedWrappers";

        private const string IncludeProjName = "Include";

        private const string HeadersToParseFileName = "HeadersToParse2.h";

        private const string OutputFolderName = "Generated2";

        private const string IRefCountObjectClassName = "IRefCountObject";
        private const string CRefCountObjectClassName = "CRefCountObject";

        private const string GeneratedNativePtrFieldName = "NativePtr";

        private const string BusObjsProjectName = "BusObjs";

        private const string BusObjsCollectionSuffix = "Collection";

        private const string StdStringType = "basic_string<char, char_traits<char>, allocator<char>>";
        private const string ConstCharPtrType = "const char*";

        private const string TargetTriple = "x86_64-pc-win32-msvc";

        private const string GlobalVarMapClassName = "globvarmap";

        private string SolutionDir { get; }

        public BusObjsManagedWrapperGenerator(string solutionDir)
        {
            if (string.IsNullOrEmpty(solutionDir))
            {
                throw new System.ArgumentNullException(nameof(solutionDir));
            }

            SolutionDir = solutionDir;
        }

        public void Setup(Driver driver)
        {
            DriverOptions options = driver.Options;            
            options.GeneratorKind = GeneratorKind.CLI;
            options.GenerateClassTemplates = true;

            Module module = options.AddModule(ModuleName);
            module.OutputNamespace = ManagedWrapperNamespace;
            module.IncludeDirs.Add(Path.Combine(SolutionDir, ModuleName));
            module.IncludeDirs.Add(Path.Combine(SolutionDir, IncludeProjName));
            module.IncludeDirs.Add(SolutionDir);
            module.Headers.Add(HeadersToParseFileName);
            options.OutputDir = Path.Combine(SolutionDir, $@"{ModuleName}\{OutputFolderName}");

            options.CompileCode = false;
            driver.ParserOptions.TargetTriple = TargetTriple;
            driver.ParserOptions.MicrosoftMode = true;
            // driver.ParserOptions.SkipPrivateDeclarations = true;
            driver.ParserOptions.SkipFunctionBodies = true;
            driver.ParserOptions.SetupMSVC(VisualStudioVersion.VS2019);
            driver.ParserOptions.AddIncludeDirs(SolutionDir);

            driver.Options.GenerateFinalizers = true;

            // GenerateBusObjsIncludes();
        }

        /// <summary>
        /// Called before any parsing takes place.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="ctx"></param>
        public void Preprocess(Driver driver, ASTContext ctx)
        {
            // Hook into this event in order to transform some of the generated methods/consturctors/destructors/finalizers, etc.
            driver.Generator.OnUnitGenerated += OnUnitGenerated;

            OverrideDefaultTypemaps(driver.Context.TypeMaps);

            // The CppSharp implementation does not work for CLI. Remove it and we'll need to generate our own if we need it.
            // driver.Context.TypeMaps.TypeMaps.Remove("std::map");

            // We don't want those default passes from CppSharp to be run as they modify generated code in ways we don't want.
            driver.Context.TranslationUnitPasses.Passes.RemoveAll(x =>
            {
                System.Type passType = x.GetType();

                return x.GetType() == typeof(CaseRenamePass)
                || x.GetType() == typeof(DelegatesPass)
                || x.GetType() == typeof(GetterSetterToPropertyPass)
                || x.GetType() == typeof(ConstructorToConversionOperatorPass)
                || x.GetType() == typeof(CheckDuplicatedNamesPass);
            });

            // Ensures the comparers have the type maps as they need it to detect if methods/parameters are equal.
            Comparers.ParameterTypeComparer.TypeMaps = driver.Context.TypeMaps;
            MethodComparer.TypeMaps = driver.Context.TypeMaps;
        }

        /// <summary>
        /// Called after parsing takes place and before code is generated.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="ctx"></param>
        /// <remarks>
        /// The reason we are ignoring headers here instead of PreProcess is because we need types from these headers to be parsed
        /// so that we can map them to something else. Then we can just say we don't want files for those headers to be generated.
        /// </remarks>
        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public void SetupPasses(Driver driver)
        {
            driver.AddTranslationUnitPass(new RemovePrivateInheritancePass());

            driver.AddTranslationUnitPass(new FlattenIgnoredBaseClassesMethods());

            driver.AddTranslationUnitPass(new SetNonGeneratedValidMethodsAsGeneratedPass());

            driver.AddTranslationUnitPass(new DisambiguateAmbiguousDerivedMethodsPass());

            driver.AddTranslationUnitPass(new RemoveOverridePass());

            driver.AddTranslationUnitPass(new NonConstRefParamToOutParamPass());

            driver.AddTranslationUnitPass(new RemoveUnsupportedSymbolsPass());
        }

        private void OverrideDefaultTypemaps(TypeMapDatabase typeMapDatabase)
        {

            // We have our own type map for std string.
            typeMapDatabase.TypeMaps[ConstCharPtrType] = new StringTypeMap();
            typeMapDatabase.TypeMaps[ConstCharPtrType].TypeMapDatabase = typeMapDatabase;

            typeMapDatabase.TypeMaps[StdStringType] = new StringTypeMap();
            typeMapDatabase.TypeMaps[StdStringType].TypeMapDatabase = typeMapDatabase;
        }

        private void OnUnitGenerated(GeneratorOutput output)
        {
            foreach (CodeGenerator template in output.Outputs)
            {
                CLISources sourcesTemplate = template as CLISources;
                if (sourcesTemplate != null)
                {
                    ModifyConstructorsBody(sourcesTemplate);

                    ModifyDestructorsBody(sourcesTemplate);

                    ModifyFinalizersBody(sourcesTemplate);

                    PreappendPrecompiledHeader(sourcesTemplate);
                }

                CLIHeaders headerTemplate = template as CLIHeaders;
                if (headerTemplate != null)
                {
                    AppendCustomTypesToForwardDeclSection(headerTemplate);
                }
            }
        }

        private bool IsRefCountObjectType(Class @class)
        {
            IEnumerable<BaseClassSpecifier> allBases = @class.Bases.Flatten(x => x.Class.Bases);

            return allBases.Any(x => x.Class.Name.Equals(IRefCountObjectClassName));
        }

        private void ModifyConstructorsBody(CLISources template)
        {
            foreach (Block block in template.FindBlocks(BlockKind.MethodBody))
            {
                Method method = block.Object as Method;
                Class @class = method.Namespace as Class;

                // Methods marked as constructors are ones that take in the managed parameters and construct the wrapper from these parameters.
                // These constructors send a nullptr as the NativePtr value to any bases, and construct the NativePtr themselves.
                // We need to call AddRef if the class is wrapping a RefCountObject instance, because we are taking a raw pointer to it.
                // In the finalizer we call Release to ensure we don't leak memory.

                // We don't call AddRef if the constructor is receiving a native pointer and passing it along to a base class, because that call will be
                // made when the native pointer reaches the lowest base class, which is what the second loop in this function caters for.
                if (method != null && method.IsConstructor && IsRefCountObjectType(@class))
                {
                    block.WriteLine($"if({GeneratedNativePtrFieldName})");
                    block.WriteOpenBraceAndIndent();
                    block.WriteLine($"static_cast<::{@class.QualifiedOriginalName}*>({GeneratedNativePtrFieldName})->AddRef();");
                    block.UnindentAndWriteCloseBrace();
                }
            }

            foreach (Block block in template.FindBlocks(BlockKind.ConstructorBody))
            {
                Class @class = block.Object as Class;
                bool hasBase = @class.HasBase && @class.Bases[0].Class.IsGenerated;

                // Constructors that are not marked as MethodBody are ones that take native pointer to wrapped class and possibly pass it along to the base
                // if the class has a base. If the class does not have a base, then this constructor would be the one where we are setting the NativePtr
                // value. In this case we make sure we call AddRef for RefCountObject pointers.
                if (IsRefCountObjectType(@class) && !hasBase && block.Parent.Kind != BlockKind.MethodBody)
                {
                    block.WriteLine($"if({GeneratedNativePtrFieldName})");
                    block.WriteOpenBraceAndIndent();
                    block.WriteLine($"static_cast<::{@class.QualifiedOriginalName}*>({GeneratedNativePtrFieldName})->AddRef();");
                    block.UnindentAndWriteCloseBrace();
                }
            }
        }

        private void ModifyDestructorsBody(CLISources template)
        {
            foreach (Block block in template.FindBlocks(BlockKind.DestructorBody))
            {
                Class @class = block.Object as Class;
                if (@class != null)
                {
                    // Call the finalizer from the constructor. The finalizer gets called by the .NET garbage collector, so all the cleanup logic should be
                    // there so that we can reuse it for both when .NET wants to clean up, and when we want to cleanup by using the disposable pattern,
                    // which call the destructor.
                    block.Text.StringBuilder.Clear();
                    block.WriteLine($"this->!{@class.Name}();");
                }
            }
        }

        private void ModifyFinalizersBody(CLISources template)
        {
            foreach (Block block in template.FindBlocks(BlockKind.FinalizerBody))
            {
                Class @class = block.Object as Class;
                if (@class != null)
                {
                    // Cleanup should only happen in the class that has the native pointer field. With CppSharp the native pointer is 
                    bool generateClassNativeField = CLIGenerator.ShouldGenerateClassNativeField(@class);

                    if (generateClassNativeField || @class.HasNonTrivialDestructor)
                    {
                        block.Text.StringBuilder.Clear();

                        bool isRefCountObjectType = IsRefCountObjectType(@class);

                        // If we are not a RefCountObject wrapper, then we first check if the wrapper is the owner of the native pointer.
                        if (!isRefCountObjectType)
                        {
                            block.WriteLine($"if ({Helpers.OwnsNativeInstanceIdentifier})");
                            block.WriteOpenBraceAndIndent();
                        }

                        block.WriteLine($"if ({GeneratedNativePtrFieldName})");

                        block.WriteOpenBraceAndIndent();

                        block.WriteLine($"auto __nativePtr = {GeneratedNativePtrFieldName};");

                        block.WriteLine($"{GeneratedNativePtrFieldName} = nullptr;");

                        // If we are a RefCountObject we should call Release instead of delete, because we would have called AddRef in the constructors.
                        if (isRefCountObjectType)
                        {
                            block.WriteLine($"static_cast<::{@class.QualifiedOriginalName}*>(__nativePtr)->Release();");
                        }
                        else if (generateClassNativeField)
                        {
                            block.WriteLine("delete __nativePtr;");
                        }
                        else if (@class.HasNonTrivialDestructor)
                        {
                            // If we are calling the finalizer on a derived class, make sure to cast the native pointer to that class when deleting to cater
                            // for scenarios where destructor is not virtual from the base class.
                            block.WriteLine("delete static_cast<::{0}*>(__nativePtr);", @class.QualifiedOriginalName);
                        }

                        block.UnindentAndWriteCloseBrace();

                        if (!isRefCountObjectType)
                        {
                            block.UnindentAndWriteCloseBrace();
                        }
                    }
                }
            }
        }

        private void PreappendPrecompiledHeader(CLISources template)
        {
            foreach (Block block in template.FindBlocks(BlockKind.Includes))
            {
                string existing = block.Blocks[0].Text.StringBuilder.ToString();
                block.Blocks[0].Text.StringBuilder.Clear();
                block.Blocks[0].WriteLine("#include \"Stdafx.h\"");
                block.Blocks[0].WriteLine(existing);
            }
        }

        private void AppendCustomTypesToForwardDeclSection(CLIHeaders template)
        {
            foreach (Block block in template.FindBlocks(BlockKind.ForwardReferences))
            {
                block.WriteLine($"namespace {ManagedWrapperNamespace}");
                block.WriteOpenBraceAndIndent();
                block.WriteLine("generic<typename T> interface class ISequence;");
                block.WriteLine("generic<typename T> interface class IEnumerator;");
                block.WriteLine("generic<typename T> interface class IList;");
                block.WriteLine("generic<typename T> interface class IOrdFiltSequence;");
                block.WriteLine("generic<typename T> interface class IPersistentOrdFiltSequence;");
                block.UnindentAndWriteCloseBrace();
                break;
            }
        }


        private void GenerateBusObjsIncludes()
        {            
            // Generate an include file that includes all business object headers.
            string[] busObjs = Directory.GetFiles(Path.Combine(SolutionDir, BusObjsProjectName), "*.h");

            StringBuilder sb = new StringBuilder();

            string busObjsIncludesFile = Path.Combine(SolutionDir, $"{ModuleName}/{OutputFolderName}/{BusObjsProjectName}.h");

            FileInfo fi = new FileInfo(busObjsIncludesFile);

            sb.AppendLine("#pragma once");
            foreach (string busObj in busObjs)
            {
                string res = busObj.Replace(@"\", "/");

                FileInfo info = new FileInfo(res);

                if (!info.Name.Contains("AutoGenerated") && !info.Name.Contains("ForwardDecl") && !info.Name.StartsWith("Mock") && info.Name != "Dll.h")
                {
                    string fileNameWithoutEx = Path.GetFileNameWithoutExtension(info.Name);
                    string collectionEquiv = $"{info.DirectoryName}/{fileNameWithoutEx}{BusObjsCollectionSuffix}.h";

                    // If there is another file with the same name but has Collection suffix, then we know this file we're looking at now
                    // is a business object header. Therefore, add an include to it and its collection header to the generated header file.
                    if (File.Exists(collectionEquiv))
                    {
                        sb.AppendLine($"#include \"{info.Directory.Name}/{info.Name}\"");
                        sb.AppendLine($"#include \"{info.Directory.Name}/{fileNameWithoutEx}{BusObjsCollectionSuffix}.h\"");
                    }
                }
            }

            string busObjsIncludes = sb.ToString();

            if (!fi.Exists || fi.Length != busObjsIncludes.Length ||
                File.ReadAllText(busObjsIncludesFile) != busObjsIncludes)
            {
                File.WriteAllText(busObjsIncludesFile, busObjsIncludes);
            }
        }

    }
}
