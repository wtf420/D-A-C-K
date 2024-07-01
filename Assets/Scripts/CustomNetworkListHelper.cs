using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public static class CustomNetworkListHelper<T> where T : unmanaged, IEquatable<T>
{
    public static T GetItemFromNetworkList(T item, NetworkList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(item))
                return list[i];
        }
        return default;
    }

    public static List<T> ConvertToNormalList(NetworkList<T> list)
    {
        List<T> result = new List<T>();
        for (int i = 0; i < list.Count; i++)
        {
            result.Add(list[i]);
        }
        return result;
    }

    public static bool UpdateItemToList(T item, NetworkList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(item))
            {
                list[i] = item;
                return true;
            }
        }
        return false;
    }
}
