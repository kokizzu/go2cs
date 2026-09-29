namespace go;

using strings = strings_package;
using System.Runtime.CompilerServices;

partial class main_package {

[MethodImpl(MethodImplOptions.NoInlining)] internal static ж<strings.Reader> makeReader() {
    return strings.NewReader("hi"u8);
}

} // end main_package
