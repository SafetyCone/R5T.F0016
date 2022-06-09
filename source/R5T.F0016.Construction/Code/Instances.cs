using System;

using R5T.F0020;


namespace R5T.F0016.Construction
{
    public static class Instances
    {
        public static IProjectReferenceDemonstrations ProjectReferenceDemonstrations { get; } = Construction.ProjectReferenceDemonstrations.Instance;

        public static IProjectFileOperator ProjectFileOperator { get; } = F0020.ProjectFileOperator.Instance;
        public static IProjectReferencesOperator ProjectReferencesOperator { get; } = F0016.ProjectReferencesOperator.Instance;
    }
}