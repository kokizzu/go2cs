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

namespace go.@internal.coverage;

public static partial class rtcov_package
{
    [GeneratedCode("go2cs-gen", "0.1.4")]
    public partial struct CovMetaBlob
    {
        // Promoted Struct References
        // -- CovMetaBlob has no promoted structs

        // Field References
        internal static ref global::go.ж<byte> ᏑP(ref CovMetaBlob instance) => ref instance.P;
        internal static ref uint ᏑLen(ref CovMetaBlob instance) => ref instance.Len;
        internal static ref global::go.array<byte> ᏑHash(ref CovMetaBlob instance) => ref instance.Hash;
        internal static ref global::go.@string ᏑPkgPath(ref CovMetaBlob instance) => ref instance.PkgPath;
        internal static ref nint ᏑPkgID(ref CovMetaBlob instance) => ref instance.PkgID;
        internal static ref byte ᏑCounterMode(ref CovMetaBlob instance) => ref instance.CounterMode;
        internal static ref byte ᏑCounterGranularity(ref CovMetaBlob instance) => ref instance.CounterGranularity;
        
        // Constructors
        public CovMetaBlob(NilType _)
        {
            this.P = default!;
            this.Len = default!;
            this.Hash = default!;
            this.PkgPath = default!;
            this.PkgID = default!;
            this.CounterMode = default!;
            this.CounterGranularity = default!;
        }

        public CovMetaBlob(global::go.ж<byte> P = default!, uint Len = default!, global::go.array<byte> Hash = default!, global::go.@string PkgPath = default!, nint PkgID = default!, byte CounterMode = default!, byte CounterGranularity = default!)
        {
            this.P = P;
            this.Len = Len;
            this.Hash = Hash;
            this.PkgPath = PkgPath;
            this.PkgID = PkgID;
            this.CounterMode = CounterMode;
            this.CounterGranularity = CounterGranularity;
        }
        
        // Handle comparisons between struct 'CovMetaBlob' instances
        public bool Equals(CovMetaBlob other) =>
            P == other.P &&
            Len == other.Len &&
            Hash == other.Hash &&
            PkgPath == other.PkgPath &&
            PkgID == other.PkgID &&
            CounterMode == other.CounterMode &&
            CounterGranularity == other.CounterGranularity;
        
        public override bool Equals(object? obj) => obj is CovMetaBlob other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            P,
            Len,
            Hash,
            PkgPath,
            PkgID,
            CounterMode,
            CounterGranularity);
        
        public static bool operator ==(CovMetaBlob left, CovMetaBlob right) => left.Equals(right);
        
        public static bool operator !=(CovMetaBlob left, CovMetaBlob right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'CovMetaBlob'
        public static bool operator ==(CovMetaBlob value, NilType nil) => value.Equals(default(CovMetaBlob));

        public static bool operator !=(CovMetaBlob value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, CovMetaBlob value) => value == nil;

        public static bool operator !=(NilType nil, CovMetaBlob value) => value != nil;

        public static implicit operator CovMetaBlob(NilType nil) => default(CovMetaBlob);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            P?.ToString() ?? "<nil>",
            Len.ToString(),
            Hash.ToString(),
            PkgPath.ToString(),
            PkgID.ToString(),
            CounterMode.ToString(),
            CounterGranularity.ToString()
        ]), "}");
    }
}
