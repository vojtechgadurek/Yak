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
    public class DanRecoveryPipeline : IJaccardIndexPredictor, ISymmetricDifferencePredictor
    {
        readonly HPWWithOracle _HPWWithOracle;
        readonly HPWWithOracle _syncMer;
        readonly Func<ulong, ulong> _syncmerOrder;


        bool decoded = false;
        const int kMerLength = 31;
        //ToDo: make it generic -> there are some issues in the underlying library
        //however it would take some time to fix them

        public DanRecoveryPipeline(int difference)
        {
            _HPWWithOracle = new HPWWithOracle(difference, 2, nRecoverySteps: 0);

            _syncMer = new HPWWithOracle(difference, 3, nRecoverySteps: 0);
            _syncmerOrder = new TabulationFamily().GetScheme(ulong.MaxValue, 0).Create().Compile();
        }


        public void AddValuesToSyncMer(ulong[] values, int numberOfItems, Action<ulong[], int> addDelegate)
        {
            var x = new ulong[numberOfItems];

            int counter = 0;
            for (int i = 0; i < numberOfItems; i++)
            {
                if (Syncmers.IsSyncMer(values[i], _syncmerOrder, 32, kMerLength))
                {
                    x[counter] = values[i];
                    counter++;
                }
            }
            addDelegate(x, counter);
        }

        public void AddValuesFromFirstSet(ulong[] values, int numberOfItems)
        {
            _HPWWithOracle.AddValuesFromFirstSet(values, numberOfItems);
            AddValuesToSyncMer(values, numberOfItems, _syncMer.AddValuesFromFirstSet);
        }

        public void AddValuesFromSecondSet(ulong[] values, int numberOfItems)
        {
            _HPWWithOracle.AddValuesFromSecondSet(values, numberOfItems);
            AddValuesToSyncMer(values, numberOfItems, _syncMer.AddValuesFromSecondSet);
        }

        public double PredictJaccardIndex()
        {
            throw new NotImplementedException();
        }


        public void Decode()
        {
            if (decoded == false)
            {
                _syncMer.Decode();
                var decoded = _syncMer.PredictSymmetricDifference().ToArray();
                Console.WriteLine($"Syncmers decode:{decoded.Length}, sum: {_syncMer.GetCount()}");
                _HPWWithOracle.AddValues(decoded, decoded.Length);
            }
            decoded = true;

            int minDistance = 1;
            int maxDistance = 20;

            for (int i = 0; i < 15; i++)
            {
                _HPWWithOracle.Decode();
                if (_HPWWithOracle.DecodingState == SymmetricDifferenceFinder.Decoders.Common.DecodingState.Success)
                {
                    break;
                }

                if (i == 1)
                {
                    maxDistance = 10;
                }

                if (i == 10)
                {
                    maxDistance = 10;
                }

                if (i % 1 == 0)
                {
                    Console.WriteLine($"Recovery {_HPWWithOracle.PredictSymmetricDifference().Count}");

                    //foreach (var item in _HPWWithOracle.PredictSymmetricDifference())
                    //{
                    //    Console.WriteLine(Convert.ToString((long)item, 2));
                    //}

                    var newlyDecoded =
                        KMerUtils.DNAGraph.Recover.RecoverGraphCanonicalV3(
                            _HPWWithOracle.PredictSymmetricDifference()
                            //RemoveHeaders
                            .Select(x => x >>> 2)
                            .ToArray(), kMerLength, 20, 1, false
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

                    //var x = KMerUtils.DNAGraph.Recover.FindPaths(
                    //    _HPWWithOracle.PredictSymmetricDifference().ToArray(), kMerLength, 100

                    //    ).ToList();

                    //var ran = new Random();
                    //var endpoints = x.Select(x => (
                    //        KMerUtils.KMer.Utils.GetLeftNeighbors(x.First(), kMerLength)[ran.Next(0, 4)].ToCanonical(kMerLength),
                    //        KMerUtils.KMer.Utils.GetRightNeighbors(x.Last(), kMerLength)[ran.Next(0, 4)].ToCanonical(kMerLength))).Select(
                    //            x => ((x.Item1 << 2) + 3, (x.Item2 << 2) + 3)

                    //        ).SelectMany(x => new ulong[] { x.Item1, x.Item2 }).ToArray();
                    //_HPWWithOracle.ToggleValues(endpoints, endpoints.Length);
                    //_HPWWithOracle.NStepsRecovery = 0;
                    //_HPWWithOracle.Decode();
                    //_HPWWithOracle.ToggleValues(endpoints, endpoints.Length);
                    //_HPWWithOracle.Decode();
                    //_HPWWithOracle.NStepsRecovery = 1;



                    //foreach (var item in newlyDecoded)
                    //{
                    //    Console.WriteLine(Convert.ToString((long)item, 2));
                    //}

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
