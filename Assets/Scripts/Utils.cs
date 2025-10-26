using System.Collections.Generic;
using UnityEngine;

namespace ProductsPlease
{
    public static class Utils
    {
        public static T GetRandom<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return default;
            }
            var index = Random.Range(0, list.Count);
            return list[index];
        }
    }
}