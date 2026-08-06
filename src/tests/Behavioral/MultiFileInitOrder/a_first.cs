namespace go;

partial class main_package {

internal static slice<@string> order;

[GoInit] internal static void init() {
    order = append(order, "a_first#1"u8);
}

[GoInit] internal static void initΔ1() {
    order = append(order, "a_first#2"u8);
}

} // end main_package
