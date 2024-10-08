// Copyright 2016 The Go Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

// package sys -- go2cs converted at 2022 March 13 05:43:19 UTC
// import "cmd/internal/sys" ==> using sys = go.cmd.@internal.sys_package
// Original source: C:\Program Files\Go\src\cmd\internal\sys\arch.go
namespace go.cmd.@internal;

using binary = encoding.binary_package;

public static partial class sys_package {

// ArchFamily represents a family of one or more related architectures.
// For example, ppc64 and ppc64le are both members of the PPC64 family.
public partial struct ArchFamily { // : byte
}

public static readonly ArchFamily NoArch = iota;
public static readonly var AMD64 = 0;
public static readonly var ARM = 1;
public static readonly var ARM64 = 2;
public static readonly var I386 = 3;
public static readonly var MIPS = 4;
public static readonly var MIPS64 = 5;
public static readonly var PPC64 = 6;
public static readonly var RISCV64 = 7;
public static readonly var S390X = 8;
public static readonly var Wasm = 9;

// Arch represents an individual architecture.
public partial struct Arch {
    public @string Name;
    public ArchFamily Family;
    public binary.ByteOrder ByteOrder; // PtrSize is the size in bytes of pointers and the
// predeclared "int", "uint", and "uintptr" types.
    public nint PtrSize; // RegSize is the size in bytes of general purpose registers.
    public nint RegSize; // MinLC is the minimum length of an instruction code.
    public nint MinLC; // Alignment is maximum alignment required by the architecture
// for any (compiler-generated) load or store instruction.
// Loads or stores smaller than Alignment must be naturally aligned.
// Loads or stores larger than Alignment need only be Alignment-aligned.
    public sbyte Alignment;
}

// InFamily reports whether a is a member of any of the specified
// architecture families.
private static bool InFamily(this ptr<Arch> _addr_a, params ArchFamily[] xs) {
    xs = xs.Clone();
    ref Arch a = ref _addr_a.val;

    foreach (var (_, x) in xs) {
        if (a.Family == x) {
            return true;
        }
    }    return false;
}

public static ptr<Arch> Arch386 = addr(new Arch(Name:"386",Family:I386,ByteOrder:binary.LittleEndian,PtrSize:4,RegSize:4,MinLC:1,Alignment:1,));

public static ptr<Arch> ArchAMD64 = addr(new Arch(Name:"amd64",Family:AMD64,ByteOrder:binary.LittleEndian,PtrSize:8,RegSize:8,MinLC:1,Alignment:1,));

public static ptr<Arch> ArchARM = addr(new Arch(Name:"arm",Family:ARM,ByteOrder:binary.LittleEndian,PtrSize:4,RegSize:4,MinLC:4,Alignment:4,));

public static ptr<Arch> ArchARM64 = addr(new Arch(Name:"arm64",Family:ARM64,ByteOrder:binary.LittleEndian,PtrSize:8,RegSize:8,MinLC:4,Alignment:1,));

public static ptr<Arch> ArchMIPS = addr(new Arch(Name:"mips",Family:MIPS,ByteOrder:binary.BigEndian,PtrSize:4,RegSize:4,MinLC:4,Alignment:4,));

public static ptr<Arch> ArchMIPSLE = addr(new Arch(Name:"mipsle",Family:MIPS,ByteOrder:binary.LittleEndian,PtrSize:4,RegSize:4,MinLC:4,Alignment:4,));

public static ptr<Arch> ArchMIPS64 = addr(new Arch(Name:"mips64",Family:MIPS64,ByteOrder:binary.BigEndian,PtrSize:8,RegSize:8,MinLC:4,Alignment:8,));

public static ptr<Arch> ArchMIPS64LE = addr(new Arch(Name:"mips64le",Family:MIPS64,ByteOrder:binary.LittleEndian,PtrSize:8,RegSize:8,MinLC:4,Alignment:8,));

public static ptr<Arch> ArchPPC64 = addr(new Arch(Name:"ppc64",Family:PPC64,ByteOrder:binary.BigEndian,PtrSize:8,RegSize:8,MinLC:4,Alignment:1,));

public static ptr<Arch> ArchPPC64LE = addr(new Arch(Name:"ppc64le",Family:PPC64,ByteOrder:binary.LittleEndian,PtrSize:8,RegSize:8,MinLC:4,Alignment:1,));

public static ptr<Arch> ArchRISCV64 = addr(new Arch(Name:"riscv64",Family:RISCV64,ByteOrder:binary.LittleEndian,PtrSize:8,RegSize:8,MinLC:4,Alignment:8,));

public static ptr<Arch> ArchS390X = addr(new Arch(Name:"s390x",Family:S390X,ByteOrder:binary.BigEndian,PtrSize:8,RegSize:8,MinLC:2,Alignment:1,));

public static ptr<Arch> ArchWasm = addr(new Arch(Name:"wasm",Family:Wasm,ByteOrder:binary.LittleEndian,PtrSize:8,RegSize:8,MinLC:1,Alignment:1,));

public static array<ptr<Arch>> Archs = new array<ptr<Arch>>(new ptr<Arch>[] { Arch386, ArchAMD64, ArchARM, ArchARM64, ArchMIPS, ArchMIPSLE, ArchMIPS64, ArchMIPS64LE, ArchPPC64, ArchPPC64LE, ArchRISCV64, ArchS390X, ArchWasm });

} // end sys_package
