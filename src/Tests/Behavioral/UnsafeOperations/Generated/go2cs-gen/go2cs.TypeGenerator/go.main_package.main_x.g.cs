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
    internal partial struct main_x
    {
        // Promoted Struct References
        // -- main_x has no promoted structs

        // Field References
        internal static ref long Ꮡa(ref main_x instance) => ref instance.a;
        internal static ref bool Ꮡb(ref main_x instance) => ref instance.b;
        internal static ref global::go.@string Ꮡc(ref main_x instance) => ref instance.c;
        
        // Constructors
        public main_x(NilType _)
        {
            this.a = default!;
            this.b = default!;
            this.c = default!;
        }


        internal main_x(long a = default!, bool b = default!, global::go.@string c = default!)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        
        // Handle comparisons between struct 'main_x' instances
        public bool Equals(main_x other) =>
            a == other.a &&
            b == other.b &&
            c == other.c;
        
        public override bool Equals(object? obj) => obj is main_x other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            a,
            b,
            c);
        
        public static bool operator ==(main_x left, main_x right) => left.Equals(right);
        
        public static bool operator !=(main_x left, main_x right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'main_x'
        public static bool operator ==(main_x value, NilType nil) => value.Equals(default(main_x));

        public static bool operator !=(main_x value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, main_x value) => value == nil;

        public static bool operator !=(NilType nil, main_x value) => value != nil;

        public static implicit operator main_x(NilType nil) => default(main_x);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            a.ToString(),
            b.ToString(),
            c.ToString()
        ]), "}");
    }
}
