using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yak
{

    internal record class Pipeline : ISketch
    {
        ISketch[] sketches;
        public byte[] GetByteRepresentation()
        {
            throw new NotImplementedException();
        }

        public void ToggleValues(ulong[] values, int numberOfItems)
        {
            foreach (var sketch in sketches)
            {
                sketch.ToggleValues(values, numberOfItems);
            }
        }
    }
}
