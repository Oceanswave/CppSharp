﻿using System;
using System.IO;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    #region CLI Type Visitors
    public class CLITypePrinter : ITypeVisitor<string>, IDeclVisitor<string>
    {
        public Generator Generator { get; set; }
        public Library Library { get; set; }

        public CLITypePrinter(Generator generator)
        {
            Generator = generator;
            Library = generator.Library;
        }

        public string VisitTagType(TagType tag, TypeQualifiers quals)
        {
            Declaration decl = tag.Declaration;

            if (decl == null)
                return string.Empty;

            return VisitDeclaration(decl, quals);
        }

        public string VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            return string.Format("array<{0}>", array.Type.Visit(this));
        }

        public string VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var arguments = function.Arguments;
            var returnType = function.ReturnType;

            string args = string.Empty;

            if (arguments.Count > 0)
                args = GetArgumentsString(function, hasNames: false);

            if (returnType.IsPrimitiveType(PrimitiveType.Void))
            {
                if (!string.IsNullOrEmpty(args))
                    args = string.Format("<{0}>", args);
                return string.Format("System::Action{0}", args);
            }

            if (!string.IsNullOrEmpty(args))
                args = string.Format(", {0}", args);

            return string.Format("System::Func<{0}{1}>", returnType.Visit(this), args);
        }

        public string GetArgumentsString(FunctionType function, bool hasNames)
        {
            var arguments = function.Arguments;
            var s = string.Empty;

            for (var i = 0; i < arguments.Count; ++i)
            {
                s += GetArgumentString(arguments[i], hasNames);
                if (i < arguments.Count - 1)
                    s += ", ";
            }

            return s;
        }

        public string GetArgumentString(Parameter arg, bool hasName = true)
        {
            var quals = new TypeQualifiers {IsConst = arg.IsConst};

            var type = arg.Type.Visit(this, quals);
            var name = arg.Name;

            if (hasName && !string.IsNullOrEmpty(name))
                return string.Format("{0} {1}", type, name);
   
            return type;
        }

        public string ToDelegateString(FunctionType function)
        {
            return string.Format("delegate {0} {{0}}({1})",
                function.ReturnType.Visit(this),
                GetArgumentsString(function, hasNames: true));
        }

        public string VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;
            
            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return string.Format("{0}^", function.Visit(this, quals));
            }

            if (pointee.IsPrimitiveType(PrimitiveType.Void))
                return "System::IntPtr";

            if (pointee.IsPrimitiveType(PrimitiveType.Char))
                return "System::String^";

            return pointee.Visit(this, quals);
        }

        public string VisitMemberPointerType(MemberPointerType member,
                                             TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public string VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public string VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool: return "bool";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.WideChar: return "char";
                case PrimitiveType.Int8: return "char";
                case PrimitiveType.UInt8: return "unsigned char";
                case PrimitiveType.Int16: return "short";
                case PrimitiveType.UInt16: return "unsigned short";
                case PrimitiveType.Int32: return "int";
                case PrimitiveType.UInt32: return "unsigned int";
                case PrimitiveType.Int64: return "long";
                case PrimitiveType.UInt64: return "unsigned long";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
            }

            return string.Empty;
        }

        public string VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            if (string.IsNullOrEmpty(decl.Name))
                return null;

            TypeMap typeMap = null;
            if (Generator.TypeDatabase.FindTypeMap(decl.QualifiedOriginalName, out typeMap))
            {
                return typeMap.Signature();
            }

            FunctionType func;
            if (typedef.Declaration.Type.IsPointerTo<FunctionType>(out func))
            {
                // TODO: Use SafeIdentifier()
                return string.Format("{0}^", typedef.Declaration.Name);
            }

            return decl.Type.Visit(this);
        }

        public string VisitTemplateSpecializationType(TemplateSpecializationType template,
                                                      TypeQualifiers quals)
        {
            var decl = template.Template.TemplatedDecl;

            TypeMap typeMap = null;
            if (Generator.TypeDatabase.FindTypeMap(decl.QualifiedOriginalName, out typeMap))
            {
                typeMap.Declaration = decl;
                typeMap.Type = template;
                return typeMap.Signature();
            }

            return decl.Name;
        }

        public string VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public string VisitDeclaration(Declaration decl)
        {
            var name = decl.Visit(this);
            return string.Format("{0}::{1}", Library.Name, name);
        }

        public string VisitClassDecl(Class @class)
        {
            return string.Format("{0}{1}", @class.Name, @class.IsRefType ? "^"
                : string.Empty);
        }

        public string VisitFieldDecl(Field field)
        {
            throw new NotImplementedException();
        }

        public string VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public string VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public string VisitParameterDecl(Parameter parameter)
        {
            throw new NotImplementedException();
        }

        public string VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public string VisitEnumDecl(Enumeration @enum)
        {
            return @enum.Name;
        }

        public string VisitClassTemplateDecl(ClassTemplate template)
        {
            throw new NotImplementedException();
        }

        public string VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public string VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }
    }

    public class CLIForwardRefeferencePrinter : IDeclVisitor<string>
    {
        public string VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public string VisitClassDecl(Class @class)
        {
            if (@class.IsValueType)
                return string.Format("value struct {0};", @class.Name);
            return string.Format("ref class {0};", @class.Name);
        }

        public string VisitFieldDecl(Field field)
        {
            throw new NotImplementedException();
        }

        public string VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public string VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public string VisitParameterDecl(Parameter parameter)
        {
            throw new NotImplementedException();
        }

        public string VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public string VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.Type.IsPrimitiveType(PrimitiveType.Int32))
                return string.Format("enum struct {0};", @enum.Name);
            return string.Format("enum struct {0} : {1};", @enum.Name,
                @enum.Type);
        }

        public string VisitClassTemplateDecl(ClassTemplate template)
        {
            throw new NotImplementedException();
        }

        public string VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public string VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region CLI Text Templates
    public abstract class CLITextTemplate : TextTemplate
    {
        protected const string DefaultIndent = "    ";
        protected const uint MaxIndent = 80;

        public CLITypePrinter TypeSig { get; set; }

        public static string SafeIdentifier(string proposedName)
        {
            return proposedName;
        }

        public string QualifiedIdentifier(Declaration decl)
        {
            return string.Format("{0}::{1}", Library.Name, decl.Name);
        }

        public void GenerateStart()
        {
            Transform.GenerateStart(this);
        }

        public void GenerateAfterNamespaces()
        {
            Transform.GenerateAfterNamespaces(this);
        }

        public void GenerateSummary(string comment)
        {
            if (String.IsNullOrWhiteSpace(comment))
                return;

            // Wrap the comment to the line width.
            var maxSize = (int)(MaxIndent - CurrentIndent.Count - "/// ".Length);
            var lines = StringHelpers.WordWrapLines(comment, maxSize);

            WriteLine("/// <summary>");
            foreach (string line in lines)
                WriteLine(string.Format("/// {0}", line.TrimEnd()));
            WriteLine("/// </summary>");
        }

        public void GenerateInlineSummary(string comment)
        {
            if (String.IsNullOrWhiteSpace(comment))
                return;
            WriteLine("/// <summary> {0} </summary>", comment);
        }

        public static bool CheckIgnoreMethod(Class @class, Method method)
        {
            if (method.Ignore) return true;

            if (@class.IsAbstract && method.IsConstructor)
                return true;

            if (@class.IsValueType && method.IsDefaultConstructor)
                return true;

            if (method.IsCopyConstructor || method.IsMoveConstructor)
                return true;

            if (method.IsDestructor)
                return true;

            if (method.OperatorKind == CXXOperatorKind.Equal)
                return true;

            if (method.Kind == CXXMethodKind.Conversion)
                return true;

            if (method.Access != AccessSpecifier.Public)
                return true;

            return false;
        }

        public abstract override string FileExtension { get; }

        protected abstract override void Generate();
    }
    #endregion

    public class CLIGenerator : ILanguageGenerator
    {
        public Options Options { get; set; }
        public Library Library { get; set; }
        public ILibrary Transform { get; set; }
        public Generator Generator { get; set; }

        private readonly CLITypePrinter typePrinter;

        public CLIGenerator(Generator generator)
        {
            Generator = generator;
            typePrinter = new CLITypePrinter(generator);
            Type.TypePrinter = typePrinter;
        }

        T CreateTemplate<T>(TranslationUnit unit) where T : CLITextTemplate, new()
        {
            var template = new T
            {
                Generator = Generator,
                Options = Options,
                Library = Library,
                Transform = Transform,
                Module = unit,
                TypeSig = typePrinter
            };

            return template;
        }

        void WriteTemplate(TextTemplate template)
        {
            var file = Path.GetFileNameWithoutExtension(template.Module.FileName) + "."
                + template.FileExtension;

            var path = Path.Combine(Options.OutputDir, file);

            Console.WriteLine("  Generated '" + file + "'.");
            File.WriteAllText(Path.GetFullPath(path), template.ToString());
        }

        public bool Generate(TranslationUnit unit)
        {
            typePrinter.Library = Library;

            var header = CreateTemplate<CLIHeadersTemplate>(unit);
            WriteTemplate(header);

            var source = CreateTemplate<CLISourcesTemplate>(unit);
            WriteTemplate(source);

            return true;
        }
    }
}