namespace Tarmi.Imaging.Algorithms.Helpers;

public static class MemoryHelpers
{
    public static unsafe void CopyMemory(void* srcPtr, void* dstPtr, int count) =>
        new Span<byte>(srcPtr, count).CopyTo(new Span<byte>(dstPtr, count));
}
