using System.Collections.Generic;

namespace Buzhou
{
    public class Context
    {
        public object Request { get; set; }

        public Dictionary<string, object> UserData { get; } = new Dictionary<string, object>();
    }
}