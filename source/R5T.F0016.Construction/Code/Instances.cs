using System;

using R5T.F0020;


namespace R5T.F0016.Construction
{
    public static class Instances
    {
        public static IProjectReferenceDemonstrations ProjectReferenceDemonstrations => Construction.ProjectReferenceDemonstrations.Instance;

        public static IProjectFileOperator ProjectFileOperator => F0020.ProjectFileOperator.Instance;
        public static F001.IProjectReferencesOperator ProjectReferencesOperator => F001.ProjectReferencesOperator.Instance;
        public static Z0000.IStrings Strings => Z0000.Strings.Instance;
    }
}