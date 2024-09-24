using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yak
{
    public interface ISketch
    {
        byte[] GetByteRepresentation();
        void ToggleValues(ulong[] values, int numberOfItems);

        //void Merge(T other);
        //void SymmetricDifference(T Other);
    }
}
