using System.Runtime.CompilerServices;

namespace go;

// Fixtures for GolibTests.RuntimePanicCheckTests: classes named exactly as the converter names package
// runtime's internal-test bridge and package runtime_test, because golib's panicCheck1 marker reads a
// frame's declaring type by full name.

// Package runtime's own _test.go files, as a -tests build declares them.
internal static class runtime_internal_test_package
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool Raise(out string frameName) => GolibTests.RuntimePanicCheckTests.s_raised(out frameName);

    internal static class Nested
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool Raise(out string frameName) => GolibTests.RuntimePanicCheckTests.s_raised(out frameName);
    }
}

// Package runtime_test: Go names its functions runtime_test.*, which panicCheck1's prefix excludes.
internal static class runtime_test_package
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool Raise(out string frameName) => GolibTests.RuntimePanicCheckTests.s_raised(out frameName);
}
