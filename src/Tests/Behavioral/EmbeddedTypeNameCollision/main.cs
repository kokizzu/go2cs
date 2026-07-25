namespace go;

using fmt = fmt_package;
using inner = EmbeddedTypeNameCollision.inner_package;
using EmbeddedTypeNameCollision;

partial class main_package {

[GoType] partial struct Buffer {
    public partial ref EmbeddedTypeNameCollision.inner_package.Buffer ΔBuffer { get; }
    public @string Tag;
}

public static @string Renamed(Buffer b) {
    return fmt.Sprintf("%s/%s/%d"u8, b.Tag, b.ΔBuffer.Data, b.ΔBuffer.N);
}

internal static void Main() {
    ref var b = ref heap<Buffer>(out var Ꮡb);
    b = new Buffer(ΔBuffer: inner.NewBuffer("alpha"u8, 3), Tag: "tagged"u8);
    fmt.Println(b.ΔBuffer.Data, b.ΔBuffer.N);
    fmt.Println(b.Data, b.N);
    fmt.Println(b.ΔBuffer.Describe());
    fmt.Println(Renamed(b));
    var p = Ꮡb;
    p.of(Buffer.ᏑΔBuffer).Bump();
    p.of(Buffer.ᏑΔBuffer).Bump();
    fmt.Println((~p).ΔBuffer.N, (~p).N, (~p).ΔBuffer.Describe());
    p.Value.ΔBuffer.Data = "beta"u8;
    p.of(Buffer.ᏑΔBuffer).Append("!"u8);
    fmt.Println((~p).Data, (~p).ΔBuffer.N, (~p).ΔBuffer.Describe());
    var z = @new<Buffer>();
    z.of(Buffer.ᏑΔBuffer).Append("gamma"u8);
    z.Value.Tag = "fresh"u8;
    fmt.Println((~z).ΔBuffer.Describe(), (~z).ΔBuffer.N, Renamed(z.Value));
}

} // end main_package
