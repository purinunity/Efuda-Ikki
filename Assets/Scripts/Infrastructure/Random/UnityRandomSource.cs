using EfudaIkki.Core;
using UnityEngine;

public sealed class UnityRandomSource : IRandomSource
{
    public int Range(int minInclusive, int maxExclusive)
    {
        return Random.Range(minInclusive, maxExclusive);
    }

    public float Value01()
    {
        return Random.value;
    }
}
