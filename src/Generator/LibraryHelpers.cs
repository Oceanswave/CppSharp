﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Cxxi.Generators;

namespace Cxxi
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LibraryTransformAttribute : Attribute
    {
    }

    /// <summary>
    /// Used to massage the library types into something more .NET friendly.
    /// </summary>
    public interface ILibrary
    {
        /// <summary>
        /// Do transformations that should happen before processing here.
        /// </summary>
        void Preprocess(LibraryHelpers g);

        /// <summary>
        /// Do transformations that should happen after processing here.
        /// </summary>
        void Postprocess(LibraryHelpers g);

        /// <summary>
        /// Setup your passes here.
        /// </summary>
        /// <param name="passes"></param>
        void SetupPasses(PassBuilder passes);

        /// <summary>
        /// Called to generate text at the start of the text template.
        /// </summary>
        /// <param name="template"></param>
        void GenerateStart(TextTemplate template);

        /// <summary>
        /// Called to generate text after the generation of namespaces.
        /// </summary>
        /// <param name="template"></param>
        void GenerateAfterNamespaces(TextTemplate template);
    }

    public enum InlineMethods
    {
        Present,
        Unavailable
    }

    public class LibraryHelpers
    {
        private Library Library { get; set; }
     
        public LibraryHelpers(Library library)
        {
            Library = library;
        }

        #region Enum Helpers

        public Enumeration FindEnum(string name)
        {
            foreach (var unit in Library.TranslationUnits)
            {
                var @enum = unit.FindEnum(name);
                if (@enum != null)
                    return @enum;
            }

            return null;
        }

        public void IgnoreEnumWithMatchingItem(string pattern)
        {
            Enumeration @enum = GetEnumWithMatchingItem(pattern);
            if (@enum != null)
                @enum.ExplicityIgnored = true;
        }

        public void SetNameOfEnumWithMatchingItem(string pattern, string name)
        {
            Enumeration @enum = GetEnumWithMatchingItem(pattern);
            if (@enum != null)
                @enum.Name = name;
        }

        public void SetNameOfEnumWithName(string enumName, string name)
        {
            Enumeration @enum = FindEnum(enumName);
            if (@enum != null)
                @enum.Name = name;
        }

        public Enumeration GetEnumWithMatchingItem(string pattern)
        {
            foreach (var module in Library.TranslationUnits)
            {
                Enumeration @enum = module.FindEnumWithItem(pattern);
                if (@enum == null) continue;
                return @enum;
            }

            return null;
        }

        public Enumeration.Item GenerateEnumItemFromMacro(MacroDefinition macro)
        {
            var item = new Enumeration.Item
            {
                Name = macro.Name,
                Expression = macro.Expression,
                Value = ParseMacroExpression(macro.Expression)
            };

            return item;
        }

        static bool ParseToNumber(string num, out long val)
        {
            if (num.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                num = num.Substring(2);

                return long.TryParse(num, NumberStyles.HexNumber,
                    CultureInfo.CurrentCulture, out val);
            }

            return long.TryParse(num, out val);
        }

        static long ParseMacroExpression(string expression)
        {
            long val;
            if (ParseToNumber(expression, out val))
                return val;
            // TODO: Handle string expressions
            return 0;
        }

        public Enumeration GenerateEnumFromMacros(string name, params string[] macros)
        {
            var @enum = new Enumeration { Name = name };

            var pattern = string.Join("|", macros);
            var regex = new Regex(pattern);

            foreach (var unit in Library.TranslationUnits)
            {
                foreach (var macro in unit.Macros)
                {
                    var match = regex.Match(macro.Name);
                    if (!match.Success) continue;

                    var item = GenerateEnumItemFromMacro(macro);
                    @enum.AddItem(item);
                }

                if (@enum.Items.Count > 0)
                {
                    unit.Enums.Add(@enum);
                    break;
                }
            }

            return @enum;
        }

        #endregion

        #region Class Helpers

        public Class FindClass(string name)
        {
            foreach (var module in Library.TranslationUnits)
            {
                var @class = module.FindClass(name);
                if (@class != null)
                    return @class;
            }

            return null;
        }

        public void SetClassBindName(string className, string name)
        {
            Class @class = FindClass(className);
            if (@class != null)
                @class.Name = name;
        }

        public void SetClassAsValueType(string className)
        {
            Class @class = FindClass(className);
            if (@class != null)
                @class.Type = ClassType.ValueType;
        }

        public void IgnoreClassWithName(string name)
        {
            Class @class = FindClass(name);
            if (@class != null)
                @class.ExplicityIgnored = true;
        }

        #endregion

        #region Function Helpers

        public Function FindFunction(string name)
        {
            foreach (var module in Library.TranslationUnits)
            {
                var function = module.FindFunction(name);
                if (function != null)
                    return function;
            }

            return null;
        }

        public void IgnoreFunctionWithName(string name)
        {
            Function function = FindFunction(name);
            if (function != null)
                function.ExplicityIgnored = true;
        }

        public void IgnoreClassMethodWithName(string className, string name)
        {
            Class @class = FindClass(className);

            if (@class == null)
                return;

            var method = @class.Methods.Find(m => m.Name == name);

            if (method == null)
                return;

            method.ExplicityIgnored = true;
        }

        #endregion

        #region Module Helpers

        public void IgnoreModulessWithName(string pattern)
        {
            var modules = Library.TranslationUnits.FindAll(m =>
                Regex.Match(m.FilePath, pattern).Success);

            foreach (var module in modules)
            {
                module.ExplicityIgnored = true;
            }
        }

        #endregion
    }
}