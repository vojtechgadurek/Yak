using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yak
{
    public interface ISymmetricDifferenceComparer
    {
        void AddValuesFromFirstSet(ulong[] values, int numberOfItems);
        void AddValuesFromSecondSet(ulong[] values, int numberOfItems);

    }

    public interface ISymmetricDifferencePredictor : ISymmetricDifferenceComparer
    {
        HashSet<ulong> PredictSymmetricDifference();
    }

    public interface IJaccardIndexPredictor : ISymmetricDifferenceComparer
    {

        double PredictJaccardIndex();
    }

    public interface IMergeable<T> : ISymmetricDifferenceComparer
    {
        void Merge(T Other);
    }
}
