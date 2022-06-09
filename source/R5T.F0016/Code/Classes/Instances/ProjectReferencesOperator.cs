using System;


namespace R5T.F0016
{
    public class ProjectReferencesOperator : IProjectReferencesOperator
    {
        #region Infrastructure

        public static ProjectReferencesOperator Instance { get; } = new();

        private ProjectReferencesOperator()
        {
        }

        #endregion
    }
}
