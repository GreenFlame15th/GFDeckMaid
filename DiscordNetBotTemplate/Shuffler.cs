using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

static class Shuffler
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static T GetRand<T>(this IList<T> list)
    {
        return list[ThreadSafeRandom.ThisThreadsRandom.Next(list.Count)];
    }

    public static int GetRandPosition<T>(this IList<T> list)
    {
        return ThreadSafeRandom.ThisThreadsRandom.Next(list.Count);
    }
}

public static class ThreadSafeRandom
{
    [ThreadStatic] private static Random _local;

    public static Random ThisThreadsRandom
    {
        get { return _local ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)); }
    }
}
