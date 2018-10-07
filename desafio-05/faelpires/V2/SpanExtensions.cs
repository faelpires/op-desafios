using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOf<T>(this Span<byte> span, byte value, int start) where T : IEquatable<T>
    {
        for (int i = start; i < span.Length; i++)
        {
            if(span[i] == value)
                return i;
        }

        return -1;
    }
}