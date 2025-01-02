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
    public class HPWWithOracle
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

        public HPWWithOracle(Massager<KMerStringFactory, CanonicalOrder> massager, Encoder<XORTable> encoder)
        {
            _decoder = massager;
            _XORTable = massager.HPWDecoder.Sketch;
            _encoder = encoder;

        }

        public void SimpleDecode()
        {
            _decoder.HPWDecoder.Decode();
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
            _decoder.GetDecodedValues().SymmetricExceptWith(values.Take(nItemsInBuffer));
            _encoder.Encode(values, nItemsInBuffer);
        }

        public void Encode(ulong[] values, int nItemsInBuffer)
        {
            _decoder.GetDecodedValues().SymmetricExceptWith(values.Take(nItemsInBuffer));
            _encoder.Encode(values, nItemsInBuffer);
        }


        public SymmetricDifferenceFinder.Decoders.Common.DecodingState DecodingState => _decoder.DecodingState;

    }
}
