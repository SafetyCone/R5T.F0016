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
		public async Task GetDependencyChainsForProject()
        {
			var rootProjectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.S0038\source\R5T.S0038\R5T.S0038.csproj";
			var dependencyProjectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.T0045\source\R5T.T0045.X001\R5T.T0045.X001.csproj";

			var dependencyChains_Inclusive = await Instances.ProjectReferencesOperator.GetDependencyChains_Inclusive(
				rootProjectFilePath,
				dependencyProjectFilePath,
				Instances.ProjectFileOperator.GetDirectProjectReferenceFilePaths);

			var outputFilePath = @"C:\Temp\Dependency Chains For Project.txt";

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
		}

		public async Task GetProjectsReferencingProjectByProjectForAllRecursiveDependencies()
        {
			var projectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.S0038\source\R5T.S0038\R5T.S0038.csproj";

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

		public async Task GetProjectsReferencingProjectByProjectForProject()
		{
			var rootProjectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.S0038\source\R5T.S0038\R5T.S0038.csproj";
			var dependencyProjectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.D0082\source\R5T.D0082.D001.I001\R5T.D0082.D001.I001.csproj";

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

		public async Task GetRecursiveProjectReferences()
		{
			var projectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.S0038\source\R5T.S0038\R5T.S0038.csproj";

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