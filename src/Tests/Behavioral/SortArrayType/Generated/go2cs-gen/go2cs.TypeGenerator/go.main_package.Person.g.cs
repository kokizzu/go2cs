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
    public partial struct Person
    {
        // Promoted Struct References
        // -- Person has no promoted structs

        // Field References
        internal static ref global::go.@string ᏑName(ref Person instance) => ref instance.Name;
        internal static ref nint ᏑAge(ref Person instance) => ref instance.Age;
        internal static ref float ᏑShoeSize(ref Person instance) => ref instance.ShoeSize;
        
        // Constructors
        public Person(NilType _)
        {
            this.Name = default!;
            this.Age = default!;
            this.ShoeSize = default!;
        }

        public Person(global::go.@string Name = default!, nint Age = default!, float ShoeSize = default!)
        {
            this.Name = Name;
            this.Age = Age;
            this.ShoeSize = ShoeSize;
        }
        
        // Handle comparisons between struct 'Person' instances
        public bool Equals(Person other) =>
            Name == other.Name &&
            Age == other.Age &&
            ShoeSize == other.ShoeSize;
        
        public override bool Equals(object? obj) => obj is Person other && Equals(other);
        
        public override int GetHashCode() => runtime.HashCode.Combine(
            Name,
            Age,
            ShoeSize);
        
        public static bool operator ==(Person left, Person right) => left.Equals(right);
        
        public static bool operator !=(Person left, Person right) => !(left == right);

        // Handle comparisons between 'nil' and struct 'Person'
        public static bool operator ==(Person value, NilType nil) => value.Equals(default(Person));

        public static bool operator !=(Person value, NilType nil) => !(value == nil);

        public static bool operator ==(NilType nil, Person value) => value == nil;

        public static bool operator !=(NilType nil, Person value) => value != nil;

        public static implicit operator Person(NilType nil) => default(Person);

        public override string ToString() => string.Concat("{", string.Join(" ",
        [
            Name.ToString(),
            Age.ToString(),
            ShoeSize.ToString()
        ]), "}");
    }
}
