using System;

namespace BattlePlanPath
{
    /// <summary>
    /// Indicates a problem with the PathSolver operation.
    /// </summary>
    [System.Serializable]
    public class PathfindingException : System.Exception
    {
        public PathfindingException() { }
        public PathfindingException(string message) : base(message) { }
        public PathfindingException(string message, System.Exception inner) : base(message, inner) { }
        protected PathfindingException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
