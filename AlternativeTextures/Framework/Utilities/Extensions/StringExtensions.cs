using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

using StardewValley;

using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Utilities.Extensions;
internal static class StringExtensions
{
    /// <summary>
    /// Gets the name of an Object (non-bigcraftable) from the index.
    /// Returns an empty string if not found.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal static string GetObjectNameFromID(this int id)
        => Game1.objectInformation.TryGetValue(id, out var data) ? data.GetNthChunk('/', Object.objectInfoNameIndex).ToString() : string.Empty;

    /// <summary>
    /// Faster replacement for str.Split()[index];.
    /// </summary>
    /// <param name="str">String to search in.</param>
    /// <param name="deliminator">deliminator to use.</param>
    /// <param name="index">index of the chunk to get.</param>
    /// <returns>a readonlyspan char with the chunk, or an empty readonlyspan for failure.</returns>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<char> GetNthChunk(this string str, char deliminator, int index = 0)
        => str.GetNthChunk(new[] { deliminator }, index);

    /// <summary>
    /// Faster replacement for str.Split()[index];.
    /// </summary>
    /// <param name="str">String to search in.</param>
    /// <param name="deliminators">deliminators to use.</param>
    /// <param name="index">index of the chunk to get.</param>
    /// <returns>a readonlyspan char with the chunk, or an empty readonlyspan for failure.</returns>
    /// <remarks>Inspired by the lovely Wren.</remarks>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<char> GetNthChunk(this string str, char[] deliminators, int index = 0)
    {
        int start = 0;
        int ind = 0;
        while (index-- >= 0)
        {
            ind = str.IndexOfAny(deliminators, start);
            if (ind == -1)
            {
                // since we've previously decremented index, check against -1;
                // this means we're done.
                if (index == -1)
                {
                    return str.AsSpan()[start..];
                }

                // else, we've run out of entries
                // and return an empty span to mark as failure.
                return ReadOnlySpan<char>.Empty;
            }

            if (index > -1)
            {
                start = ind + 1;
            }
        }
        return str.AsSpan()[start..ind];
    }
}
