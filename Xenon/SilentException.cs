using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenon
{
    public class SilentException : Exception
    {
        // throw to reach finally
        public SilentException() { }
    }
}
