using System;
using System.Threading.Tasks;


namespace R5T.F0016
{
    public delegate Task<string[]> GetDirectProjectReferenceDependencies(string projectFilePath);
}
