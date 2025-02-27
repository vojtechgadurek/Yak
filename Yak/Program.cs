﻿using Yak;
using RedaFasta;
using System.Diagnostics;
using System.Text.Json;
using FlashHash.SchemesAndFamilies;
using Microsoft.CodeAnalysis;
using System.Security.AccessControl;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Diagnostics.Tracing.Parsers.ClrPrivate;
using SymmetricDifferenceFinder.Tables;
using System.Globalization;
using KMerUtils.DNAGraph;
using SymmetricDifferenceFinder.Encoders;
using Yak.Cache;
using LittleSharp.Literals;
using Microsoft.CodeAnalysis.Operations;
using FlashHash;
using SymmetricDifferenceFinder.Improvements.StringFactories;
using SymmetricDifferenceFinder.Improvements.Oracles;
using System.Text;
using System.Threading.Tasks.Sources;
using SymmetricDifferenceFinder.Decoders.HPW;
using Utilities;
using LittleSharp;
using System.Runtime.InteropServices;



public partial class Program
{
    static void RunCommand(string[] args)
    {
        string command = args[0];
        switch (command)
        {
            case "get-all-hash-functions":
                HashFunctionProvider.GetAllHashingFunctionFamilies().ToList().ForEach(x => Console.WriteLine((x.Name, x.AssemblyQualifiedName.ToString())));
                break;
            case "create-hash":
                HashFunctionCache.
                    GenerateHashFunction(args[1], ulong.Parse(args[3]), ulong.Parse(args[4]), HashFunctionProvider.GetFamilyByName(args[2])
                ??
                    throw new ArgumentException($"Provided command {args[2]} is not correct hash function command"));
                break;
            case "load-hash":
                Console.WriteLine(HashFunctionCache.Get(args[1]));

                break;
            case "compare-exact-hashset":
                Console.WriteLine(IO.GetExactDifference(args[1], args[2]));
                break;
            case "encode-to-sketch":
                IO.EncodingConfigTemplate.Open(args[1]).Encode(args[2], args[3], int.Parse(args[4]), args.Length > 5 ? int.Parse(args[5]) : 2048);
                break;

            case "create-template":
                File.WriteAllText(args[1], JsonSerializer.Serialize(new IO.EncodingConfigTemplate([
                    new IO.SketchConfig(args.AsSpan().Slice(6, 3).ToArray(), int.Parse(args[2]), null),
                    new IO.SketchConfig(args.AsSpan().Slice(9, 3).ToArray(),int.Parse(args[3]),  JsonSerializer.Serialize( new Syncmers.SyncMerFilter(int.Parse(args[5]), int.Parse(args[4]), args[9])))
                    ])));

                JsonSerializer.Deserialize<IO.EncodingConfigTemplate>(File.ReadAllText(args[1]));
                break;

            case "create-basic-template":
                File.WriteAllText(args[1], JsonSerializer.Serialize(new IO.EncodingConfigTemplate([
                    new IO.SketchConfig(["tab1", "tab2", "tab3"], 1000, null),
                    new IO.SketchConfig(["tab1", "tab2", "tab3"], 1000, JsonSerializer.Serialize( new Syncmers.SyncMerFilter(31, 20, "tab4")))
                    ])));

                JsonSerializer.Deserialize<IO.EncodingConfigTemplate>(File.ReadAllText(args[1]));
                break;

            case "symmetric-difference":
                string firstFile = args[1];
                string secondFile = args[2];
                string resultFile = args[3];

                IO.Sketch sketch1 = IO.Sketch.OpenSketch(args[1], args[3]);
                IO.Sketch sketch2 = IO.Sketch.OpenSketch(args[2], "resultSecond");

                sketch1.Table.SymmetricDifference(sketch2.Table);
                sketch1.Dump();
                break;

            case "create-test-data":
                int kMerLength = int.Parse(args[1]);
                int testFileLength = int.Parse(args[2]);
                int seed = int.Parse(args[3]);

                string fileName = args[4];

                Random random = new Random(seed);
                string header = $">test k={kMerLength} l={testFileLength}";
                string dnaString = IO.CreateRandomDNAString(testFileLength, random);
                File.WriteAllLines(fileName, [header, dnaString]);


                break;

            case "recover-sketch":

                string sketchname = args[1];
                string result = args[2];

                IO.Sketch sketch = IO.Sketch.OpenSketch(sketchname, "test");


                HPWDecoderFactory<XORTable> decoderFactory = new HPWDecoderFactory<XORTable>(
                    sketch.Config.HashFunctionSchemesFileNames.Select(x => HashFunctionCache.Get(x).Create()).ToList()
                    );

                var decoder = decoderFactory.Create(sketch.Table);
                decoder.Decode();
                File.WriteAllBytes(result, JsonSerializer.SerializeToUtf8Bytes(decoder.GetDecodedValues()));
                Console.WriteLine(decoder.GetDecodedValues().Count);
                break;

            case "interactive":

                while (true)
                {
                    var cm = Console.ReadLine();
                    if (cm.StartsWith("exit")) break;
                    try
                    {
                        Commands.RunCommand(cm.Split(' '));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace);
                        Console.WriteLine(e.Message);
                    }
                }
                break;
            case "execute-file":
                string fN = args[1];
                string[] lines = File.ReadAllLines(fN);
                foreach (var line in lines)
                {
                    Commands.RunCommand(line.Split(' '));
                }
                break;

            case "execute-file-args":
                string fN2 = args[1];
                for (int i = 1; i < args.Length - 1; i++)
                {
                    Commands.RunCommand(["set-variable", i.ToString(), args[1 + i]]);
                }
                string[] lines2 = File.ReadAllLines(fN2);
                foreach (var line in lines2)
                {
                    Commands.RunCommand(line.Split(' '));
                }
                break;

            case "execute-files":
                foreach (var fN1 in args.Skip(1))
                {
                    string[] lines1 = File.ReadAllLines(fN1);
                    foreach (var line in lines1)
                    {
                        Commands.RunCommand(line.Split(' '));
                    }
                }
                break;

            default:
                throw new ArgumentException($"Unknown command {command}");
        }
    }
    static void Main(string[] args)
    {
        string command = args[0];

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        RunCommand(args);
        stopwatch.Stop();
        Console.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds} ms");
    }
}
public static class IO
{

    public static int GetExactDifference(string firstFileName, string secondFileName)
    {
        {
            var fastaFileReader1Config = FastaFile.Open(new StreamReader(firstFileName));
            var fastaFileReader2Config = FastaFile.Open(new StreamReader(secondFileName));

            var fastaFileReader1 = new FastaFileReader(fastaFileReader1Config.kMerSize, fastaFileReader1Config.nCharsInFile, fastaFileReader1Config.textReader);
            var fastaFileReader2 = new FastaFileReader(fastaFileReader2Config.kMerSize, fastaFileReader2Config.nCharsInFile, fastaFileReader2Config.textReader);


            var symmetricDifferenceComparer = new HashSetPredictor();
            CompareTwoFiles(fastaFileReader1, fastaFileReader2, symmetricDifferenceComparer);

            fastaFileReader1.Dispose();
            fastaFileReader2.Dispose();

            return symmetricDifferenceComparer.PredictSymmetricDifference().Count;
        }
    }
    public static void CompareTwoFiles(IFastaFileReader firstSet, IFastaFileReader secondSet, ISymmetricDifferenceComparer comparer, int nTasks = 8)
    {
        FromFileToComparer(comparer.AddValuesFromFirstSet, firstSet);
        FromFileToComparer(comparer.AddValuesFromSecondSet, secondSet);
    }


    public static string CreateRandomDNAString(int fileLength, Random random)
    {
        char[] chars = ['A', 'C', 'G', 'T'];
        StringBuilder stringBuilder = new StringBuilder(fileLength);
        for (int i = 0; i < fileLength; i++)
        {
            stringBuilder.Append(chars[random.Next(4)]);
        }
        return stringBuilder.ToString();
    }

    public static void FromFileToComparer(Action<ulong[], int> addToComparer, IFastaFileReader fastaFileReader)
    {
        while (true)
        {
            FastaFileReader.Buffer? buffer;
            lock (fastaFileReader)
            {
                buffer = fastaFileReader.BorrowBuffer();

            }
            if (buffer == null)
            {
                break;
            }

            //Print array


            addToComparer(buffer.Data, buffer.Size);
            lock (fastaFileReader)
            {
                fastaFileReader.RecycleBuffer(buffer);
            }
        }
    }

    public record class Pipeline(Action<ulong[], int, Action<ulong[], int>>? filter, Action<ulong[], int> Toggle)
    {
        public void ToggleValues(ulong[] values, int numberOfItems)
        {
            if (filter is not null)
            {
                filter(values, numberOfItems, Toggle);
            }
            else
            {
                Toggle(values, numberOfItems);
            }
        }
    }

    public static void ParallelEncode(Action<ulong[], int>[] encoders, FastaFileReader fastaFileReader)
    {
        Task.WaitAll(encoders.Select(encoders => Task.Run(() => FromFileToComparer(encoders, fastaFileReader))).ToArray());
    }


    public record SketchConfig(string[] HashFunctionSchemesFileNames, int TableSize, string? Filter)
    {
        EncoderFactory<XORTable>? _encoderFactory = null;
        EncoderFactory<XORTable> CreateEncoderFactory()
        {
            if (_encoderFactory is null)
            {
                _encoderFactory = new EncoderFactory<XORTable>(new EncoderConfiguration<XORTable>(HashFunctionSchemesFileNames.Select(HashFunctionCache.Get), TableSize), (size) => new XORTable(size));
            }
            return _encoderFactory;
        }

        static Action<ulong[], int, Action<ulong[], int>> GetFilter(string filter)
        {

            var syncmerFilter = JsonSerializer.Deserialize<Syncmers.SyncMerFilter>(filter);
            return syncmerFilter.GetFilter();


            throw new ArgumentException($"Unknown filter {filter}");
        }

        public (XORTable table, Action<ulong[], int> encoder) CreateEncoder()
        {
            var encoder = CreateEncoderFactory().Create();
            if (Filter is null)
            {
                return (encoder.GetTable(), encoder.Encode);
            }

            return (encoder.GetTable(), (ulong[] t, int i) => GetFilter(Filter)(t, i, encoder.Encode));
        }
    };
    public record Sketch(string SketchName, XORTable Table, SketchConfig Config)
    {

        Encoder<XORTable>? _encoder = null;
        public async Task Dump()
        {
            Directory.CreateDirectory(SketchName);
            await DumpXORTable(Table, Path.Combine(SketchName, "sketch.b"));
            File.WriteAllText(Path.Combine(SketchName, "config.txt"),
                JsonSerializer.Serialize(Config));
        }

        public static Sketch OpenSketch(string sketchName, string newSketchName)
        {
            if (sketchName == newSketchName) throw new ArgumentException("Sketch name and new sketch name should be different");

            var table = ReadXORTable(Path.Combine(sketchName, "sketch.b"));
            var config = JsonSerializer.Deserialize<SketchConfig>(File.ReadAllText(Path.Combine(sketchName, "config.txt")));
            return new Sketch(newSketchName, table, config);
        }

        static XORTable ReadXORTable(string fileName)
        {

            var table = MemoryMarshal.Cast<byte, ulong>(File.ReadAllBytes(fileName)).ToArray();
            var xorTable = new XORTable(table);
            return xorTable;
        }

        public async Task DumpXORTable(XORTable xORTable, string fileName)
        {
            await File.WriteAllBytesAsync(fileName, MemoryMarshal.AsBytes(xORTable.GetUnderlyingTable().AsSpan()).ToArray());
        }
    };

    public static Action<ulong[], int> Connector(Action<ulong[], int> first, Action<ulong[], int> second)
    {
        return (t, i) => { first(t, i); second(t, i); };
    }

    public record class EncodingConfiguration(string fileName, int KMerLength, long NKmers, string[] sketchFoldersNames);

    public record class RecoveryStep(string SketchName)
    {

    }
    public record class RecoveryOrder(RecoveryStep[] RecoverySteps)
    {

    }

    public record class EncodingConfigTemplate(SketchConfig[] SketchConfigurations)
    {
        public static EncodingConfigTemplate Open(string templateName)
        {
            return
                JsonSerializer.Deserialize<EncodingConfigTemplate>(File.ReadAllText(templateName))
                ??
                throw new ArgumentException($"{templateName} template does not exits");
        }

        public void Encode(string fileName, string sketchName, int nInstances, int bufferSize = 2048)
        {
            if (Directory.Exists(sketchName))
            {
                throw new ArgumentException($"Name {sketchName} already exits");
            }

            Directory.CreateDirectory(sketchName);

            var fastaFileReaderConfig = FastaFile.Open(new StreamReader(fileName));
            var fastaFileReader = new FastaFileReader(fastaFileReaderConfig.kMerSize, fastaFileReaderConfig.nCharsInFile, fastaFileReaderConfig.textReader, bufferSize: bufferSize, nInstances * 2);



            XORTable[][] tables = new XORTable[nInstances][];
            Action<ulong[], int>[] actions = new Action<ulong[], int>[nInstances];
            for (int i = 0; i < nInstances; i++)
            {
                var e = SketchConfigurations.Select(x => x.CreateEncoder()).ToArray();
                tables[i] = e.Select(x => x.table).ToArray();
                actions[i] = e.Select(x => x.encoder).Aggregate(Connector);
            }

            //every 1s return the number of items left in reader

            bool end = true;
            var task = Task.Run(() =>
            {
                while (end)
                {
                    Console.WriteLine($"Workleft {(double)fastaFileReader.CharsLeft / fastaFileReaderConfig.nCharsInFile}");
                    Task.Delay(1000).Wait();
                }
            });

            ParallelEncode(actions, fastaFileReader);

            end = false;


            XORTable[] answers = new XORTable[SketchConfigurations.Length];
            for (int i = 0; i < SketchConfigurations.Length; i++)
            {
                XORTable t = tables[0][i];
                for (int j = 1; j < nInstances; j++)
                {
                    t = t.SymmetricDifference(tables[j][i]);
                }
                answers[i] = t;
            }

            void PrintArray(ulong[] array)
            {
                foreach (var item in array)
                {
                    Console.WriteLine(item);
                }
            }


            List<Task> tasks = new List<Task>();
            for (int i = 0; i < SketchConfigurations.Length; i++)
            {
                tasks.Add(new Sketch(Path.Combine(sketchName, i.ToString()), answers[i], SketchConfigurations[i]).Dump());
                Console.WriteLine($"Successfully writen Sketch {i}");
            }
            Task.WaitAll(tasks.ToArray());

            File.WriteAllText(
                "config.txt",
                JsonSerializer.Serialize(
                    new EncodingConfiguration(
                        sketchName,
                        fastaFileReaderConfig.kMerSize,
                        fastaFileReaderConfig.nCharsInFile,
                        Enumerable.Range(
                            0,
                            SketchConfigurations.Length
                        ).Select(x => x.ToString()).ToArray()
                        )
                    )
                );
        }
    }
}

