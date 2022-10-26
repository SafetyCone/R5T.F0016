using System;


namespace R5T.F0016.F001
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