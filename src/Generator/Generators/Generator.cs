﻿using System;
using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Generators
{
    /// <summary>
    /// Kinds of language generators.
    /// </summary>
    public enum LanguageGeneratorKind
    {
        CPlusPlusCLI,
        CSharp,
    }

    /// <summary>
    /// Output generated by each backend generator.
    /// </summary>
    public struct GeneratorOutput
    {
        /// <summary>
        /// Translation unit associated with output.
        /// </summary>
        public TranslationUnit TranslationUnit;

        /// <summary>
        /// Text templates with generated output.
        /// </summary>
        public List<Template> Templates;
    }

    /// <summary>
    /// Generators are the base class for each language backend.
    /// </summary>
    public abstract class Generator
    {
        public Driver Driver { get; private set; }

        protected Generator(Driver driver)
        {
            Driver = driver;
        }

        /// <summary>
        /// Called when a translation unit is generated.
        /// </summary>
        public Action<GeneratorOutput> OnUnitGenerated = delegate { };

        /// <summary>
        /// Setup any generator-specific passes here.
        /// </summary>
        public abstract bool SetupPasses();

        /// <summary>
        /// Setup any generator-specific processing here.
        /// </summary>
        public virtual void Process()
        {

        }

        /// <summary>
        /// Generates the outputs.
        /// </summary>
        public virtual List<GeneratorOutput> Generate()
        {
            var outputs = new List<GeneratorOutput>();

            foreach (var unit in Driver.Library.TranslationUnits)
            {
                if (unit.Ignore || !unit.HasDeclarations)
                    continue;

                if (unit.IsSystemHeader)
                    continue;

                var templates = Generate(unit);
                if (templates.Count == 0)
                    continue;

                foreach (var template in templates)
                    template.Process();

                var output = new GeneratorOutput
                    {
                        TranslationUnit = unit,
                        Templates = templates
                    };
                outputs.Add(output);

                OnUnitGenerated(output);
            }

            return outputs;
        }

        /// <summary>
        /// Generates the outputs for a given translation unit.
        /// </summary>
        public abstract List<Template> Generate(TranslationUnit unit);
    }
}