using KMerUtils;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlashHash.SchemesAndFamilies;
using KMerUtils.KMer;

namespace Yak
{
    public class DanRecoveryPipeline
    {
        readonly HPWWithOracle _HPWWithOracle;
        readonly HPWWithOracle _syncMer;
        readonly HashSet<ulong> _startingKmers;
        int _minDistance = 1;
        int _maxDistance = 20;
        int _rounds = 1;
        bool _verbose;

        bool decoded = false;
        const int kMerLength = 31;
        //ToDo: make it generic -> there are some issues in the underlying library
        //however it would take some time to fix them

        public DanRecoveryPipeline(HPWWithOracle mainDecoder, HashSet<ulong> startingKmers, int minDistance, int maxDistance, int rounds, bool verbose = true)
        {
            _HPWWithOracle = mainDecoder;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _rounds = rounds;
            _startingKmers = startingKmers;
            _verbose = verbose;
        }


        public void Decode()
        {
            if (_verbose) Console.WriteLine("Starting decoding");
            Console.WriteLine($"Starting set size {_startingKmers.Count}");
            Console.WriteLine($"_minDistance {_minDistance}, _maxDistance{_maxDistance}, _rounds {_rounds}");
            Console.WriteLine($"NSteps {_HPWWithOracle.NStepsDecoder}, NStepsRecovery {_HPWWithOracle.NStepsRecovery}");

            if (decoded == false)
            {
                decoded = true;
                var newlyDecoded =
                        KMerUtils.DNAGraph.Recover.RecoverGraphCanonicalV3(
                            _startingKmers
                            //RemoveHeaders
                            .Select(x => x >>> 2)
                            .ToArray(), kMerLength, _maxDistance, _minDistance, false
                            );
                for (int index = 0; index < newlyDecoded.Length; index++)
                {
                    //AddHeader
                    newlyDecoded[index] = (newlyDecoded[index] << 2) | 0b11;
                }

                //We should not forget that some of the values are already in the set
                //And we do not want to lose them
                _HPWWithOracle.AddValues(newlyDecoded, newlyDecoded.Length);
            }
            for (int i = 0; i < _rounds; i++)
            {
                _HPWWithOracle.Decode();
                if (_HPWWithOracle.DecodingState == SymmetricDifferenceFinder.Decoders.Common.DecodingState.Success)
                {
                    break;
                }

                if (i % 1 == 0)
                {
                    Console.WriteLine($"Recovery {_HPWWithOracle.PredictSymmetricDifference().Count}");

                    var newlyDecoded =
                        KMerUtils.DNAGraph.Recover.RecoverGraphCanonicalV3(
                            _HPWWithOracle.PredictSymmetricDifference()
                            //RemoveHeaders
                            .Select(x => x >>> 2)
                            .ToArray(), kMerLength, _maxDistance, _minDistance, false
                            );

                    Console.WriteLine($"Graph decoded {newlyDecoded.Count()}");
                    Console.WriteLine($"Graph decoded {newlyDecoded.ToHashSet().Count()}");



                    for (int index = 0; index < newlyDecoded.Length; index++)
                    {
                        //AddHeader
                        newlyDecoded[index] = (newlyDecoded[index] << 2) | 0b11;
                    }

                    //We should not forget that some of the values are already in the set
                    //And we do not want to lose them
                    _HPWWithOracle.AddValues(newlyDecoded, newlyDecoded.Length);
                    Console.WriteLine($"Recovery After {_HPWWithOracle.PredictSymmetricDifference().Count}");
                    _HPWWithOracle.Decode();

                }

            }

            Console.WriteLine("TIMEOUT");

        }

        public HashSet<ulong> PredictSymmetricDifference()
        {
            if (!decoded) Decode();
            return _HPWWithOracle.PredictSymmetricDifference();
        }
    }
}
