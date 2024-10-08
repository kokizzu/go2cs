// Code generated by "stringer -type=SymKind"; DO NOT EDIT.

// package objabi -- go2cs converted at 2022 March 13 05:43:23 UTC
// import "cmd/internal/objabi" ==> using objabi = go.cmd.@internal.objabi_package
// Original source: C:\Program Files\Go\src\cmd\internal\objabi\symkind_string.go
namespace go.cmd.@internal;

using strconv = strconv_package;

public static partial class objabi_package {

private static void _() { 
    // An "invalid array index" compiler error signifies that the constant values have changed.
    // Re-run the stringer command to generate them again.
    var x = default;
    _ = x[Sxxx - 0];
    _ = x[STEXT - 1];
    _ = x[SRODATA - 2];
    _ = x[SNOPTRDATA - 3];
    _ = x[SDATA - 4];
    _ = x[SBSS - 5];
    _ = x[SNOPTRBSS - 6];
    _ = x[STLSBSS - 7];
    _ = x[SDWARFCUINFO - 8];
    _ = x[SDWARFCONST - 9];
    _ = x[SDWARFFCN - 10];
    _ = x[SDWARFABSFCN - 11];
    _ = x[SDWARFTYPE - 12];
    _ = x[SDWARFVAR - 13];
    _ = x[SDWARFRANGE - 14];
    _ = x[SDWARFLOC - 15];
    _ = x[SDWARFLINES - 16];
    _ = x[SABIALIAS - 17];
    _ = x[SLIBFUZZER_EXTRA_COUNTER - 18];
}

private static readonly @string _SymKind_name = "SxxxSTEXTSRODATASNOPTRDATASDATASBSSSNOPTRBSSSTLSBSSSDWARFCUINFOSDWARFCONSTSDWARFFCNSDWARFABSFCNSDWARFTYPESDWARFVARSDWARFRANGESDWARFLOCSDWARFLINESSABIALIASSLIBFUZZER_EXTRA_COUNTER";



private static array<byte> _SymKind_index = new array<byte>(new byte[] { 0, 4, 9, 16, 26, 31, 35, 44, 51, 63, 74, 83, 95, 105, 114, 125, 134, 145, 154, 178 });

public static @string String(this SymKind i) {
    if (i >= SymKind(len(_SymKind_index) - 1)) {
        return "SymKind(" + strconv.FormatInt(int64(i), 10) + ")";
    }
    return _SymKind_name[(int)_SymKind_index[i]..(int)_SymKind_index[i + 1]];
}

} // end objabi_package
