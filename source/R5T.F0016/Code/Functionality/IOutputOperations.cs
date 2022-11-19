using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using R5T.F0000;
using R5T.T0132;
using R5T.Z0000;


namespace R5T.F0016
{
	[FunctionalityMarker]
	public partial interface IOutputOperations : IFunctionalityMarker
	{
		public IEnumerable<string> GetOutputLines(Dictionary<string, string[]> projectReferencesByProjectFilePath)
		{
			var lines = EnumerableOperator.Instance.Empty<string>()
				.AppendIf(projectReferencesByProjectFilePath.Any(),
					projectReferencesByProjectFilePath
						.SelectMany(pair => EnumerableOperator.Instance.From(pair.Key)
							.Append(EnumerableOperator.Instance.Empty<string>()
								.AppendIf(pair.Value.Any(),
									pair.Value,
									EnumerableOperator.Instance.From(
										Texts.Instance.NoDependencies))
								.Select(StringOperator.Instance.Indent))
							.Append(Strings.Instance.Empty)),
					EnumerableOperator.Instance.From(
						Texts.Instance.NoProjectReferences))
				;

			return lines;
		}

		public void WriteToOutput_Synchronous(
			Dictionary<string, string[]> projectReferencesByProjectFilePath,
			StreamWriter writer)
        {
			var lines = this.GetOutputLines(projectReferencesByProjectFilePath);

			StreamWriterOperator.Instance.WriteAllLines_Synchronous(
				writer,
				lines);
        }

		public void WriteToOutput_Synchronous(
			Dictionary<string, string[]> projectReferencesByProjectFilePath,
			string textFilePath)
        {
			using var writer = StreamWriterOperator.Instance.NewWrite(textFilePath);

			this.WriteToOutput_Synchronous(
				projectReferencesByProjectFilePath,
				writer);
        }
	}
}