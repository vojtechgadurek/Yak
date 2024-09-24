using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yak
{
    class HashSetPredictor : ISymmetricDifferencePredictor, IJaccardIndexPredictor
    {
        private HashSet<ulong> firstSet = new HashSet<ulong>();
        private HashSet<ulong> secondSet = new HashSet<ulong>();

        public void AddValuesFromFirstSet(ulong[] values, int numberOfItems)
        {
            for (int i = 0; i < numberOfItems; i++)
            {
                firstSet.Add((ulong)values[i]);
            }
        }

        public void AddValuesFromSecondSet(ulong[] values, int numberOfItems)
        {
            for (int i = 0; i < numberOfItems; i++)
            {
                secondSet.Add((ulong)values[i]);
            }
        }

        public double PredictJaccardIndex()
        {
            HashSet<ulong> union = new HashSet<ulong>(firstSet);
            union.UnionWith(secondSet);

            HashSet<ulong> intersection = new HashSet<ulong>(firstSet);
            intersection.IntersectWith(secondSet);


            HashSet<ulong> symmetricDifference = new HashSet<ulong>(firstSet);
            symmetricDifference.SymmetricExceptWith(secondSet);
            Console.WriteLine(symmetricDifference.Count);

            Console.WriteLine("[" + string.Join(", ", symmetricDifference) + "]");

            Console.WriteLine(secondSet.Count + firstSet.Count);
            Console.WriteLine(intersection.Count);
            return (double)intersection.Count / union.Count;
        }

        public HashSet<ulong> PredictSymmetricDifference()
        {
            HashSet<ulong> symmetricDifference = new HashSet<ulong>(firstSet);
            symmetricDifference.SymmetricExceptWith(secondSet);

            return symmetricDifference;
        }
    }
}
