[assembly: go.GoPositionMap("abi_amd64.go", "abi_amd64.cs", "")]

namespace go;

partial class main_package {

public static UntypedInt IntArgRegs => 9;
public static UntypedInt FloatArgRegs => 15;
public static UntypedInt EffectiveFloatRegSize => 8;

} // end main_package
