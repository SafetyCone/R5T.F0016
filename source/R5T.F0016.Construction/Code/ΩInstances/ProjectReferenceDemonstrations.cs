using System;


namespace R5T.F0016.Construction
{
	public class ProjectReferenceDemonstrations : IProjectReferenceDemonstrations
	{
		#region Infrastructure

	    public static ProjectReferenceDemonstrations Instance { get; } = new();

	    private ProjectReferenceDemonstrations()
	    {
        }

	    #endregion
	}
}