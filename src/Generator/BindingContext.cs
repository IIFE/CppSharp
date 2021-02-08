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

        public Dictionary<string, List<ProtoMessage>> Rpcs { get; set; } = new Dictionary<string, List<ProtoMessage>>();

        public Dictionary<string, List<ProtoEnum>> Enums { get; set; } = new Dictionary<string, List<ProtoEnum>>();

        public Proto()
        {
            ProtoMessage fileTime = new ProtoMessage();
            fileTime.MessageName = "_FILETIME";
            fileTime.Fields.Add(new ProtoField { FieldName = "dwLowDateTime", FieldType = "uint64" });
            fileTime.Fields.Add(new ProtoField { FieldName = "dwHighDateTime", FieldType = "uint64" });
            AddMessage("data.service.models.protobuf", fileTime);

            AddMessage("data.service.models.protobuf", new ProtoMessage { MessageName = "Empty" });
        }

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

        public void GenerateRpcModels(string @namespace, ProtoMessage rpc)
        {
            string finalNamespace = @namespace.Replace(".protobuf", ".models.protobuf");
            List<ProtoMessage> messages;
            bool hasValue = Messages.TryGetValue(finalNamespace, out messages);

            bool alreadyExists = hasValue && messages.Any(x => x.MessageName == $"{rpc.MessageName}Response");

            if (!alreadyExists)
            {
                ProtoMessage rpcResponse = new ProtoMessage();
                rpcResponse.MessageName = $"{rpc.MessageName}Response";
                rpcResponse.Fields.Add(new ProtoField { FieldName = "result", FieldType = "request.lib.models.protobuf.REQUEST_RESULT" });
                AddMessage(finalNamespace, rpcResponse);
            }

            if (rpc.Fields.Any())
            {
                alreadyExists = hasValue && messages.Any(x => x.MessageName == $"{rpc.MessageName}Request");

                if (!alreadyExists)
                {
                    ProtoMessage rpcRequest = new ProtoMessage();
                    rpcRequest.MessageName = $"{rpc.MessageName}Request";

                    foreach (ProtoField field in rpc.Fields)
                    {
                        rpcRequest.Fields.Add(new ProtoField { FieldName = field.FieldName, FieldType = field.FieldType });
                    }

                    AddMessage(finalNamespace, rpcRequest);
                }
            }
        }

        public void AddRpc(string @namespace, ProtoMessage rpc)
        {
            IEnumerable<ProtoMessage> allRpcs = Rpcs.Values.SelectMany(x => x) ?? new List<ProtoMessage>();

            bool alreadyExists = allRpcs.Any(x => x.MessageName == rpc.MessageName);

            if (!alreadyExists)
            {
                GenerateRpcModels(@namespace, rpc);

                if (Rpcs.TryGetValue(@namespace, out List<ProtoMessage> rpcs))
                {
                    rpcs.Add(rpc);
                }
                else
                {
                    Rpcs.Add(@namespace, new List<ProtoMessage> { rpc });
                }
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