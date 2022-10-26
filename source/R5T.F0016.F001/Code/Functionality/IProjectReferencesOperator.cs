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
    }
}