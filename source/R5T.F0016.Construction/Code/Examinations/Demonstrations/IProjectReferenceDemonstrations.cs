using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using R5T.T0141;


namespace R5T.F0016.Construction
{
	[DemonstrationsMarker]
	public partial interface IProjectReferenceDemonstrations : IDemonstrationsMarker
	{
		/// <summary>
		/// For a root project and a target project within the recursive project references of the root project, starting at the target project, walk up the project references tree and accumulate each path from the target to the root project.
		/// </summary>
		/// <returns></returns>
		public async Task GetDependencyChainsForProject()
        {
			/// Inputs.
			var rootProjectFilePath = @"C:\Code\DEV\Git\GitHub\davidcoats\D8S.S0002.Private\source\D8S.S0002\D8S.S0002.csproj";
			var dependencyProjectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.Magyar\source\R5T.Magyar\R5T.Magyar.csproj";

			var outputFilePath = @"C:\Temp\Dependency Chains For Project.txt";

			/// Run.
			var dependencyChains_Inclusive = await Instances.ProjectReferencesOperator.GetDependencyChains_Inclusive(
				rootProjectFilePath,
				dependencyProjectFilePath,
				Instances.ProjectFileOperator.GetDirectProjectReferenceFilePaths);

			// Write output file.
			var lines = EnumerableHelper.From($"For dependency project:\n{dependencyProjectFilePath}\n")
				.Append($"Within root project:\n{rootProjectFilePath}\n")
				.Append(dependencyChains_Inclusive
					.SelectMany(projects => projects
						// Skip the first since it will be the root project.
						.SkipFirst()
						.Append(String.Empty)))
				;

			FileHelper.WriteAllLines_Synchronous(
				outputFilePath,
				lines);

			// Open output file.
			F0033.NotepadPlusPlusOperator.Instance.Open(outputFilePath);
		}

		/// <summary>
		/// For each of the recursive project references of a root project, get the projects within the recursive project references of the root project that reference that target project.
		/// </summary>
		public async Task GetProjectsReferencingProjectByProjectForAllRecursiveDependencies()
        {
			var projectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.S0041\source\R5T.S0041\R5T.S0041.csproj";

			var projectsReferencingProjectByProject = await Instances.ProjectReferencesOperator.GetProjectsReferencingProjectByProjectForAllRecursiveDependencies(
				projectFilePath,
				Instances.ProjectFileOperator.GetDirectProjectReferenceFilePaths);

			var outputFilePath = @"C:\Temp\Projects Referencing Projects.txt";

			var lines = projectsReferencingProjectByProject
				.OrderAlphabetically(x => x.Key)
				.SelectMany(xPair => EnumerableHelper.From($"{xPair.Key}\n")
					.Append(xPair.Value
						.OrderAlphabetically()
						.Select(x => $"\t{x}"))
					.Append(String.Empty))
				;

			FileHelper.WriteAllLines_Synchronous(
				outputFilePath,
				lines);
		}

		/// <summary>
		/// For a given root project, and another target project referenced either directly or recursively by the root project, get the list of projects in the recursive project references of the root project, that reference the target project.
		/// </summary>
		public async Task GetProjectsReferencingProjectByProjectForProject()
		{
			var rootProjectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.S0041\source\R5T.S0041\R5T.S0041.csproj";
			var dependencyProjectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.T0041\source\R5T.T0041.X002\R5T.T0041.X002.csproj";

			var projectsReferencingProjectByProject = await Instances.ProjectReferencesOperator.GetProjectsReferencingProjectByProjectForAllRecursiveDependencies(
				rootProjectFilePath,
				Instances.ProjectFileOperator.GetDirectProjectReferenceFilePaths);

			var outputFilePath = @"C:\Temp\Projects Referencing Project.txt";

			var lines = EnumerableHelper.From($"For root project:\n{rootProjectFilePath}\n")
				.Append($"{dependencyProjectFilePath}\n")
				.Append(projectsReferencingProjectByProject[dependencyProjectFilePath]
					.Select(x => $"\t{x}"))
				;

			FileHelper.WriteAllLines_Synchronous(
				outputFilePath,
				lines);
		}

		/// <summary>
		/// Lists all project references for a project, recursively (i.e. including all projects referenced the projects referenced by the project).
		/// </summary>
		public async Task GetRecursiveProjectReferences()
		{
			var projectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.S0041\source\R5T.S0041\R5T.S0041.csproj";

			var projectReferenceFilePaths = await Instances.ProjectReferencesOperator.GetRecursiveProjectReferences(
				projectFilePath,
				Instances.ProjectFileOperator.GetDirectProjectReferenceFilePaths);

			var outputFilePath = @"C:\Temp\Recursive Project References.txt";

			FileHelper.WriteAllLines_Synchronous(
				outputFilePath,
				projectReferenceFilePaths
					.OrderAlphabetically());
		}
	}
}