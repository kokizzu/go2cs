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

#nullable enable

namespace go;

public static partial class main_package
{
    [GeneratedCode("go2cs-gen", "0.1.4")]
    public partial struct Uncommon_uᴛ4
    {
        // Promoted Struct References
        private readonly ж<global::go.main_package.ChanType> ᏑʗChanType;

        // Promoted Struct Accessors
        public partial ref global::go.main_package.ChanType ChanType => ref ᏑʗChanType.val;

        // Promoted Struct Field Accessors
        public ref global::go.main_package.Type Type => ref ChanType.Type;
        public ref global::go.ж<global::go.main_package.Type> Elem => ref ChanType.Elem;
        public ref global::go.main_package.ΔChanDir Dir => ref ChanType.Dir;

        // Promoted Struct Field Accessor References
        public static ref global::go.main_package.Type ᏑType(ref Uncommon_uᴛ4 instance) => ref instance.ChanType.Type;
        public static ref global::go.ж<global::go.main_package.Type> ᏑElem(ref Uncommon_uᴛ4 instance) => ref instance.ChanType.Elem;
        public static ref global::go.main_package.ΔChanDir ᏑDir(ref Uncommon_uᴛ4 instance) => ref instance.ChanType.Dir;

        // Field References
        public static ref global::go.main_package.ChanType ᏑChanType(ref Uncommon_uᴛ4 instance) => ref instance.ChanType;
        public static ref global::go.main_package.UncommonType Ꮡu(ref Uncommon_uᴛ4 instance) => ref instance.u;
        
        // Constructors
        public Uncommon_uᴛ4(NilType _)
        {
            ᏑʗChanType = new ж<global::go.main_package.ChanType>(new global::go.main_package.ChanType(nil));
            this.u = default!;
        }

        public Uncommon_uᴛ4(global::go.main_package.ChanType ChanType = default!)
        {
            ᏑʗChanType = new ж<global::go.main_package.ChanType>(ChanType);
        }

        internal Uncommon_uᴛ4(global::go.main_package.ChanType ChanType = default!, global::go.main_package.UncommonType u = default!)
        {
            ᏑʗChanType = new ж<global::go.main_package.ChanType>(ChanType);
            this.u = u;
        }
        
        // Handle comparisons between struct 'Uncommon_uᴛ4' instances
        public bool Equals(Uncommon_uᴛ4 other) =>
            ChanType == other.ChanType &&
            u == other.u;
        
        public override bool Equals(object? obj) => obj is Uncommon_uᴛ4 other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            ChanType,
            u);
        
        public static bool operator ==(Uncommon_uᴛ4 left, Uncommon_uᴛ4 right) => left.Equals(right);
        
        public static bool operator !=(Uncommon_uᴛ4 left, Uncommon_uᴛ4 right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'Uncommon_uᴛ4'
        public static bool operator ==(Uncommon_uᴛ4 value, NilType nil) => value.Equals(default(Uncommon_uᴛ4));

        public static bool operator !=(Uncommon_uᴛ4 value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, Uncommon_uᴛ4 value) => value == nil;

        public static bool operator !=(NilType nil, Uncommon_uᴛ4 value) => value != nil;

        public static implicit operator Uncommon_uᴛ4(NilType nil) => default(Uncommon_uᴛ4);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            ChanType.ToString(),
            u.ToString()
        ]), "}");
    }

    // Promoted Struct Receivers
}
