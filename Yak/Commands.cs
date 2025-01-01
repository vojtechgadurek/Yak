using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yak;
using System.IO;
using static IO;
using SymmetricDifferenceFinder.Decoders.HPW;
using SymmetricDifferenceFinder.Tables;
using Yak.Cache;
using SymmetricDifferenceFinder.Improvements;
using SymmetricDifferenceFinder.Improvements.Oracles;
using SymmetricDifferenceFinder.Improvements.StringFactories;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace Yak
{
    static class Commands
    {
        static Dictionary<string, Command> _dic = new Dictionary<string, Command>();

        static Dictionary<string, string> _variables = new Dictionary<string, string>();


        public static void RunCommand(string[] parts)
        {
            string name = parts[0];
            string[] args = parts.Skip(1).Select(x => x.StartsWith("$") ? _variables[x.Substring(1)] : x).ToArray();
            if (!_dic.ContainsKey(name))
                throw new Exception($"Command {name} not found");
            _dic[name].Action(args);
        }

        static Commands()
        {
            List<Command> commands = [
                new Command(
                    "load-sketch",
                    [("SketchFile", typeof(string)), ("SketchNewFile", typeof(string)), ("VariableName", typeof(string))],
                    (string[] args) => {
                        if (args[0] == args[1]) throw new Exception("SketchFile and SketchNewFile must be different");
                        var sketch = IO.Sketch.OpenSketch(args[0], args[1]);
                        Variables<IO.Sketch>.Set(args[2], sketch);
                    },
                    "Load a sketch from a file Args1 to a variable Args2"),

                new Command("sym-diff",
                    [("VariableName1", typeof(string)), ("VariableName2", typeof(string))],
                    (string[] args) => {
                        var sketch1 = Variables<IO.Sketch>.Values[args[0]];
                        var sketch2 = Variables<IO.Sketch>.Values[args[1]];
                        sketch1.Table.SymmetricDifference(sketch2.Table);
                    },
                    "SymmetricDifference of two sketches Args1 and Args2, this operation is destructive to the first sketch. One can always load new copy."),

                new Command("dump-sketch",
                    [("SketchName", typeof(string))],
                    (string[] args) => {
                        Variables<IO.Sketch>.Values[args[1]].Dump();
                    },
                    "Dump a sketch by sketch name"),

                new Command("recover-sketch-basic",
                    [("VariableName", typeof(string)), ("SketchName" , typeof(string))],
                    (string[] args) => {
                        var sketch = Variables<IO.Sketch>.Values[args[1]];
                        HPWDecoderFactory<XORTable> decoderFactory = new HPWDecoderFactory<XORTable>(
                        sketch.Config.HashFunctionSchemesFileNames.Select(x => HashFunctionCache.Get(x).Create()).ToList());
                        var decoder = decoderFactory.Create(sketch.Table);
                        decoder.Decode();
                        Variables<HashSet<ulong>>.Set(args[0], decoder.GetDecodedValues());},
                    "Recover a sketch by sketch name"),

                new Command("create-decoder",
                   [("VariableName", typeof(string)), ("SketchName", typeof(string))],
                   (string[] args) => {
                          var sketch = Variables<IO.Sketch>.Values[args[1]];
                          HPWDecoderFactory<XORTable> decoderFactory = new HPWDecoderFactory<XORTable>(
                          sketch.Config.HashFunctionSchemesFileNames.Select(x => HashFunctionCache.Get(x).Create()).ToList());
                          var decoder = decoderFactory.Create(sketch.Table);
                          Variables<HPWDecoder<XORTable>>.Set(args[0], decoder);
                     },
                   "Create a decoder by sketch name"
                   ),
                new Command("create-oracle-decoder",
                [("VariableName", typeof(string)), ("SketchName", typeof(string)), ("DecoderStepsDuringDecodingRound", typeof(int)) ,("DecoderStepsInitial", typeof(int))],
                (string[] args) => {
                    var sketch = Variables<IO.Sketch>.Values[args[1]];
                    var hfs =  sketch.Config.HashFunctionSchemesFileNames.Select(x => HashFunctionCache.Get(x).Create()).ToList();
                    HPWDecoderFactory<XORTable> decoderFactory = new HPWDecoderFactory<XORTable>(hfs
                    );

                    var decoder = new Massager<KMerStringFactory, CanonicalOrder>(decoderFactory.Create(sketch.Table), hfs.Select(x => x.Compile()));
                    decoder.NStepsDecoder = int.Parse(args[2]);
                    decoder.NStepsDecoderInitial = int.Parse(args[3]);
                    Variables<HPWWithOracle>.Set(args[0], new HPWWithOracle(decoder));
                },
                "Create a decoder by sketch name"
                ),

                new Command("dan-recovery-pipeline",
                [("OutputName", typeof(string)), ("Oracle-Decoder", typeof(string)), ("Input-HashSet", typeof(HashSet<ulong>)), ("MaxDistance", typeof(int)), ("MinDistance", typeof(int)), ("Rounds", typeof(int))],
                (args) =>{
                    Variables<HashSet<ulong>>.Set(args[0],
                        new DanRecoveryPipeline(
                            Variables<HPWWithOracle>.Values[args[1]], Variables<HashSet<ulong>>.Values[args[2]], int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]))
                        .PredictSymmetricDifference());
                },
                "Apply dan recovery pipeline to a given oracle-decoder and input hashset."

                ),

                new Command("dump-hashset",


                [("HashSetName", typeof(string)), ("FileName", typeof(string))],
                (args) => {
                    File.WriteAllText(args[1],JsonSerializer.Serialize(   Variables<HashSet<ulong>>.Values[args[0]]));
                },

                "Dump a hashset to a file."
                ),

                new Command(
                    "get-hashset-size",
                    [("HashSetName", typeof(string))],
                    (args) => {
                        Console.WriteLine(Variables<HashSet<ulong>>.Values[args[0]].Count);
                    },
                    "Get the size of a hashset."
                ),



                new Command("decode-decoder",
                    [("HashSetName", typeof(HashSet<ulong>)), ("DecoderName", typeof(string))],
                    (string[] args) => {
                        Variables<HPWDecoder<XORTable>>.Values[args[0]].Decode();
                        Variables<HashSet<ulong>>.Set(args[1], Variables<HPWDecoder<XORTable>>.Values[args[0]].GetDecodedValues());
                    },
                    "Decode a decoder by decoder name"){
                },

                new Command("decode-decoder-oracle",
                    [("HashSetName", typeof(HashSet<ulong>)), ("DecoderName", typeof(string))],
                    (string[] args) => {
                        Variables<HPWWithOracle>.Values[args[1]].Decode();
                        Variables<HashSet<ulong>>.Set(args[0], Variables<HPWWithOracle>.Values[args[1]].PredictSymmetricDifference());
                    },
                    "Decode a decoder by decoder name"){
                },

                new Command("decoder-oracle-get-state",
                    [("HashSetName", typeof(HashSet<ulong>)), ("DecoderName", typeof(string))],
                    (string[] args) => {
                        Console.WriteLine(Variables<HPWWithOracle>.Values[args[1]].DecodingState);
                    },
                    "Decode a decoder by decoder name"){
                },

                new Command("decoder-get-state",
                    [("HashSetName", typeof(HashSet<ulong>)), ("DecoderName", typeof(string))],
                    (string[] args) => {
                        Console.WriteLine(Variables<HPWDecoder<XORTable>>.Values[args[1]].DecodingState);
                    },
                    "Decode a decoder by decoder name"){
                },


                new Command("set-variable",
                [("VariableName", typeof(string)), ("Value", typeof(string))],
                    (string[] args) => {
                        if(_variables.ContainsKey(args[0]))
                            _variables[args[0]] = args[1];
                        else
                            _variables.Add(args[0], args[1]);
                    },
                    "Set a variable by variable name"
                )
            ];

            _dic = commands.ToDictionary(x => x.Name);
        }

        public static class Variables<T>
        {
            static public Dictionary<string, T> Values = new Dictionary<string, T>();

            public static void Set(string name, T value)
            {
                if (Values.ContainsKey(name))
                    Values[name] = value;
                else
                    Values.Add(name, value);
            }
        }

        public record class Command(string Name, (string, Type)[] Types, Action<string[]> Action, string? description);
    }
}
