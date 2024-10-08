//---------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool. Changes to this
//     file may cause incorrect behavior and will be lost
//     if the code is regenerated.
//
//     Generated on 2022 March 13 06:43:44 UTC
// </auto-generated>
//---------------------------------------------------------
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using color = go.image.color_package;
using bits = go.math.bits_package;
using strconv = go.strconv_package;

#nullable enable

namespace go
{
    public static partial class image_package
    {
        [GeneratedCode("go2cs", "0.1.2.0")]
        public partial struct Rectangle
        {
            // Constructors
            public Rectangle(NilType _)
            {
                this.Min = default;
                this.Max = default;
            }

            public Rectangle(Point Min = default, Point Max = default)
            {
                this.Min = Min;
                this.Max = Max;
            }

            // Enable comparisons between nil and Rectangle struct
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Rectangle value, NilType nil) => value.Equals(default(Rectangle));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Rectangle value, NilType nil) => !(value == nil);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(NilType nil, Rectangle value) => value == nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(NilType nil, Rectangle value) => value != nil;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Rectangle(NilType nil) => default(Rectangle);
        }

        [GeneratedCode("go2cs", "0.1.2.0")]
        public static Rectangle Rectangle_cast(dynamic value)
        {
            return new Rectangle(value.Min, value.Max);
        }
    }
}