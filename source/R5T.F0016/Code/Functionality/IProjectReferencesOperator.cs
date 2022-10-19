using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using R5T.Magyar;

using R5T.T0132;


namespace R5T.F0016
{
    [FunctionalityMarker]
    public interface IProjectReferencesOperator : IFunctionalityMarker
    {
        /// <summary>
        /// For a set of project file paths, get the set of all recursive project references for those projects.
        /// </summary>
        public async Task<string[]> GetAllRecursiveProjectReferences(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var recursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive = await this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive(
                projectFilePaths,
                getDirectProjectReferenceDependencies);

            var allRecursiveProjectReferences = recursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive
                .SelectMany(pair => pair.Value)
                .Distinct()
                .OrderAlphabetically()
                .ToArray();

            return allRecursiveProjectReferences;
        }

        /// <summary>
        /// Get the chains of dependencies leading from the <paramref name="dependencyProjectFilePath"/> to the <paramref name="rootProjectFilePath"/>.
        /// Inclusive in the sense that the descendant project is the first project in the chain.
        /// </summary>
        public async Task<string[][]> GetDependencyChains_Inclusive(
            string rootProjectFilePath,
            string dependencyProjectFilePath,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var directProjectReferencesByProject_Exclusive = await this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                EnumerableHelper.From(rootProjectFilePath),
                getDirectProjectReferenceDependencies);

            return this.GetDependencyChains_Inclusive(
                rootProjectFilePath,
                dependencyProjectFilePath,
                directProjectReferencesByProject_Exclusive);
        }

        /// <inheritdoc cref="GetDependencyChains_Inclusive(string, string, GetDirectProjectReferenceDependencies)"/>
        public string[][] GetDependencyChains_Inclusive(
            string rootProjectFilePath,
            string dependencyProjectFilePath,
            Dictionary<string, string[]> directProjectReferencesByProject_Exclusive)
        {
            if (!directProjectReferencesByProject_Exclusive.ContainsKey(rootProjectFilePath))
            {
                throw new InvalidOperationException($"No depdencies found for project:\n{rootProjectFilePath}");
            }

            if (!directProjectReferencesByProject_Exclusive.ContainsKey(dependencyProjectFilePath))
            {
                throw new InvalidOperationException($"No depdencies found for project:\n{dependencyProjectFilePath}");
            }

            // Invert project references by project to get projects referencing projecy by project.
            var projectsReferencingProjectByProject = this.GetProjectsReferencingProjectByProject(directProjectReferencesByProject_Exclusive);

            var dependencyChainsByDependencyProject = new Dictionary<string, string[][]>();

            var output = this.GetDependencyChains_Inclusive(
                rootProjectFilePath,
                dependencyProjectFilePath,
                projectsReferencingProjectByProject,
                dependencyChainsByDependencyProject);

            return output;
        }

        /// <inheritdoc cref="GetDependencyChains_Inclusive(string, string, GetDirectProjectReferenceDependencies)"/>
        private string[][] GetDependencyChains_Inclusive(
            string rootProjectFilePath,
            string dependencyProjectFilePath,
            Dictionary<string, string[]> projectsReferencingProjectByProject,
            Dictionary<string, string[][]> dependencyChainsByDependencyProject)
        {
            // Base case: root project is dependency project.
            if(rootProjectFilePath == dependencyProjectFilePath)
            {
                return new[] { new[] { dependencyProjectFilePath } };
            }

            var output = new List<string[]>();

            // For each project referencing the descendant project, get the dependency chains.
            var projectsReferencingDependency = projectsReferencingProjectByProject[dependencyProjectFilePath];

            foreach (var projectReferencingDependency in projectsReferencingDependency)
            {
                // Are the dependency chains for this project already known?
                var dependencyChainsAlreadyKnown = dependencyChainsByDependencyProject.ContainsKey(projectReferencingDependency);

                var dependencyChainsForDependency = dependencyChainsAlreadyKnown
                    // If so, use them.
                    ? dependencyChainsByDependencyProject[projectReferencingDependency]
                    // Else, figure them out recursively.
                    : this.GetDependencyChains_Inclusive(
                        rootProjectFilePath,
                        projectReferencingDependency,
                        projectsReferencingProjectByProject,
                        dependencyChainsByDependencyProject)
                    ;

                // If the dependency chains for the dependency were not already known, add them.
                if (!dependencyChainsAlreadyKnown)
                {
                    dependencyChainsByDependencyProject.Add(projectReferencingDependency, dependencyChainsForDependency);
                }

                // Now add the dependency onto the end of the depdency chains, and add the to the output.
                output.AddRange(
                        dependencyChainsForDependency
                            .Select(x => x.Append(dependencyProjectFilePath).ToArray()));
            }

            return output.ToArray();
        }

        public async Task<Dictionary<string, string[]>> GetProjectsReferencingProjectByProjectForAllRecursiveDependencies(
           string projectFilePath,
           GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var directProjectReferencesByProject_Exclusive = await this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                EnumerableHelper.From(projectFilePath),
                getDirectProjectReferenceDependencies);

            var output = this.GetProjectsReferencingProjectByProject(directProjectReferencesByProject_Exclusive);
            return output;
        }

        public Dictionary<string, string[]> GetProjectsReferencingProjectByProject(
            Dictionary<string, string[]> directProjectReferencesByProject_Exclusive)
        {
            // Initialize.
            var projectsReferencingProjectByProject = new Dictionary<string, List<string>>(directProjectReferencesByProject_Exclusive.Count);

            foreach (var pair in directProjectReferencesByProject_Exclusive)
            {
                projectsReferencingProjectByProject.Add(pair.Key, new List<string>());
            }

            foreach (var pair in directProjectReferencesByProject_Exclusive)
            {
                foreach (var projectReference in pair.Value)
                {
                    var projectsReferencingProject = projectsReferencingProjectByProject[projectReference];

                    projectsReferencingProject.Add(pair.Key);
                }
            }

            return projectsReferencingProjectByProject.ToDictionary(
                x => x.Key,
                x => x.Value.ToArray());
        }

        /// <summary>
        /// Preprocess by removing duplicates and evaluating the enumerable.
        /// Additionally, order the file paths alphabetically to aid debugging.
        /// </summary>
        public string[] PreprocessProjectFilePaths(
            IEnumerable<string> unPreprocessedProjectFilePaths)
        {
            var projectFilePaths = unPreprocessedProjectFilePaths
                // Make distinct to avoid double-work.
                .Distinct()
                .OrderAlphabetically_OnlyIfDebug()
                // Evaluate now so we know what we are working with.
                .Evaluate();

            return projectFilePaths;
        }

        /// <summary>
        /// Gets the direct project references for all project references in the recursive project references set of the specified project file paths.
        /// Exclusive in the sense that the direct dependencies set for each project does not contain the project itself.
        /// </summary>
        public Task<Dictionary<string, string[]>> GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            // Use internal function to change input variable name to reflect un-preprocessed status.
            async Task<Dictionary<string, string[]>> Internal(
                IEnumerable<string> unPreprocessedProjectFilePaths)
            {
                var projectFilePaths = this.PreprocessProjectFilePaths(unPreprocessedProjectFilePaths);

                var output = new Dictionary<string, string[]>();

                var projectFilePathsToProcess = new HashSet<string>(projectFilePaths);

                while (projectFilePathsToProcess.Any())
                {
                    var projectFilePath = projectFilePathsToProcess.First();

                    var alreadyProcessed = output.ContainsKey(projectFilePath);
                    if (!alreadyProcessed)
                    {
                        var directProjectReferenceDependencies = await getDirectProjectReferenceDependencies(projectFilePath);

                        output.Add(projectFilePath, directProjectReferenceDependencies);

                        projectFilePathsToProcess.AddRange(directProjectReferenceDependencies);
                    }

                    projectFilePathsToProcess.Remove(projectFilePath);
                }

                return output;
            }

            return Internal(projectFilePaths);
        }

        public Dictionary<string, string[]> ConvertExclusiveToInclusive(
            Dictionary<string, string[]> exclusive)
        {
            var output = exclusive
                .ToDictionary(
                    x => x.Key,
                    x => x.Value
                        .Append(x.Key)
                        .OrderAlphabetically_OnlyIfDebug()
                        .ToArray()
                    );

            return output;
        }

        /// <inheritdoc cref="GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/>
        public async Task<Dictionary<string, string[]>> GetDirectProjectReferencesForAllRecursiveProjectReferences_Inclusive(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var exclusive = await this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                getDirectProjectReferenceDependencies);

            var output = this.ConvertExclusiveToInclusive(exclusive);
            return output;
        }

        /// <summary>
        /// <inheritdoc cref="GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/>
        /// Chooses <see cref="GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/> as the default.
        /// </summary>
        public Task<Dictionary<string, string[]>> GetDirectProjectReferencesForAllRecursiveProjectReferences(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            return this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                getDirectProjectReferenceDependencies);
        }

        /// <summary>
        /// Gets the recursive project references for all project references in the recursive project references set of the specified project file paths.
        /// </summary>
        public async Task<Dictionary<string, string[]>> GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var directProjectReferencesByProjectFilePath = await this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                getDirectProjectReferenceDependencies);

            var output = new Dictionary<string, string[]>();

            foreach (var pair in directProjectReferencesByProjectFilePath)
            {
                var projectFilePath = pair.Key;
                var directProjectReferences = pair.Value;

                foreach (var projectReferenceFilePath in directProjectReferences)
                {
                    var alreadyProcessed = output.ContainsKey(projectReferenceFilePath);
                    if (!alreadyProcessed)
                    {
                        var recursiveProjectReferences = this.GetRecursiveProjectReferences_Exclusive(
                            projectReferenceFilePath,
                            directProjectReferencesByProjectFilePath,
                            output);

                        output.Add(projectReferenceFilePath, recursiveProjectReferences);
                    }
                }
            }

            return output;
        }

        /// <inheritdoc cref="GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/>
        public async Task<Dictionary<string, string[]>> GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var exclusive = await this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                getDirectProjectReferenceDependencies);

            var output = this.ConvertExclusiveToInclusive(exclusive);
            return output;
        }

        /// <summary>
        /// <inheritdoc cref="GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/>
        /// Chooses <see cref="GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/> as the default.
        /// </summary>
        public Task<Dictionary<string, string[]>> GetRecursiveProjectReferencesForAllRecursiveProjectReferences(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            return this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                getDirectProjectReferenceDependencies);
        }

        /// <summary>
        /// Get recursive project references for a project file, using the given direct project reference sets and recursive project reference sets, while filling-in the recursive project reference sets for any dependencies.
        /// </summary>
        public string[] GetRecursiveProjectReferences_Exclusive(
            string projectFilePath,
            Dictionary<string, string[]> directProjectReferences_Exclusive,
            Dictionary<string, string[]> recursiveProjectReferences_Exclusive)
        {
            var directProjectReferences = directProjectReferences_Exclusive[projectFilePath];

            var recursiveProjectReferences = new HashSet<string>();

            foreach (var projectReferenceFilePath in directProjectReferences)
            {
                var alreadyProcessed = recursiveProjectReferences_Exclusive.ContainsKey(projectReferenceFilePath);
                if (alreadyProcessed)
                {
                    var projectReferenceRecursiveProjectReferences = recursiveProjectReferences_Exclusive[projectReferenceFilePath];

                    recursiveProjectReferences.AddRange(projectReferenceRecursiveProjectReferences);
                }
                else
                {
                    var recursiveProjectReferencesOfReference = this.GetRecursiveProjectReferences_Exclusive(
                        projectReferenceFilePath,
                        directProjectReferences_Exclusive,
                        recursiveProjectReferences_Exclusive);

                    recursiveProjectReferences.AddRange(recursiveProjectReferencesOfReference);

                    recursiveProjectReferences_Exclusive.Add(projectReferenceFilePath, recursiveProjectReferencesOfReference);
                }
            }

            var output = recursiveProjectReferences
                .OrderAlphabetically_OnlyIfDebug()
                .ToArray();

            return output;
        }

        public async Task<string[]> GetRecursiveProjectReferences_Exclusive(
            string projectFilePath,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var directProjectReferencesOfAllRecursiveProjectReferences = await this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                EnumerableHelper.From(projectFilePath),
                getDirectProjectReferenceDependencies);

            var output = directProjectReferencesOfAllRecursiveProjectReferences.Keys
                // Exclusive, so do not return the input project file path itself.
                .Except(projectFilePath)
                .OrderAlphabetically_OnlyIfDebug()
                .ToArray();

            return output;
        }

        /// <summary>
        /// Chooses <see cref="GetRecursiveProjectReferences_Exclusive(string, GetDirectProjectReferenceDependencies)"/> as the default.
        /// </summary>
        public Task<string[]> GetRecursiveProjectReferences(
            string projectFilePath,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            return this.GetRecursiveProjectReferences_Exclusive(
                projectFilePath,
                getDirectProjectReferenceDependencies);
        }

        public async Task<string[]> GetProjectReferencesToAdd(
            string projectFilePath,
            IEnumerable<string> projectReferenceFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var projectReferenceFilePaths_Preprocessed = this.PreprocessProjectFilePaths(projectReferenceFilePaths);

            // Get inclusive, recursive, references by reference file path.
            var recursiveProjectReferences = await this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive(
                projectReferenceFilePaths
                    .Append(projectFilePath),
                getDirectProjectReferenceDependencies);

            // Determine if any of the project reference file paths are dependencies of any of the other project reference file paths.
            // For each reference file path, if any of the other reference file paths' recursive reference sets contain the reference file path, remove it from consideration.
            var topLevelProjectReferenceFilePaths = new List<string>();

            var recursiveProjectReferenceHashes = recursiveProjectReferences
                .ToDictionary(
                    x => x.Key,
                    x => new HashSet<string>(x.Value));

            foreach (var projectReferenceFilePath in projectReferenceFilePaths)
            {
                var projectIsTopLevel = true;

                foreach (var otherProjectReferenceFilePath in projectReferenceFilePaths.Except(projectReferenceFilePath))
                {
                    var otherProjectRecursiveProjectReferencesHash = recursiveProjectReferenceHashes[otherProjectReferenceFilePath];

                    var otherProjectHasProjectAsDependency = otherProjectRecursiveProjectReferencesHash.Contains(projectReferenceFilePath);
                    if (otherProjectHasProjectAsDependency)
                    {
                        projectIsTopLevel = false;

                        break;
                    }
                }

                if (projectIsTopLevel)
                {
                    topLevelProjectReferenceFilePaths.Add(projectReferenceFilePath);
                }
            }

            // For remaining reference file paths, take a union of all their recursive references.
            var projectReferencesRecursiveDependencies = new HashSet<string>();

            foreach (var topLevelProjectFilePath in topLevelProjectReferenceFilePaths)
            {
                var topLevelRecursiveProjectReferences = recursiveProjectReferences[topLevelProjectFilePath];

                projectReferencesRecursiveDependencies.AddRange(topLevelProjectReferenceFilePaths);
            }

            // Get the recursive references for the project file path.
            var projectRecursiveProjectReferences = recursiveProjectReferences[projectFilePath];

            // Get the complement in project references' recursive dependencies set of the project file's recusive dependencies set.
            var newProjectReferencesToAdd = projectReferencesRecursiveDependencies.Except(projectRecursiveProjectReferences)
                .OrderAlphabetically_OnlyIfDebug()
                .Evaluate();

            return newProjectReferencesToAdd;
        }
    }
}
