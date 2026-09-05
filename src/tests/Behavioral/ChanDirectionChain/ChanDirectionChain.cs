namespace go;

using fmt = fmt_package;
using reflect = reflect_package;

partial class main_package {

// Hoisted @string literals (single allocation; Go keeps these in RODATA)
private static readonly object chanChanIntˢ = (@string)"1 chan chan int    :"u8;
private static readonly object chanChanIntˢ2 = (@string)"2 chan<- chan int  :"u8;
private static readonly object chanChanIntˢ3 = (@string)"3 chan (<-chan int):"u8;
private static readonly object chanChanIntˢ4 = (@string)"4 chan chan<- int  :"u8;
private static readonly object chanChanIntˢ5 = (@string)"5 <-chan chan int  :"u8;
private static readonly object elemOfChanChanIntˢ = (@string)"elem of chan (<-chan int):"u8;
private static readonly object elemDirˢ = (@string)"elem dir                 :"u8;
private static readonly object identityChanOfBothIntˢ = (@string)"identity ChanOf(Both,int) == TypeOf((chan int)(nil)):"u8;
private static readonly object identityChanOfBothChanˢ = (@string)"identity ChanOf(Both,chan int) == TypeOf((chan chan int)(nil)):"u8;

internal static void Main() {
    var intT = reflect.TypeOf((nint)(0));
    var both = reflect.ChanOf(reflect.BothDir, intT);
    var recv = reflect.ChanOf(reflect.RecvDir, intT);
    var send = reflect.ChanOf(reflect.SendDir, intT);
    fmt.Println(chanChanIntˢ, reflect.ChanOf(reflect.BothDir, both).String());
    fmt.Println(chanChanIntˢ2, reflect.ChanOf(reflect.SendDir, both).String());
    fmt.Println(chanChanIntˢ3, reflect.ChanOf(reflect.BothDir, recv).String());
    fmt.Println(chanChanIntˢ4, reflect.ChanOf(reflect.BothDir, send).String());
    fmt.Println(chanChanIntˢ5, reflect.ChanOf(reflect.RecvDir, both).String());
    fmt.Println(elemOfChanChanIntˢ, reflect.ChanOf(reflect.BothDir, recv).Elem().String());
    fmt.Println(elemDirˢ, reflect.ChanOf(reflect.BothDir, recv).Elem().ChanDir());
    channel<nint> nilChan = default!;
    fmt.Println(identityChanOfBothIntˢ,
        AreEqual(reflect.ChanOf(reflect.BothDir, intT), reflect.TypeOf(nilChan)));
    channel<channel<nint>> nilNested = default!;
    fmt.Println(identityChanOfBothChanˢ,
        AreEqual(reflect.ChanOf(reflect.BothDir, both), reflect.TypeOf(nilNested)));
}

} // end main_package
