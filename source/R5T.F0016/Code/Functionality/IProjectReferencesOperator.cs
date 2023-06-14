using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using R5T.F0000;
using R5T.T0132;


namespace R5T.F0016
{
    [FunctionalityMarker]
    public interface IProjectReferencesOperator : IFunctionalityMarker
    {
        private static Internal.IProjectReferencesOperator Internal => F0016.Internal.ProjectReferencesOperator.Instance;


        /// <summary>
        /// Given an existing set of recursive project references by project, determine the recursive project references for new projects and add them to the set.
        /// Recursive project references are determined using both the existing set of recursive project references, and recursively querying the direct project references for any projects not already in the set.
        /// The function is idempotent in that there is no error if projects in the list of projects to add that already exist in the recursive set. These projects are not re-added.
        /// </summary>
        /// <param name="recursiveProjectReferencesByProject_Exclusive">The set of existing recursive project references for each project file path, exclusive in the </param>
        /// <param name="projectFilePaths">The set of project file paths to add to the existing recursive project references set by computing their recursive project references.</param>
        /// <returns>The set of project file paths that were actually added to the recursive refernces set (those project that did not already exist).</returns>
        public async Task<string[]> AddRecursiveProjectReferences_Exclusive_Idempotent(
            IDictionary<string, string[]> recursiveProjectReferencesByProject_Exclusive,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies,
            IEnumerable<string> projectFilePaths)
        {
            var initialProjects = recursiveProjectReferencesByProject_Exclusive.Keys.Now();

            //// Compute the set of project file paths to add before doing anything (as opposed to checking if they already exist in the loop below).
            //// Since some of the projects can be recursive references of other of the projects, and if we did not compute this initially the referenced projects would already exist, and thus not be recognized as being added.
            //var projectFilePathsToAdd = projectFilePaths
            //    .Except(recursiveProjectReferencesByProject_Exclusive.Keys)
            //    .Now();

            foreach (var projectFilePath in projectFilePaths)
            {
                var projectFilePathAlreadyHandled = recursiveProjectReferencesByProject_Exclusive.ContainsKey(projectFilePath);
                if (!projectFilePathAlreadyHandled)
                {
                    var directProjectReferencesOfUnhandledRecursiveProjectReferences = new Dictionary<string, string[]>();

                    await Internal.AccumulateDirectProjectReferencesOfUnhandledRecursiveProjectReferences_Unchecked(
                        projectFilePath,
                        directProjectReferencesOfUnhandledRecursiveProjectReferences,
                        recursiveProjectReferencesByProject_Exclusive,
                        getDirectProjectReferenceDependencies);

                    foreach (var unhandledProjectFilePath in directProjectReferencesOfUnhandledRecursiveProjectReferences.Keys)
                    {
                        // Check if the unhandled project file path is still unhandled (it might be a dependency of a prior unhandled project, in which case it got handled).
                        var isNowHandled = recursiveProjectReferencesByProject_Exclusive.ContainsKey(unhandledProjectFilePath);
                        if(!isNowHandled)
                        {
                            var recursiveProjectReferences = new HashSet<string>();

                            Internal.AccumulateAndAddRecursiveProjectFilePaths_Exclusive(
                                unhandledProjectFilePath,
                                recursiveProjectReferences,
                                directProjectReferencesOfUnhandledRecursiveProjectReferences,
                                recursiveProjectReferencesByProject_Exclusive);

                            recursiveProjectReferencesByProject_Exclusive.Add(
                                unhandledProjectFilePath,
                                recursiveProjectReferences.ToArray());
                        }
                    }
                }
            }

            var finalProjects = recursiveProjectReferencesByProject_Exclusive.Keys;

            var projectsAdded = finalProjects
                .Except(initialProjects)
                .Now();

            return projectsAdded;
        }

        /// <inheritdoc cref="AddRecursiveProjectReferences_Exclusive_Idempotent(IDictionary{string, string[]}, GetDirectProjectReferenceDependencies, IEnumerable{string})"/>
        public Task<string[]> AddRecursiveProjectReferences_Exclusive_Idempotent(
            IDictionary<string, string[]> recursiveProjectReferencesByProject_Exclusive,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies,
            params string[] projectFilePaths)
        {
            return this.AddRecursiveProjectReferences_Exclusive_Idempotent(
                recursiveProjectReferencesByProject_Exclusive,
                getDirectProjectReferenceDependencies,
                projectFilePaths.AsEnumerable());
        }

        /// <summary>
        /// For a set of project file paths, get the set of recursive project references for those projects.
        /// Return the recursive project references in ascending dependency order (from least dependent to most dependent).
        /// <para>Note: output is exclusive of inputs (output does not contain inputs).</para>
        /// </summary>
        public async Task<string[]> Get_RecursiveProjectReferences_InDependencyOrder(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var recursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive = await this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                getDirectProjectReferenceDependencies);

            var recursiveProjectFilePaths = recursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive.Keys
                .OrderBy(
                    x => x,
                    new MethodBasedComparer<string>(
                        (projectFilePath1, projectFilePath2) =>
                        {
                            var recursiveDependenciesof1 = recursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive[projectFilePath1];

                            var recursiveDependenciesof1Contains2 = recursiveDependenciesof1.Contains(projectFilePath2);
                            if(recursiveDependenciesof1Contains2)
                            {
                                return Instances.ComparisonResults.GreaterThan;
                            }
                            else
                            {
                                // Return equal-to to preserve order.
                                return Instances.ComparisonResults.EqualTo;
                            }
                        }))
                .ToArray();

            return recursiveProjectFilePaths;
        }

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
                EnumerableOperator.Instance.From(rootProjectFilePath),
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
                EnumerableOperator.Instance.From(projectFilePath),
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
        /// Gets the direct project references for all project references in the recursive project references set of the specified project file paths.
        /// Exclusive in the sense that the set of direct dependencies for each project does not contain the project itself (which is available as the key in the output dictionary).
        /// </summary>
        public Task<Dictionary<string, string[]>> GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            // Use internal function to change input variable name to reflect un-preprocessed status.
            async Task<Dictionary<string, string[]>> Internal(
                IEnumerable<string> unPreprocessedProjectFilePaths)
            {
                var projectFilePaths = IProjectReferencesOperator.Internal.PreprocessProjectFilePaths(unPreprocessedProjectFilePaths);

                var output = new Dictionary<string, string[]>();

                var projectFilePathsToProcess = new HashSet<string>(projectFilePaths);

                while (projectFilePathsToProcess.Any())
                {
                    var projectFilePath = projectFilePathsToProcess.First();

                    var alreadyProcessed = output.ContainsKey(projectFilePath);
                    if (!alreadyProcessed)
                    {
                        try
                        {
                            var directProjectReferenceDependencies = await getDirectProjectReferenceDependencies(projectFilePath);

                            output.Add(projectFilePath, directProjectReferenceDependencies);

                            projectFilePathsToProcess.AddRange(directProjectReferenceDependencies);
                        }
                        catch(Exception)
                        {
                            // If there is an error getting direct references for a project, just add an empty set of references.
                            output.Add(projectFilePath, Array.Empty<string>());
                        }
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

            var output = this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                directProjectReferencesByProjectFilePath);

            return output;
        }

        /// <inheritdoc cref="GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/>
        public Dictionary<string, string[]> GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
            IEnumerable<string> projectFilePaths,
            Dictionary<string, string[]> directProjectReferencesByProjectFilePath)
        {
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

        /// <summary>
        /// Inclusive in that all input projects are included in the output.
        /// </summary>
        public Dictionary<string, string[]> GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive(
            Dictionary<string, string[]> directProjectReferencesByProjectFilePath)
        {
            var output = new Dictionary<string, string[]>();

            foreach (var pair in directProjectReferencesByProjectFilePath)
            {
                var projectFilePath = pair.Key;

                var projectAlreadyProcessed = output.ContainsKey(projectFilePath);
                if (!projectAlreadyProcessed)
                {
                    var directProjectReferences = pair.Value;

                    var allRecursiveProjectReferences = new HashSet<string>(directProjectReferences);

                    foreach (var projectReferenceFilePath in directProjectReferences)
                    {
                        var directReferenceAlreadyProcessed = output.ContainsKey(projectReferenceFilePath);
                        if (!directReferenceAlreadyProcessed)
                        {
                            var recursiveProjectReferences = this.GetRecursiveProjectReferences_Exclusive(
                                projectReferenceFilePath,
                                directProjectReferencesByProjectFilePath,
                                output);

                            output.Add(projectReferenceFilePath, recursiveProjectReferences);
                        }

                        allRecursiveProjectReferences.AddRange(
                            output[projectReferenceFilePath]);
                    }

                    output.Add(
                        projectFilePath,
                        allRecursiveProjectReferences.ToArray());
                }
            }

            return output;
        }

        /// <inheritdoc cref="GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/>
        public Dictionary<string, string[]> GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
            Dictionary<string, string[]> directProjectReferencesByProjectFilePath)
        {
            var output = this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                directProjectReferencesByProjectFilePath.Keys,
                directProjectReferencesByProjectFilePath);

            return output;
        }

        /// <inheritdoc cref="GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Exclusive(IEnumerable{string}, GetDirectProjectReferenceDependencies)"/>
        public async Task<Dictionary<string, string[]>> GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive(
            IEnumerable<string> projectFilePaths,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var directProjectReferencesByProjectFilePath = await this.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
                projectFilePaths,
                getDirectProjectReferenceDependencies);

            var output = this.GetRecursiveProjectReferencesForAllRecursiveProjectReferences_Inclusive(
                directProjectReferencesByProjectFilePath);

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
        /// Note: returns recursive project references in reverse-dependency order (most dependent to least dependent).
        /// </summary>
        public string[] GetRecursiveProjectReferences_Exclusive(
            string projectFilePath,
            Dictionary<string, string[]> directProjectReferences_Exclusive,
            Dictionary<string, string[]> recursiveProjectReferences_Exclusive)
        {
            var directProjectReferences = directProjectReferences_Exclusive[projectFilePath];

            // Start with the direct project references.
            var recursiveProjectReferences = new HashSet<string>(directProjectReferences);

            // Then for each direct project reference:
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
                EnumerableOperator.Instance.From(projectFilePath),
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
            var projectReferenceFilePaths_Preprocessed = Internal.PreprocessProjectFilePaths(projectReferenceFilePaths);

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
                .Now();

            return newProjectReferencesToAdd;
        }
    }
}
