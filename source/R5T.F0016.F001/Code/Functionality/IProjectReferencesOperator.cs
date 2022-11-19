using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using R5T.F0000;
using R5T.T0132;


namespace R5T.F0016.F001
{
	[FunctionalityMarker]
	public partial interface IProjectReferencesOperator : IFunctionalityMarker,
        F0016.IProjectReferencesOperator
	{
        /// <summary>
        /// Chooses <see cref="GetAllRecursiveProjectReferences_Exclusive(IEnumerable{string})"/> as the default.
        /// <para><inheritdoc cref="GetAllRecursiveProjectReferences_Exclusive(IEnumerable{string})" path="/summary"/></para>
        /// </summary>
        public Task<string[]> GetAllRecursiveProjectReferences(IEnumerable<string> projectFilePaths)
        {
            return this.GetAllRecursiveProjectReferences_Exclusive(projectFilePaths);
        }

        /// <summary>
        /// For a set of project file paths, get the set of all recursive project references for those projects, but do not include the input project file paths (unless some are recursive project references of others, in which case they will be included).
        /// </summary>
        public Task<string[]> GetAllRecursiveProjectReferences_Exclusive(IEnumerable<string> projectFilePaths)
        {
            return this.GetAllRecursiveProjectReferences(
                projectFilePaths,
                Instances.ProjectFileOperator.GetDirectProjectReferenceFilePaths);
        }

        /// <summary>
        /// For a set of project file paths, get the set of all recursive project references for those projects.
        /// </summary>
        public async Task<string[]> GetAllRecursiveProjectReferences_Inclusive(IEnumerable<string> projectFilePaths)
        {
            var exclusiveProjectReferences = await this.GetAllRecursiveProjectReferences_Exclusive(projectFilePaths);

            var output = exclusiveProjectReferences
                .Append(projectFilePaths)
                .Distinct()
                .OrderAlphabetically()
                .Now();

            return output;
        }

        public Task<string[]> GetAllRecursiveProjectReferences_Inclusive(string projectFilePath)
        {
            var projectFilePaths = EnumerableOperator.Instance.From(projectFilePath);

            return this.GetAllRecursiveProjectReferences_Inclusive(
                projectFilePaths);
        }

        public Task<Dictionary<string, string[]>> GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
            IEnumerable<string> projectFilePaths)
        {
            return this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                Instances.ProjectFileOperator.GetDirectProjectReferenceFilePaths);
        }

        public Task<Dictionary<string, string[]>> GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
            string projectFilePath)
        {
            return this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                EnumerableOperator.Instance.From(projectFilePath));
        }

        /// <summary>
        /// For all recursive references of the specified project, get the extraneous project references of each project.
        /// Include only projects which have any extraneous project references.
        /// </summary>
        public async Task<Dictionary<string, string[]>> GetExtraneousProjectReferencesOnlyForRecursiveReferencesWithExtranousReferences(
            string projectFilePath)
        {
            var extraneousProjectReferencesByProject = await this.GetExtraneousProjectReferencesForAllRecursiveReferencesByProject(
                projectFilePath);

            var extraneousProjectReferencesByProjectOnlyForProjectsWithExtraneousReferences = extraneousProjectReferencesByProject
                .Where(x => x.Value.Any())
                .ToDictionary();

            return extraneousProjectReferencesByProjectOnlyForProjectsWithExtraneousReferences;
        }

        /// <summary>
        /// For all recursive references of the specified project, get the extraneous project references of each project.
        /// Include all projects, even those without any extraneous project references.
        /// </summary>
        public async Task<Dictionary<string, string[]>> GetExtraneousProjectReferencesForAllRecursiveReferencesByProject(
            string projectFilePath)
        {
            var directDependenciesForAllRecursiveProjects = await this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePath);

            var recursiveDependenciesForAllRecursiveDependencies_Exclusive = this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                directDependenciesForAllRecursiveProjects);

            var projectFilePaths = directDependenciesForAllRecursiveProjects.Keys;

            var output = new Dictionary<string, string[]>();

            foreach (var currentProjectFilePath in projectFilePaths)
            {
                var directDependencies = directDependenciesForAllRecursiveProjects[currentProjectFilePath];

                var allRecursiveDependenciesOfDirectDependencies = new HashSet<string>();

                foreach (var directDependency in directDependencies)
                {
                    var recursiveDependenciesOfDependency = recursiveDependenciesForAllRecursiveDependencies_Exclusive[directDependency];

                    allRecursiveDependenciesOfDirectDependencies.AddRange(recursiveDependenciesOfDependency);
                }

                var extraneousDirectDependencies = directDependencies
                    .Intersect(allRecursiveDependenciesOfDirectDependencies)
                    .Now();

                output.Add(currentProjectFilePath, extraneousDirectDependencies);
            }

            return output;
        }

        public async Task<Dictionary<string, string[]>> GetRecursiveProjectReferencesForAllRecursiveProjectReferences(
            IEnumerable<string> projectFilePaths)
        {
            var output = await this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences(
                projectFilePaths,
                Instances.ProjectFileOperator.GetDirectProjectReferenceFilePaths);

            return output;
        }

        public async Task<Dictionary<string, string[]>> GetRecursiveProjectReferencesForAllRecursiveProjectReferences(
            string projectFilePath)
        {
            var output = await this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences(
                EnumerableOperator.Instance.From(projectFilePath));

            return output;
        }

        public async Task<bool> HasAnyRecursiveCOMReferences_Inclusive(string projectFilePath)
        {
            var allRecursiveProjectReferences = await this.GetAllRecursiveProjectReferences(
                EnumerableOperator.Instance.From(projectFilePath));

            foreach (var referenceProjectFilePath in allRecursiveProjectReferences)
            {
                var hasAnyCOMReference = Instances.ProjectFileOperator.HasAnyCOMReferences(referenceProjectFilePath);
                if (hasAnyCOMReference)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Chooses <see cref="HasAnyRecursiveCOMReferences_Inclusive(string)"/> as the default.
        /// </summary>
        public async Task<bool> HasAnyRecursiveCOMReferences(string projectFilePath)
        {
            var output = await this.HasAnyRecursiveCOMReferences_Inclusive(projectFilePath);
            return output;
        }

        /// <summary>
        /// For all recursive project dependencies of the project (inclusive including the project itself), identifies which project references of the dependencies are extraneous and removes them.
        /// </summary>
        /// <returns>The extraneous project references by project that were removed.</returns>
        public async Task<Dictionary<string, string[]>> RemoveExtraneousProjectReferencesFromAllRecursiveReferences(
            string projectFilePath)
        {
            var onlyExtraneousDependenciesForAllRecursiveProjects = await this.GetExtraneousProjectReferencesOnlyForRecursiveReferencesWithExtranousReferences(
                projectFilePath);

            foreach (var pair in onlyExtraneousDependenciesForAllRecursiveProjects)
            {
                var projectToModifyFilePath = pair.Key;
                var projectReferencesToRemove = pair.Value;

                Instances.ProjectFileOperator.RemoveProjectReferences_Synchronous(
                    projectToModifyFilePath,
                    projectReferencesToRemove);
            }

            return onlyExtraneousDependenciesForAllRecursiveProjects;
        }
    }
}