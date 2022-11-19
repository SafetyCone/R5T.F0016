using System;

using R5T.T0131;


namespace R5T.F0016
{
	[ValuesMarker]
	public partial interface ITexts : IValuesMarker
	{
		public string NoDependencies => "<No Dependencies>";
		public string NoProjectReferences => "<No Project References>";
	}
}