using System;
using System.Collections.Generic;
using System.GAC;
using System.Text;

namespace Yam.Core
{
    internal static class GacUtil
    {
        public static HashSet<string> GetGacSet()
        {
            return new HashSet<string>(GetGac());
        }

        private static IEnumerable<string> GetGac()
        {
            var assemblyEnum = AssemblyCache.CreateGACEnum();
            IAssemblyName name;
            while (assemblyEnum.GetNextAssembly(IntPtr.Zero, out name, 0) == 0)
            {
                yield return GetName(name);
            }
        }

        private static string GetName(IAssemblyName name)
        {
            uint bufferSize = 255;
            var stringBuilder = new StringBuilder((int)bufferSize);
            name.GetName(ref bufferSize, stringBuilder);
            return stringBuilder.ToString().Trim();
        }
    }
}