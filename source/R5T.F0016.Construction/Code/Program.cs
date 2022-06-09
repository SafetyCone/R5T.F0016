using System;
using System.Threading.Tasks;


namespace R5T.F0016.Construction
{
    static class Program
    {
        static async Task Main()
        {
            await Instances.ProjectReferenceDemonstrations.GetDependencyChainsForProject();
            //await Instances.ProjectReferenceDemonstrations.GetProjectsReferencingProjectByProjectForAllRecursiveDependencies();
            //await Instances.ProjectReferenceDemonstrations.GetProjectsReferencingProjectByProjectForProject();
            //await Instances.ProjectReferenceDemonstrations.GetRecursiveProjectReferences();
        }
    }
}