namespace go;

using fmt = fmt_package;
using reflect = reflect_package;
using iolike = EmbeddedInterfaceWitness.iolike_package;
using EmbeddedInterfaceWitness;

partial class main_package {

[GoType] partial struct wrapper {
    public EmbeddedInterfaceWitness.iolike_package.Reader Reader;
    internal @string prefix;
}

internal static @string Write(this wrapper w, @string s) {
    return w.prefix + s;
}

[GoType] partial struct plain {
    internal @string tag;
}

internal static @string Read(this plain p) {
    return "read:"u8 + p.tag;
}

internal static @string Write(this plain p, @string s) {
    return "p:"u8 + s;
}

[GoType] partial struct holder {
    public iolike.Reader Reader;
    internal @string prefix;
}

internal static @string Write(this holder h, @string s) {
    return h.prefix + s;
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object readWriterYesˢ = (@string)"ReadWriter: yes"u8;
private static readonly object readWriterNoˢ = (@string)"ReadWriter: no"u8;
private static readonly object readerYesˢ = (@string)"Reader: yes"u8;
private static readonly object readerNoˢ = (@string)"Reader: no"u8;
private static readonly object numMethodˢ = (@string)"NumMethod:"u8;

internal static void check(@string label, any value) {
    {
        var (rw, ok) = value._<iolike.ReadWriter>(ᐧ); if (ok){
            fmt.Println(label, readWriterYesˢ, rw.Read(), rw.Write("x"u8));
        } else {
            fmt.Println(label, readWriterNoˢ);
        }
    }
    {
        var (r, ok) = value._<iolike.Reader>(ᐧ); if (ok){
            fmt.Println(label, readerYesˢ, r.Read());
        } else {
            fmt.Println(label, readerNoˢ);
        }
    }
    fmt.Println(label, numMethodˢ, reflect.TypeOf(value).NumMethod());
}

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly @string wrapperˢ = "wrapper"u8;
private static readonly @string plainˢ = "plain"u8;
private static readonly @string holderˢ = "holder"u8;

internal static void Main() {
    check(wrapperˢ, new wrapper(Reader: new iolike.Base(Tag: "base"u8), prefix: "w:"u8));
    check(plainˢ, new plain(tag: "p"u8));
    check(holderˢ, new holder(Reader: new iolike.Base(Tag: "held"u8), prefix: "h:"u8));
}

} // end main_package
