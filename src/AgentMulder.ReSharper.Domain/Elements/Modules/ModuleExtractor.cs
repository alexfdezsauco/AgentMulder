using System.Collections.Generic;
using System.Linq;
using AgentMulder.ReSharper.Domain.Elements.Modules.Impl;
using JetBrains.ProjectModel;

namespace AgentMulder.ReSharper.Domain.Elements.Modules
{
    public static class ModuleExtractor
    {
        private static readonly List<IModuleExtractor> extractors = new List<IModuleExtractor>();

        static ModuleExtractor()
        {
            var typeElementExtractor = new TypeElementExtractor();
            var typeOfExtractor = new TypeOfExtractor(typeElementExtractor);
            var typeAssemblyPropertyExtractor = new TypeAssemblyPropertyExtractor(typeOfExtractor);
            
            extractors.Add(typeElementExtractor);
            extractors.Add(typeOfExtractor);
            extractors.Add(typeAssemblyPropertyExtractor);
            extractors.Add(new AssemblyNameExtractor());
            extractors.Add(new GetExecutingAssemblyExtractor());
        }

        public static IModule GetTargetModule<T>(T element)
        {
            return extractors.Select(extractor => extractor.GetTargetModule(element)).FirstOrDefault(result => result != null);
        }
    }
}