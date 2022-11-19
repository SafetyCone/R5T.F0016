using System;


namespace R5T.F0016
{
	public class OutputOperations : IOutputOperations
	{
		#region Infrastructure

	    public static IOutputOperations Instance { get; } = new OutputOperations();

	    private OutputOperations()
	    {
        }

	    #endregion
	}
}