using System;
using System.Threading.Tasks;


namespace R5T.F0016.Construction
{
    static class Program
    {
        static async Task Main()
        {
            await Instances.ProjectReferenceDemonstrations.GetRecursiveProjectReferences();
            //await Instances.ProjectReferenceDemonstrations.GetProjectsReferencingProjectByProjectForProject();
            //await Instances.ProjectReferenceDemonstrations.GetProjectsReferencingProjectByProjectForAllRecursiveDependencies();
            //await Instances.ProjectReferenceDemonstrations.GetDependencyChainsForProject();
        }
    }
}