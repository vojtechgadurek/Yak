using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.Mail;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using FlashHash.SchemesAndFamilies;

namespace Yak.Cache
{
    public record class StoredHashFunctionScheme<T>(T StoredHashFunction, string Type)
    {
        public StoredHashFunctionScheme(object StoredHashFunction) : this((T)StoredHashFunction, typeof(T).AssemblyQualifiedName!)
        {
        }
    }
    public static class HashFunctionCache
    {
        private static Dictionary<string, IHashFunctionScheme> _cache = new();
        private static Dictionary<string, Func<ulong, ulong>> _cacheCompiled = new();


        static readonly string _folder = "HashFunctions";


        static HashFunctionCache()
        {
            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }
        }

        public static void GenerateHashFunction(string fileName, ulong size, ulong offset, Type type)
        {

            var scheme = ((IHashFunctionFamily)Activator.CreateInstance(type)).GetScheme(size, offset);

            var storage = Activator.CreateInstance(typeof(StoredHashFunctionScheme<>).MakeGenericType(scheme.GetType()), scheme);
            File.WriteAllBytes(Path.Combine(_folder, fileName), JsonSerializer.SerializeToUtf8Bytes(
                storage, storage.GetType()
                ));

            Dictionary<string, IHashFunctionScheme> cache = _cache;

        }

        public static void LoadHashFunctionIntoCache(string name)
        {
            lock (_cache)
            {
                if (!_cache.ContainsKey(name))
                {
                    var path = Path.Combine(_folder, name);

                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException($"Hash function {name} not found");
                    }
                    {
                        Utf8JsonReader reader = new Utf8JsonReader(File.ReadAllBytes(path));
                        JsonDocument jsonDocument
                            = JsonDocument.ParseValue(ref reader);

                        //Console.WriteLine(jsonDocument.RootElement.GetRawText());
                        jsonDocument.RootElement.TryGetProperty("Type", out var outType);



                        jsonDocument.RootElement.TryGetProperty("StoredHashFunction", out var outFunction);



                        _cache[name] = (IHashFunctionScheme)outFunction.Deserialize(Type.GetType(outType.ToString()));
                    }
                }
            }
        }

        public static IHashFunctionScheme Get(string name)
        {
            // Console.WriteLine(name);
            if (!_cache.ContainsKey(name))
            {
                LoadHashFunctionIntoCache(name);
            }
            return _cache[name];
        }

        public static Func<ulong, ulong> GetCompiled(string name)
        {
            if (!_cacheCompiled.ContainsKey(name))
            {
                lock (_cacheCompiled)
                {
                    var scheme = Get(name);
                    if (!_cacheCompiled.ContainsKey(name))
                        _cacheCompiled[name] = scheme.Create().Compile();
                }

            }
            return _cacheCompiled[name];
        }
    }
}
