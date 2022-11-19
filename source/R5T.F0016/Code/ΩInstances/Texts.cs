using System;


namespace R5T.F0016
{
	public class Texts : ITexts
	{
		#region Infrastructure

	    public static ITexts Instance { get; } = new Texts();

	    private Texts()
	    {
        }

	    #endregion
	}
}