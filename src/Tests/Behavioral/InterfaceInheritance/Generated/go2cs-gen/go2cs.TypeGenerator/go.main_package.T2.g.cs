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
    public partial struct T2
    {
        // Promoted Struct References
        // -- T2 has no promoted structs

        // Field References
        internal static ref global::go.@string Ꮡname(ref T2 instance) => ref instance.name;
        
        // Constructors
        public T2(NilType _)
        {
            this.name = default!;
        }


        internal T2(global::go.@string name = default!)
        {
            this.name = name;
        }
        
        // Handle comparisons between struct 'T2' instances
        public bool Equals(T2 other) =>
            name == other.name;
        
        public override bool Equals(object? obj) => obj is T2 other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            name);
        
        public static bool operator ==(T2 left, T2 right) => left.Equals(right);
        
        public static bool operator !=(T2 left, T2 right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'T2'
        public static bool operator ==(T2 value, NilType nil) => value.Equals(default(T2));

        public static bool operator !=(T2 value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, T2 value) => value == nil;

        public static bool operator !=(NilType nil, T2 value) => value != nil;

        public static implicit operator T2(NilType nil) => default(T2);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            name.ToString()
        ]), "}");
    }
}
