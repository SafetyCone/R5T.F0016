using System;


namespace R5T.F0016
{
    public class ProjectReferencesOperator : IProjectReferencesOperator
    {
        #region Infrastructure

        public static IProjectReferencesOperator Instance { get; } = new ProjectReferencesOperator();

        private ProjectReferencesOperator()
        {
        }

        #endregion
    }
}


namespace R5T.F0016.Internal
{
    public class ProjectReferencesOperator : IProjectReferencesOperator
    {
        #region Infrastructure

        public static IProjectReferencesOperator Instance { get; } = new ProjectReferencesOperator();

        private ProjectReferencesOperator()
        {
        }

        #endregion
    }
}
