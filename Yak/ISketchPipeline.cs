using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yak
{
    public interface ISketchPipeline
    {
        void ToggleValues(ulong[] values, int numberOfItems);
        void Merge();
    }
}
