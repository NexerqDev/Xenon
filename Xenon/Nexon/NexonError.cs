using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenon.Nexon
{
    public class NexonError : Exception
    {
        public NexonError(string code, string message)
            : base($"{message} [{code}]") { }
    }
}
