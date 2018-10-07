using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot_Builder_Echo_Bot_V4
{
    public class TopicState
    {
        public string Stage { get; set; } = "intro";

        public string Prompt { get; set; } = "hi";

    }
}
