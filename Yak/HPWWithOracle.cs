using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SymmetricDifferenceFinder.Tables;
using SymmetricDifferenceFinder.Encoders;
using FlashHash.SchemesAndFamilies;
using SymmetricDifferenceFinder.Improvements;
using SymmetricDifferenceFinder.Improvements.StringFactories;
using SymmetricDifferenceFinder.Improvements.Oracles;
using SymmetricDifferenceFinder.Decoders.HPW;
using SymmetricDifferenceFinder.Decoders.Common;
using LittleSharp.Literals;
using BenchmarkDotNet.Attributes;

namespace Yak
{
    public class HPWWithOracle : IJaccardIndexPredictor, ISymmetricDifferencePredictor
    {
        Encoder<XORTable> _encoder;
        XORTable _XORTable;

        Massager<KMerStringFactory, CanonicalOrder> _decoder;

        int _count = 0;
        bool decoded = false;

        public int NStepsDecoderInitial { get => _decoder.NStepsDecoderInitial; set { _decoder.NStepsDecoderInitial = value; } }
        public int NStepsDecoder { get => _decoder.NStepsDecoder; set { _decoder.NStepsDecoder = value; } }
        public int NStepsRecovery
        {
            get => _decoder.NStepsRecovery; set
            {
                _decoder.NStepsRecovery = value;
            }
        }
        public int GetCount() => _count;

        public HPWWithOracle(int predictedSymmetricDifferenceSize, int nHashFunction, int nStepsDecoderInitial = 1000, int nStepsDecoder = 100, int nRecoverySteps = 10)
        {
            var TabFam = new TabulationFamily();

            var hfSchemes = Enumerable.Range(0, nHashFunction).Select(i => TabFam.GetScheme((ulong)(predictedSymmetricDifferenceSize), 0)).Select(x => (IHashFunctionScheme)x).ToList(); // : new() { TabFam.GetScheme((ulong)(predictedSymmetricDifferenceSize / 2), 0), TabFam.GetScheme((ulong)(predictedSymmetricDifferenceSize / 2), (ulong)(predictedSymmetricDifferenceSize / 2)) };
            var hfs = hfSchemes.Select(x => x.Create()).ToList();
            _encoder = new EncoderFactory<XORTable>(new EncoderConfiguration<XORTable>(hfSchemes, predictedSymmetricDifferenceSize), (size) => new XORTable(size)).Create();
            _XORTable = _encoder.GetTable();
            _decoder = new Massager<KMerStringFactory, CanonicalOrder>(new HPWDecoderFactory<XORTable>(hfs).Create(_XORTable), hfs.Select(hf => hf.Compile()), nStepsDecoderInitial, nStepsDecoder, nRecoverySteps);
        }
        public void AddValuesFromFirstSet(ulong[] values, int numberOfItems)
        {
            _count += numberOfItems;
            _encoder.Encode(values, numberOfItems);

        }

        public void AddValuesFromSecondSet(ulong[] values, int numberOfItems)
        {
            _count += numberOfItems;
            _encoder.Encode(values, numberOfItems);
        }

        public double PredictJaccardIndex()
        {
            if (!decoded)
            {
                _decoder.Decode();
            }
            Console.WriteLine(_count);
            int symDiff = _decoder.GetDecodedValues().Count;
            Console.WriteLine(symDiff);

            //_decoder.GetDecodedValues().Order().ToList().ForEach(Console.WriteLine);
            return (double)symDiff / _count;
        }

        public HashSet<ulong> PredictSymmetricDifference()
        {
            if (!decoded)
            {
                Decode();
            }
            return _decoder.GetDecodedValues();
        }

        public void Decode()
        {
            _decoder.Decode();
            decoded = true;
        }

        public void AddValues(ulong[] values, int nItemsInBuffer)
        {
            var decodedValues = _decoder.GetDecodedValues();

            int counter = 0;
            for (int i = 0; i < nItemsInBuffer; i++)
            {
                var item = values[i];
                if (!decodedValues.Contains(item))
                {
                    values[counter] = item;
                    counter++;
                    decodedValues.Add(item);
                }

            }
            _encoder.Encode(values, counter);

        }

        public void ToggleValues(ulong[] values, int nItemsInBuffer)
        {
            _decoder.GetDecodedValues().ExceptWith(values.Take(nItemsInBuffer));
            _encoder.Encode(values, nItemsInBuffer);
        }

        public SymmetricDifferenceFinder.Decoders.Common.DecodingState DecodingState => _decoder.DecodingState;

    }
}
