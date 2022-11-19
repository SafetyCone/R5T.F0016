using System;
using System.Threading.Tasks;
using System.Linq;

using R5T.T0120;
using R5T.T0132;


namespace R5T.F0016.Construction
{
	[FunctionalityMarker]
	public partial interface ITry : IFunctionalityMarker
	{
		public async Task RemoveExtraneousProjectReferencesByProject()
		{
			var onlyExtraneousDependenciesForAllRecursiveProjects = await F001.ProjectReferencesOperator.Instance.RemoveExtraneousProjectReferencesFromAllRecursiveReferences(
				Z0018.ProjectFilePaths.Instance.HasExtraneousProjectReference);

			OutputOperations.Instance.WriteToOutput_Synchronous(
				onlyExtraneousDependenciesForAllRecursiveProjects,
				Z0015.FilePaths.Instance.OutputTextFilePath);

			F0033.NotepadPlusPlusOperator.Instance.Open(
				Z0015.FilePaths.Instance.OutputTextFilePath);
		}

		public async Task IdentifyExtraneousProjectReferencesByProject()
        {
			var extraneousDependenciesForAllRecursiveProjects = await F001.ProjectReferencesOperator.Instance.GetExtraneousProjectReferencesForAllRecursiveReferencesByProject(
				Z0018.ProjectFilePaths.Instance.HasExtraneousProjectReference);

			OutputOperations.Instance.WriteToOutput_Synchronous(
				extraneousDependenciesForAllRecursiveProjects,
				Z0015.FilePaths.Instance.OutputTextFilePath);

			F0033.NotepadPlusPlusOperator.Instance.Open(
				Z0015.FilePaths.Instance.OutputTextFilePath);
		}

		public async Task OutputDirectProjectReferencesForAllRecursiveProjects()
		{
			var directProjectReferencesForAllRecursiveProjects = await F001.ProjectReferencesOperator.Instance.GetDirectProjectReferencesForAllRecursiveProjectReferences_Exclusive(
				Z0018.ProjectFilePaths.Instance.HasExtraneousProjectReference);

			OutputOperations.Instance.WriteToOutput_Synchronous(
				directProjectReferencesForAllRecursiveProjects,
				Z0015.FilePaths.Instance.OutputTextFilePath);

			F0033.NotepadPlusPlusOperator.Instance.Open(
				Z0015.FilePaths.Instance.OutputTextFilePath);
		}

		public async Task OutputRecursiveProjectReferencesByProject()
		{
			var recursiveProjectReferencesByProjectFilePath = await F001.ProjectReferencesOperator.Instance.GetRecursiveProjectReferencesForAllRecursiveProjectReferences(
				Z0018.ProjectFilePaths.Instance.HasExtraneousProjectReference);

			OutputOperations.Instance.WriteToOutput_Synchronous(
				recursiveProjectReferencesByProjectFilePath,
				Z0015.FilePaths.Instance.OutputTextFilePath);

			F0033.NotepadPlusPlusOperator.Instance.Open(
				Z0015.FilePaths.Instance.OutputTextFilePath);
		}

		public async Task AnyRecursiveCOMReferences()
        {
			var expectation =
				//ExpectationOperator.Instance.From(
				//	Z0018.ProjectFilePaths.Instance.HasRecursiveCOMReference,
				//	true)
				ExpectationOperator.Instance.From(
					Z0018.ProjectFilePaths.Instance.HasNoRecursiveCOMReferences,
					false)
				;

			var hasAnyRecursiveCOMReferences = await F001.ProjectReferencesOperator.Instance.HasAnyRecursiveCOMReferences_Inclusive(
				expectation);

			expectation.Verify_OrThrow(hasAnyRecursiveCOMReferences);
        }
	}
}