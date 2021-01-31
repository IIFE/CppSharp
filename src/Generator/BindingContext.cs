using CppSharp.AST;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Parser;
using System.Linq;
using System.Collections.Generic;

namespace CppSharp.Generators
{
    public class ProtoField
    {
        public string FieldName { get; set; }

        public string FieldType { get; set; }
    }

    public class ProtoEnum
    {
        public string Name { get; set; }

        public List<KeyValuePair<string, string>> Enums { get; set; } = new List<KeyValuePair<string, string>>();
    }

    public class ProtoMessage
    {
        public string MessageName { get; set; }

        public List<ProtoField> Fields { get; set; } = new List<ProtoField>();
    }

    public class Proto
    {
        public Dictionary<string, List<ProtoMessage>> Messages { get; set; } = new Dictionary<string, List<ProtoMessage>>();

        public Dictionary<string, List<ProtoEnum>> Enums { get; set; } = new Dictionary<string, List<ProtoEnum>>();

        public void AddEnum(string @namespace, ProtoEnum @enum)
        {
            if(Enums.TryGetValue(@namespace, out List<ProtoEnum> enums))
            {
                enums.Add(@enum);
            }
            else
            {
                Enums.Add(@namespace, new List<ProtoEnum> { @enum });
            }
        }

        public void AddMessage(string @namespace, ProtoMessage message)
        {
            if (Messages.TryGetValue(@namespace, out List<ProtoMessage> messages))
            {
                messages.Add(message);
            }
            else
            {
                Messages.Add(@namespace, new List<ProtoMessage> { message });
            }
        }
    }

    public class BindingContext
    {
        public DriverOptions Options { get; }
        public ParserOptions ParserOptions { get; set; }

        public ASTContext ASTContext { get; set; }
        public ParserTargetInfo TargetInfo { get; set; }

        public SymbolContext Symbols { get; }

        public TypeMapDatabase TypeMaps { get; set; }
        public DeclMapDatabase DeclMaps { get; set; }

        public PassBuilder<TranslationUnitPass> TranslationUnitPasses { get; }
        public PassBuilder<GeneratorOutputPass> GeneratorOutputPasses { get; }

        public Proto Proto = new Proto();

        public BindingContext(DriverOptions options, ParserOptions parserOptions = null)
        {
            Options = options;
            ParserOptions = parserOptions;

            Symbols = new SymbolContext();

            TranslationUnitPasses = new PassBuilder<TranslationUnitPass>(this);
            GeneratorOutputPasses = new PassBuilder<GeneratorOutputPass>(this);
        }

        public void RunPasses()
        {
            TranslationUnitPasses.RunPasses(pass =>
                {
                    Diagnostics.Debug("Pass '{0}'", pass);

                    Diagnostics.PushIndent();
                    pass.Context = this;
                    pass.VisitASTContext(ASTContext);
                    Diagnostics.PopIndent();
                });
        }
    }
}