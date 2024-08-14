using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquareGrid.Common.Exceptions
{
    public class SquareGridException : Exception
    {
        public SquareGridException(string error) : base(error) { }
    }
}
