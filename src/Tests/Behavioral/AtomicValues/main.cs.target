namespace go;

using fmt = fmt_package;
using atomic = sync.atomic_package;
using sync;

partial class main_package {

[GoType] partial struct holder {
    internal atomic.Int64 count;
}

internal static ж<holder> ᏑgHolder = new(default(holder));
internal static ref holder gHolder => ref ᏑgHolder.Value;

internal static void Main() {
    ref var n = ref heap(new atomic.Int32(), out var Ꮡn);
    Ꮡn.Store(10);
    fmt.Println((@string)"add:"u8, Ꮡn.Add(5));
    fmt.Println((@string)"load:"u8, Ꮡn.Load());
    fmt.Println((@string)"swap:"u8, Ꮡn.Swap(100));
    fmt.Println((@string)"cas ok:"u8, Ꮡn.CompareAndSwap(100, 7));
    fmt.Println((@string)"cas no:"u8, Ꮡn.CompareAndSwap(100, 8));
    fmt.Println((@string)"final:"u8, Ꮡn.Load());
    ref var p = ref heap(new atomic.Pointer<nint>(), out var Ꮡp);
    fmt.Println((@string)"ptr nil:"u8, Ꮡp.Load() == nil);
    ref var a = ref heap<nint>(out var Ꮡa);
    a = 1;
    Ꮡp.Store(Ꮡa);
    fmt.Println((@string)"ptr load:"u8, Ꮡp.Load().Value);
    ref var b = ref heap<nint>(out var Ꮡb);
    b = 2;
    fmt.Println((@string)"ptr cas no:"u8, Ꮡp.CompareAndSwap(Ꮡb, Ꮡb));
    fmt.Println((@string)"ptr cas ok:"u8, Ꮡp.CompareAndSwap(Ꮡa, Ꮡb));
    fmt.Println((@string)"ptr final:"u8, Ꮡp.Load().Value);
    var old = Ꮡp.Swap(Ꮡa);
    fmt.Println((@string)"ptr swap:"u8, old.Value, Ꮡp.Load().Value);
    ᏑgHolder.of(holder.Ꮡcount).Store(42);
    ᏑgHolder.of(holder.Ꮡcount).Add(8);
    fmt.Println((@string)"global field:"u8, ᏑgHolder.of(holder.Ꮡcount).Load());
}

} // end main_package
