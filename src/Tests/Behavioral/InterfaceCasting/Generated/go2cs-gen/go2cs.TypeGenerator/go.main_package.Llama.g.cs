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
    public partial struct Llama
    {
        // Promoted Struct References
        // -- Llama has no promoted structs

        // Field References
        // -- Llama has no defined fields
        
        // Constructors
        public Llama(NilType _)
        {
        }

        
        // Handle comparisons between struct 'Llama' instances
        public bool Equals(Llama other) =>
            true /* empty */;
        
        public override bool Equals(object? obj) => obj is Llama other && Equals(other);
        
        public override int GetHashCode() => base.GetHashCode();
        
        public static bool operator ==(Llama left, Llama right) => left.Equals(right);
        
        public static bool operator !=(Llama left, Llama right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'Llama'
        public static bool operator ==(Llama value, NilType nil) => value.Equals(default(Llama));

        public static bool operator !=(Llama value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, Llama value) => value == nil;

        public static bool operator !=(NilType nil, Llama value) => value != nil;

        public static implicit operator Llama(NilType nil) => default(Llama);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            ""
        ]), "}");
    }
}
