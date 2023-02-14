using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using R5T.T0132;


namespace R5T.F0016.Internal
{
    [FunctionalityMarker]
    public interface IProjectReferencesOperator : IFunctionalityMarker
    {
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
                .Now();

            return projectFilePaths;
        }

        /// <summary>
        /// Given a project, a set of previously queried direct project references, and a set of previously computed recursive project references, determine whether enough information is already available to skip querying for the project's direct project references.
        /// </summary>
        /// <param name="projectFilePath">The project of interest.</param>
        /// <param name="directProjectReferencesByProject">Previously queried direct project references, by project.</param>
        /// <param name="recursiveProjectReferencesByProject_Exclusive">Previously computed recursive project references, by project.</param>
        /// <returns>Whether to skip querying the project's direct project references since enough information is already available.</returns>
        public bool ShouldSkipQueryForDirectProjectReferences(
            string projectFilePath,
            IDictionary<string, string[]> directProjectReferencesByProject,
            IDictionary<string, string[]> recursiveProjectReferencesByProject_Exclusive)
        {
            // Check if the project has actually already been handled in the recursive set of project references. Skip if so.
            var projectAlreadyHandled = recursiveProjectReferencesByProject_Exclusive.ContainsKey(projectFilePath);
            if (projectAlreadyHandled)
            {
                return true;
            }

            // Check if the direct project references have already been queried. Skip if so.
            // The can occur if project A references B and C, and then project B references C.
            // There is no need to query project C's direct references twice!
            var directProjectReferencesAlreadyQueried = directProjectReferencesByProject.ContainsKey(projectFilePath);
            if (directProjectReferencesAlreadyQueried)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unchecked in that the function assumes the provided <paramref name="projectFilePath"/> has not actually been handled.
        /// </summary>
        public async Task AccumulateDirectProjectReferencesOfUnhandledRecursiveProjectReferences_Unchecked(
            string projectFilePath,
            IDictionary<string, string[]> directProjectReferencesOfUnhandledRecursiveProjectReferences,
            IDictionary<string, string[]> handledRecursiveProjectReferences,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            var directProjectReferences = await getDirectProjectReferenceDependencies(projectFilePath);

            directProjectReferencesOfUnhandledRecursiveProjectReferences.Add(
                projectFilePath,
                directProjectReferences);

            foreach (var directProjectReference in directProjectReferences)
            {
                // Should skip the direct reference project?
                var shouldSkip = this.ShouldSkipQueryForDirectProjectReferences(
                    directProjectReference,
                    directProjectReferencesOfUnhandledRecursiveProjectReferences,
                    handledRecursiveProjectReferences);

                if (shouldSkip)
                {
                    continue;
                }

                // Else, do the work and recurse.
                await this.AccumulateDirectProjectReferencesOfUnhandledRecursiveProjectReferences_Unchecked(
                    directProjectReference,
                    directProjectReferencesOfUnhandledRecursiveProjectReferences,
                    handledRecursiveProjectReferences,
                    getDirectProjectReferenceDependencies);
            }
        }

        /// <summary>
        /// Checked in that, to avoid spending time querying the project file for its direct project references, if the project is already handled, the method immediately returns.
        /// </summary>
        public async Task AccumulateDirectProjectReferencesOfUnhandledRecursiveProjectReferences_Checked(
            string projectFilePath,
            IDictionary<string, string[]> directProjectReferencesOfUnhandledRecursiveProjectReferences,
            IDictionary<string, string[]> handledRecursiveProjectReferences,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            // Should skip the input project?
            var shouldSkip = this.ShouldSkipQueryForDirectProjectReferences(
                projectFilePath,
                directProjectReferencesOfUnhandledRecursiveProjectReferences,
                handledRecursiveProjectReferences);

            if (shouldSkip)
            {
                return;
            }

            // Else, actually do the work.
            await this.AccumulateDirectProjectReferencesOfUnhandledRecursiveProjectReferences_Unchecked(
                projectFilePath,
                directProjectReferencesOfUnhandledRecursiveProjectReferences,
                handledRecursiveProjectReferences,
                getDirectProjectReferenceDependencies);
        }

        /// <summary>
        /// Chooses <see cref="AccumulateDirectProjectReferencesOfUnhandledRecursiveProjectReferences_Checked(string, IDictionary{string, string[]}, IDictionary{string, string[]}, GetDirectProjectReferenceDependencies)"/> as the default.
        /// </summary>
        public Task AccumulateDirectProjectReferencesOfUnhandledRecursiveProjectReferences(
            string projectFilePath,
            IDictionary<string, string[]> directProjectReferencesOfUnhandledRecursiveProjectReferences,
            IDictionary<string, string[]> handledRecursiveProjectReferences,
            GetDirectProjectReferenceDependencies getDirectProjectReferenceDependencies)
        {
            return this.AccumulateDirectProjectReferencesOfUnhandledRecursiveProjectReferences_Checked(
                projectFilePath,
                directProjectReferencesOfUnhandledRecursiveProjectReferences,
                handledRecursiveProjectReferences,
                getDirectProjectReferenceDependencies);
        }

        public void AccumulateAndAddRecursiveProjectFilePaths_Exclusive(
            string projectFilePath,
            HashSet<string> recursiveProjectFilePaths,
            IDictionary<string, string[]> directProjectReferencesOfUnhandledRecursiveProjectReferences,
            IDictionary<string, string[]> handledRecursiveProjectReferences)
        {
            var alreadyHandled = handledRecursiveProjectReferences.ContainsKey(projectFilePath);
            if (alreadyHandled)
            {
                // Base case: recursive project references already exist for the project file path.
                var recursiveProjectReferences = handledRecursiveProjectReferences[projectFilePath];

                recursiveProjectFilePaths.AddRange(recursiveProjectReferences);
            }
            else
            {
                // Get the direct dependencies of the project, accumulate their recursive project references, then add the project so the projects are produced in dependency order, from least to most dependent.
                var directProjectReferences = directProjectReferencesOfUnhandledRecursiveProjectReferences[projectFilePath];

                foreach (var directProjectReference in directProjectReferences)
                {
                    // Check if the direct project reference has already been handled (it may have been dependency of prior dependency).
                    var directProjectReferenceAlreadyHandled = handledRecursiveProjectReferences.ContainsKey(directProjectReference);
                    if (directProjectReferenceAlreadyHandled)
                    {
                        var recursiveProjectFilePathsForHandledDirectProjectReference = handledRecursiveProjectReferences[directProjectReference];

                        recursiveProjectFilePaths.AddRange(recursiveProjectFilePathsForHandledDirectProjectReference);
                    }
                    else
                    {
                        var recursiveProjectFilePathsForUnhandledDirectProjectReference = new HashSet<string>();

                        this.AccumulateAndAddRecursiveProjectFilePaths_Exclusive(
                            directProjectReference,
                            recursiveProjectFilePathsForUnhandledDirectProjectReference,
                            directProjectReferencesOfUnhandledRecursiveProjectReferences,
                            handledRecursiveProjectReferences);

                        // Add the dependency to the set of handled projects.
                        handledRecursiveProjectReferences.Add(
                            directProjectReference,
                            recursiveProjectFilePathsForUnhandledDirectProjectReference.ToArray());

                        // Add the recursive dependencies for the direct project reference to the recursive dependencies of the project.
                        recursiveProjectFilePaths.AddRange(recursiveProjectFilePathsForUnhandledDirectProjectReference);
                    }

                    // And then add the direct dependency.
                    recursiveProjectFilePaths.Add(directProjectReference);
                }
            }
        }
    }
}
