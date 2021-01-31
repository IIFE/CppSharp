using CppSharp.Generators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CppSharp
{
    public enum NewLineKind
    {
        Never,
        Always,
        BeforeNextBlock,
        IfNotEmpty
    }

    public enum BlockKind
    {
        Unknown,
        Block,
        BlockComment,
        InlineComment,
        Header,
        Footer,
        Usings,
        Namespace,
        TranslationUnit,
        Enum,
        EnumItem,
        Typedef,
        Class,
        InternalsClass,
        InternalsClassMethod,
        InternalsClassField,
        Functions,
        Function,
        Method,
        Event,
        Variable,
        Property,
        Unreachable,
        Field,
        VTableDelegate,
        Region,
        Interface,
        Finalizer,
        Includes,
        IncludesForwardReferences,
        ForwardReferences,
        MethodBody,
        FunctionsClass,
        Template,
        Destructor,
        AccessSpecifier,
        Fields,
        Constructor,
        ConstructorBody,
        DestructorBody,
        FinalizerBody
    }

    [DebuggerDisplay("{Kind} | {Object}")]
    public class Block : ITextGenerator
    {
        public TextGenerator Text { get; set; }
        public BlockKind Kind { get; set; }
        public NewLineKind NewLineKind { get; set; }

        public object Object { get; set; }

        public Block Parent { get; set; }
        public List<Block> Blocks { get; set; }

        private bool hasIndentChanged;

        public Func<bool> CheckGenerate;

        public Block() : this(BlockKind.Unknown)
        {

        }

        public Block(BlockKind kind)
        {
            Kind = kind;
            Blocks = new List<Block>();
            Text = new TextGenerator();
            hasIndentChanged = false;
        }

        public void AddBlock(Block block)
        {
            if (Text.StringBuilder.Length != 0 || hasIndentChanged)
            {
                hasIndentChanged = false;
                var newBlock = new Block { Text = Text.Clone() };
                Text.StringBuilder.Clear();

                AddBlock(newBlock);
            }

            block.Parent = this;
            Blocks.Add(block);
        }

        public IEnumerable<Block> FindBlocks(BlockKind kind)
        {
            foreach (var block in Blocks)
            {
                if (block.Kind == kind)
                    yield return block;

                foreach (var childBlock in block.FindBlocks(kind))
                    yield return childBlock;
            }
        }

        public virtual StringBuilder Generate()
        {
            if (CheckGenerate != null && !CheckGenerate())
                return new StringBuilder();

            if (Blocks.Count == 0)
                return Text.StringBuilder;

            var builder = new StringBuilder();
            Block previousBlock = null;

            var blockIndex = 0;
            foreach (var childBlock in Blocks)
            {
                var childText = childBlock.Generate();

                var nextBlock = (++blockIndex < Blocks.Count)
                    ? Blocks[blockIndex]
                    : null;

                var skipBlock = false;
                if (nextBlock != null)
                {
                    var nextText = nextBlock.Generate();
                    if (nextText.Length == 0 &&
                        childBlock.NewLineKind == NewLineKind.IfNotEmpty)
                        skipBlock = true;
                }

                if (skipBlock)
                    continue;

                if (childText.Length == 0)
                    continue;

                if (previousBlock != null &&
                    previousBlock.NewLineKind == NewLineKind.BeforeNextBlock)
                    builder.AppendLine();

                builder.Append(childText);

                if (childBlock.NewLineKind == NewLineKind.Always)
                    builder.AppendLine();

                previousBlock = childBlock;
            }

            if (Text.StringBuilder.Length != 0)
                builder.Append(Text.StringBuilder);

            return builder;
        }

        public bool IsEmpty
        {
            get
            {
                if (Blocks.Any(block => !block.IsEmpty))
                    return false;

                return string.IsNullOrEmpty(Text.ToString());
            }
        }

        #region ITextGenerator implementation

        public void Write(string msg, params object[] args)
        {
            Text.Write(msg, args);
        }

        public void WriteLine(string msg, params object[] args)
        {
            Text.WriteLine(msg, args);
        }

        public void WriteLineIndent(string msg, params object[] args)
        {
            Text.WriteLineIndent(msg, args);
        }

        public void NewLine()
        {
            Text.NewLine();
        }

        public void NewLineIfNeeded()
        {
            Text.NewLineIfNeeded();
        }

        public void NeedNewLine()
        {
            Text.NeedNewLine();
        }

        public bool NeedsNewLine
        {
            get => Text.NeedsNewLine;
            set => Text.NeedsNewLine = value;
        }

        public void ResetNewLine()
        {
            Text.ResetNewLine();
        }

        public void Indent(uint indentation = 4u)
        {
            hasIndentChanged = true;
            Text.Indent(indentation);
        }

        public void Unindent()
        {
            hasIndentChanged = true;
            Text.Unindent();
        }

        public void WriteOpenBraceAndIndent()
        {
            Text.WriteOpenBraceAndIndent();
        }

        public void UnindentAndWriteCloseBrace()
        {
            Text.UnindentAndWriteCloseBrace();
        }

        #endregion
    }

    public abstract class BlockGenerator : ITextGenerator
    {
        private static string[] LineBreakSequences = new[] { "\r\n", "\r", "\n" };

        public Block RootBlock { get; }
        public Block ActiveBlock { get; private set; }
        public uint CurrentIndentation => ActiveBlock.Text.CurrentIndentation;

        protected BlockGenerator()
        {
            RootBlock = new Block();
            ActiveBlock = RootBlock;
        }

        public virtual string Generate()
        {
            return RootBlock.Generate().ToString();
        }

        #region Block helpers

        public void AddBlock(Block block)
        {
            ActiveBlock.AddBlock(block);
        }

        public void PushBlock(BlockKind kind = BlockKind.Unknown, object obj = null)
        {
            var block = new Block { Kind = kind, Object = obj };
            block.Text.CurrentIndentation = CurrentIndentation;
            block.Text.IsStartOfLine = ActiveBlock.Text.IsStartOfLine;
            block.Text.NeedsNewLine = ActiveBlock.Text.NeedsNewLine;
            PushBlock(block);
        }

        public void PushBlock(Block block)
        {
            block.Parent = ActiveBlock;
            ActiveBlock.AddBlock(block);
            ActiveBlock = block;
        }

        public Block PopBlock(NewLineKind newLineKind = NewLineKind.Never)
        {
            var block = ActiveBlock;

            ActiveBlock.NewLineKind = newLineKind;
            ActiveBlock = ActiveBlock.Parent;

            return block;
        }

        public IEnumerable<Block> FindBlocks(BlockKind kind)
        {
            return RootBlock.FindBlocks(kind);
        }

        public Block FindBlock(BlockKind kind)
        {
            return FindBlocks(kind).SingleOrDefault();
        }

        #endregion

        #region ITextGenerator implementation

        public void Write(string msg, params object[] args)
        {
            ActiveBlock.Write(msg, args);
        }

        public void WriteLine(string msg, params object[] args)
        {
            ActiveBlock.WriteLine(msg, args);
        }

        public void WriteLines(string msg, bool trimIndentation = false)
        {
            var lines = msg.Split(LineBreakSequences, StringSplitOptions.None);
            int indentation = int.MaxValue;

            if (trimIndentation)
            {
                foreach(var line in lines)
                { 
                    for (int i = 0; i < line.Length; ++i)
                    {
                        if (char.IsWhiteSpace(line[i]))
                            continue;
                        
                        if (i < indentation)
                        { 
                            indentation = i;
                            break;
                        }
                    }
                }
            }

            bool foundNonEmptyLine = false;
            foreach (var line in lines)
            {
                if (!foundNonEmptyLine && string.IsNullOrEmpty(line))
                    continue;

                WriteLine(line.Length >= indentation ? line.Substring(indentation) : line);
                foundNonEmptyLine = true;
            }
        }

        public void WriteLineIndent(string msg, params object[] args)
        {
            ActiveBlock.WriteLineIndent(msg, args);
        }

        public void NewLine()
        {
            ActiveBlock.NewLine();
        }

        public void NewLineIfNeeded()
        {
            ActiveBlock.NewLineIfNeeded();
        }

        public void NeedNewLine()
        {
            ActiveBlock.NeedNewLine();
        }

        public bool NeedsNewLine
        {
            get => ActiveBlock.NeedsNewLine;
            set => ActiveBlock.NeedsNewLine = value;
        }

        public void ResetNewLine()
        {
            ActiveBlock.ResetNewLine();
        }

        public void Indent(uint indentation = 4u)
        {
            ActiveBlock.Indent(indentation);
        }

        public void Unindent()
        {
            ActiveBlock.Unindent();
        }

        public void WriteOpenBraceAndIndent()
        {
            ActiveBlock.WriteOpenBraceAndIndent();
        }

        public void UnindentAndWriteCloseBrace()
        {
            ActiveBlock.UnindentAndWriteCloseBrace();
        }

        #endregion
    }

    public class ProtoGenerator : BlockGenerator
    {
        BindingContext Context { get; set; }

        public ProtoGenerator(BindingContext context)
        {
            Context = context;
        }

        public override string Generate()
        {
            HashSet<string> visistedNamespaces = new HashSet<string>();
            foreach(var enums in Context.Proto.Enums)
            {
                visistedNamespaces.Add(enums.Key);

                WriteLine("syntax = \"proto3\";");
                NewLine();
                WriteLine($"option csharp_namespace = \"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(enums.Key.Replace(".protobuf", ""))}\";");
                NewLine();
                WriteLine($"package {enums.Key};");
                NewLine();

                HashSet<string> dupEnumItems = new HashSet<string>();

                foreach (var @enum in enums.Value)
                {
                    Write($"enum {@enum.Name} ");
                    WriteOpenBraceAndIndent();

                    IEnumerable<string> values = @enum.Enums.Select(x => x.Value);

                    if (values.Count() != values.Distinct().Count())
                    {
                        WriteLine("option allow_alias = true;");
                    }

                    bool needsUnknown = !values.Any(x => x == "0");
                    if (needsUnknown)
                    {
                        @enum.Enums.Add(new KeyValuePair<string, string>($"{@enum.Name}Unknown", "0"));
                    }

                    @enum.Enums = @enum.Enums.OrderByDescending(o => o.Value == "0").ThenBy(o => o.Value).ToList();

                    foreach (var kvp in @enum.Enums)
                    {
                        if (dupEnumItems.Contains(kvp.Key))
                        {
                            WriteLine($"{@enum.Name}{kvp.Key} = {kvp.Value};");
                        }
                        else
                        {
                            dupEnumItems.Add(kvp.Key);
                            WriteLine($"{kvp.Key} = {kvp.Value};");
                        }
                    }

                    UnindentAndWriteCloseBrace();
                    NewLine();
                }

                string output = base.Generate();

                string outputFileName = enums.Key.Replace(".protobuf", "").Replace('.', '_') + ".proto";
                string filePath = Path.Combine(Context.Options.OutputDir, outputFileName);
                using (var stream = File.AppendText(filePath))
                {
                    stream.Write(output);
                }
                
                RootBlock.Text.StringBuilder.Clear();
            }

            foreach (var messages in Context.Proto.Messages)
            {
                if (!visistedNamespaces.Contains(messages.Key))
                {
                    WriteLine("syntax = \"proto3\";");
                    NewLine();
                    WriteLine($"option csharp_namespace = \"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(messages.Key.Replace(".protobuf", ""))}\";");
                    NewLine();
                    WriteLine($"package {messages.Key};");
                    NewLine();
                }

                HashSet<string> dupEnumItems = new HashSet<string>();

                HashSet<string> allImports = new HashSet<string>();

                string outputFileName = messages.Key.Replace(".protobuf", "").Replace('.', '_') + ".proto";

                foreach (var msg in messages.Value)
                {
                    bool hasFieldsWithoutTypes = msg.Fields.Any(x => string.IsNullOrEmpty(x.FieldType));
                    if (msg.Fields.Count() > 0 && !hasFieldsWithoutTypes)
                    {
                        IEnumerable<string> imports = msg.Fields
                            .Where(x => x.FieldType.Contains('.'))
                            .Select(x => x.FieldType.Substring(0, x.FieldType.LastIndexOf('.')));

                        IEnumerable<string> newImports = imports.Except(allImports);

                        foreach (string newImport in newImports)
                        {
                            string newImportFinal = newImport;
                            if (newImportFinal.Contains("repeated "))
                            {
                                newImportFinal = newImport.Substring("repeated ".Length);
                            }

                            string importFileName = newImportFinal.Replace(".protobuf", "").Replace('.', '_') + ".proto";

                            if (importFileName != outputFileName)
                            {
                                WriteLine($"import \"Protos/Models/{importFileName}\";");
                            }
                        }

                        allImports = allImports.Union(newImports).ToHashSet();

                        Write($"message {msg.MessageName} ");
                        WriteOpenBraceAndIndent();

                        int i = 0;                        
                        foreach (ProtoField field in msg.Fields)
                        {
                            WriteLine($"{field.FieldType} {field.FieldName} = {++i};");
                        }

                        UnindentAndWriteCloseBrace();
                        NewLine();
                    }
                }

                string output = base.Generate();
                
                string filePath = Path.Combine(Context.Options.OutputDir, outputFileName);
                using (var stream = File.AppendText(filePath))
                {
                    stream.Write(output);
                }

                RootBlock.Text.StringBuilder.Clear();
            }

            //Dictionary<string, int> duplicates = new Dictionary<string, int>();

            //foreach(ProtoEnum @enum in Context.Proto.Enums)
            //{
            //    if (duplicates.ContainsKey(@enum.Name))
            //    {
            //        duplicates[@enum.Name] = duplicates[@enum.Name] + 1;
            //        @enum.Name = $"{@enum.Name}{duplicates[@enum.Name]}";
            //    }
            //    else
            //    {
            //        duplicates.Add(@enum.Name, 1);
            //    }

            //    Write($"enum {@enum.Name} ");
            //    WriteOpenBraceAndIndent();

            //    IEnumerable<string> values = @enum.Enums.Select(x => x.Value);

            //    if(values.Count() != values.Distinct().Count())
            //    {
            //        WriteLine("option allow_alias = true;");
            //    }

            //    bool needsUnknown = !values.Any(x => x == "0");
            //    if(needsUnknown)
            //    {
            //        @enum.Enums.Add(new KeyValuePair<string, string>("Unknown", "0"));
            //    }

            //    @enum.Enums = @enum.Enums.OrderByDescending(o => o.Value == "0").ThenBy(o => o.Value).ToList();

            //    foreach (var kvp in @enum.Enums)
            //    {
            //        if(kvp.Key.StartsWith(@enum.Name) || kvp.Key.StartsWith('_') || @enum.Name.EndsWith('_'))
            //        {
            //            WriteLine($"{kvp.Key} = {kvp.Value};");                        
            //        }
            //        else
            //        {
            //            WriteLine($"{@enum.Name}_{kvp.Key} = {kvp.Value};");
            //        }                    
            //    }

            //    UnindentAndWriteCloseBrace();
            //    NewLine();
            //}

            //foreach(ProtoMessage msg in Context.Proto.Messages)
            //{
            //    Write($"message {msg.MessageName} ");
            //    WriteOpenBraceAndIndent();

            //    int i = 0;
            //    foreach(ProtoField field in msg.Fields)
            //    {
            //        WriteLine($"{field.FieldType} {field.FieldName} = {++i};");
            //    }

            //    UnindentAndWriteCloseBrace();
            //    NewLine();
            //}

            return "";
        }
    }
}
