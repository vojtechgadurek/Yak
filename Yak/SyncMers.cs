using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Yak.Cache;

namespace Yak
{
    public static class Syncmers
    {
        public record SyncMerFilter(int KMerLength, int MaskLength, string HashFunctionName)
        {
            public Action<ulong[], int, Action<ulong[], int>> GetFilter()
            {
                return (values, nItems, action) => Syncmers.FilterSyncMers(values, nItems, KMerLength, MaskLength, HashFunctionCache.GetCompiled(HashFunctionName), action);
            }

            public static SyncMerFilter Open(string filterName)
            {
                return JsonSerializer.Deserialize<Syncmers.SyncMerFilter>(File.ReadAllText(filterName))!;
            }

            public void Dump(string filterName)
            {
                File.WriteAllText(filterName, JsonSerializer.Serialize(this));
            }
        }
        public static void FilterSyncMers(
            ulong[] values, int nItems, int kMerLength, int maskLength, Func<ulong, ulong> order, Action<ulong[], int> action)
        {
            var filtered = ArrayPool<ulong>.Shared.Rent(nItems);
            int counter = 0;
            for (int i = 0; i < nItems; i++)
            {
                var value = values[i];
                if (IsSyncMer(value, order, maskLength, kMerLength))
                {
                    counter++;
                    filtered[i] = value;
                }
            }
            action(filtered, counter);
            ArrayPool<ulong>.Shared.Return(filtered);
        }
        public static bool IsSyncMer(ulong value, Func<ulong, ulong> order, int maskLength, int kMerLength)
        {
            ulong mask = (1UL << (maskLength * 2)) - 1;

            ulong suffixvalue = order(value & mask)
                ;

            ulong prefixvalue = order(value >>> (kMerLength * 2 - maskLength * 2));
            ulong maxValue = Math.Max(suffixvalue, prefixvalue);

            for (int i = 1; i < kMerLength - maskLength - 1; i++)
            {
                value >>>= 2;
                ulong nextValue = order(mask & value);
                if (nextValue > maxValue) return false;
            }
            return true;
        }
    }
}
