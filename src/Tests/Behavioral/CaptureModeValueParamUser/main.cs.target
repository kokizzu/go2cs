namespace go;

using fmt = fmt_package;
using CaptureModeValueParamLib = CaptureModeValueParamLib_package;

partial class main_package {

internal static (@string, @string) render(CaptureModeValueParamLib.Config cfgʗp, @string label) {
    ref var cfg = ref heap(cfgʗp, out var Ꮡcfg);

    cfg.Indent = cfg.Indent + 1;
    var (s1, _) = Ꮡcfg.Fprint(label);
    var (s2, _) = Ꮡcfg.Fprint(label + "2"u8);
    return (s1 + "," + s2, cfg.Trace());
}

internal static void Main() {
    var cfg = new CaptureModeValueParamLib.Config(Indent: 3);
    var (@out, trace) = render(cfg, "go"u8);
    fmt.Println((@string)"rendered:"u8, @out);
    fmt.Println((@string)"trace:"u8, trace);
    fmt.Println((@string)"caller Indent unchanged:"u8, cfg.Indent);
}

} // end main_package
