using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace WPR.MonoGameCompability.GamerServices
{
    [Serializable]
    public class GameUpdateRequiredException : Exception
    {
        public GameUpdateRequiredException()
        {
        }

        public GameUpdateRequiredException(string message)
            : base(message)
        {
        }

        protected GameUpdateRequiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public GameUpdateRequiredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
