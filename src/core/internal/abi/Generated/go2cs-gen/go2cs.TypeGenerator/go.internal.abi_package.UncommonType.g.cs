﻿//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
// </auto-generated>
//---------------------------------------------------------

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using go;

#nullable enable

namespace go.@internal;

public static partial class abi_package
{
    [GeneratedCode("go2cs-gen", "0.1.4")]
    public partial struct UncommonType
    {
        // Promoted Struct References
        // -- UncommonType has no promoted structs

        // Field References
        public static ref global::go.@internal.abi_package.NameOff ᏑPkgPath(ref UncommonType instance) => ref instance.PkgPath;
        internal static ref ushort ᏑMcount(ref UncommonType instance) => ref instance.Mcount;
        internal static ref ushort ᏑXcount(ref UncommonType instance) => ref instance.Xcount;
        internal static ref uint ᏑMoff(ref UncommonType instance) => ref instance.Moff;
        internal static ref uint Ꮡ_(ref UncommonType instance) => ref instance._;
        
        // Constructors
        public UncommonType(NilType _)
        {
            this.PkgPath = default!;
            this.Mcount = default!;
            this.Xcount = default!;
            this.Moff = default!;
            this._ = default!;
        }

        public UncommonType(global::go.@internal.abi_package.NameOff PkgPath = default!, ushort Mcount = default!, ushort Xcount = default!, uint Moff = default!, uint _ = default!)
        {
            this.PkgPath = PkgPath;
            this.Mcount = Mcount;
            this.Xcount = Xcount;
            this.Moff = Moff;
            this._ = _;
        }
        
        // Handle comparisons between struct 'UncommonType' instances
        public bool Equals(UncommonType other) =>
            PkgPath == other.PkgPath &&
            Mcount == other.Mcount &&
            Xcount == other.Xcount &&
            Moff == other.Moff &&
            _ == other._;
        
        public override bool Equals(object? obj) => obj is UncommonType other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            PkgPath,
            Mcount,
            Xcount,
            Moff,
            _);
        
        public static bool operator ==(UncommonType left, UncommonType right) => left.Equals(right);
        
        public static bool operator !=(UncommonType left, UncommonType right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'UncommonType'
        public static bool operator ==(UncommonType value, NilType nil) => value.Equals(default(UncommonType));

        public static bool operator !=(UncommonType value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, UncommonType value) => value == nil;

        public static bool operator !=(NilType nil, UncommonType value) => value != nil;

        public static implicit operator UncommonType(NilType nil) => default(UncommonType);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            PkgPath.ToString(),
            Mcount.ToString(),
            Xcount.ToString(),
            Moff.ToString(),
            _.ToString()
        ]), "}");
    }
}
