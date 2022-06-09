using System;


namespace R5T.F0016
{
    public class Glossary
    {
        /// <summary>
        /// The direct project references set of a project includes only direct dependencies of a project.
        /// </summary>
        public static readonly object DirectProjectReferencesSet;

        /// <summary>
        /// A project references set for a project is inclusive if it does not contain the project itself.
        /// </summary>
        public static readonly object ExclusiveProjectReferencesSet;

        /// <summary>
        /// A project references set for a project is inclusive if it contains the project itself.
        /// </summary>
        public static readonly object InclusiveProjectReferencesSet;

        /// <summary>
        /// A project reference is "available" to a project if it is in the inclusive recursive project references set of the project.
        /// </summary>
        public static readonly object ProjectReferencesAvailable;

        /// <summary>
        /// The recursive project references set of a project includes all direct dependencies of a project, and all dependencies of it's dependencies, recursively.
        /// </summary>
        public static readonly object RecursiveProjectReferencesSet;
    }
}
