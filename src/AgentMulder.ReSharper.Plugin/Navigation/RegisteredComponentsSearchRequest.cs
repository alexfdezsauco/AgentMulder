using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AgentMulder.ReSharper.Domain.Registrations;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurences;
using JetBrains.ReSharper.Feature.Services.Search;
using JetBrains.ReSharper.Feature.Services.Search.SearchRequests;
using JetBrains.ReSharper.Features.Finding.FindDependentCode;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace AgentMulder.ReSharper.Plugin.Navigation
{
    public class RegisteredComponentsSearchRequest : SearchRequest
    {
        private readonly ISolution solution;
        private readonly IShellLocks locks;
        private readonly IComponentRegistration componentRegistration;

        public RegisteredComponentsSearchRequest([NotNull] ISolution solution, [NotNull] IShellLocks locks, IComponentRegistration componentRegistration)
        {
            this.solution = solution;
            this.locks = locks;
            this.componentRegistration = componentRegistration;
        }

        public override ICollection<IOccurence> Search(IProgressIndicator progressIndicator)
        {
            var typeElements = new List<ITypeDeclaration>();

            var multiCoreFibersPool = new MultiCoreFibersPool("Search type declarations", locks);
            using (IMultiCoreFibers multiCoreFibers = multiCoreFibersPool.Create())
            {
                foreach (var project in solution.GetAllProjects())
                {
                    var sourceFiles = project.GetAllProjectFiles().SelectMany(projectFile => projectFile.ToSourceFiles());
                    foreach (var psiSourceFile in sourceFiles)
                    {
                        IFile file = psiSourceFile.GetPsiFile(CSharpLanguage.Instance);
                        if (file == null)
                        {
                            continue;
                        }

                        multiCoreFibers.EnqueueJob(() => file.ProcessChildren<ITypeDeclaration>(typeElements.Add));
                    }
                }
            }

            return (from typeDeclaration in typeElements
                    let element = typeDeclaration.DeclaredElement
                    where element != null
                    where componentRegistration.IsSatisfiedBy(element)
                    select new DeclaredElementOccurence(element)).Cast<IOccurence>().ToList();
        }

        public override string Title
        {
            get { return "Components registered via this registration"; }
        }

        public override ISolution Solution
        {
            get { return solution; }
        }

        public override ICollection SearchTargets
        {
            get { return new ArrayList(); }
        }
    }
}