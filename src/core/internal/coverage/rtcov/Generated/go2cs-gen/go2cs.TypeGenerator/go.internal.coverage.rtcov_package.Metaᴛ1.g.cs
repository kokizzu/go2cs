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
    public partial struct Metaᴛ1
    {
        // Promoted Struct References
        // -- Metaᴛ1 has no promoted structs

        // Field References
        public static ref global::go.slice<global::go.@internal.coverage.rtcov_package.CovMetaBlob> ᏑList(ref Metaᴛ1 instance) => ref instance.List;
        internal static ref global::go.map<nint, nint> ᏑPkgMap(ref Metaᴛ1 instance) => ref instance.PkgMap;
        internal static ref bool ᏑhardCodedListNeedsUpdating(ref Metaᴛ1 instance) => ref instance.hardCodedListNeedsUpdating;
        
        // Constructors
        public Metaᴛ1(NilType _)
        {
            this.List = default!;
            this.PkgMap = default!;
            this.hardCodedListNeedsUpdating = default!;
        }

        public Metaᴛ1(global::go.slice<global::go.@internal.coverage.rtcov_package.CovMetaBlob> List = default!, global::go.map<nint, nint> PkgMap = default!)
        {
            this.List = List;
            this.PkgMap = PkgMap;
        }

        internal Metaᴛ1(global::go.slice<global::go.@internal.coverage.rtcov_package.CovMetaBlob> List = default!, global::go.map<nint, nint> PkgMap = default!, bool hardCodedListNeedsUpdating = default!)
        {
            this.List = List;
            this.PkgMap = PkgMap;
            this.hardCodedListNeedsUpdating = hardCodedListNeedsUpdating;
        }
        
        // Handle comparisons between struct 'Metaᴛ1' instances
        public bool Equals(Metaᴛ1 other) =>
            List == other.List &&
            PkgMap == other.PkgMap &&
            hardCodedListNeedsUpdating == other.hardCodedListNeedsUpdating;
        
        public override bool Equals(object? obj) => obj is Metaᴛ1 other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            List,
            PkgMap,
            hardCodedListNeedsUpdating);
        
        public static bool operator ==(Metaᴛ1 left, Metaᴛ1 right) => left.Equals(right);
        
        public static bool operator !=(Metaᴛ1 left, Metaᴛ1 right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'Metaᴛ1'
        public static bool operator ==(Metaᴛ1 value, NilType nil) => value.Equals(default(Metaᴛ1));

        public static bool operator !=(Metaᴛ1 value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, Metaᴛ1 value) => value == nil;

        public static bool operator !=(NilType nil, Metaᴛ1 value) => value != nil;

        public static implicit operator Metaᴛ1(NilType nil) => default(Metaᴛ1);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            List.ToString(),
            PkgMap.ToString(),
            hardCodedListNeedsUpdating.ToString()
        ]), "}");
    }
}
