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
using System.Threading;

#nullable enable

namespace go;

public static partial class main_package
{
    [GeneratedCode("go2cs-gen", "0.1.4")]
    public static (nint n, go.error err) Read(this ж<go.main_package.Buffer> Ꮡb, go.slice<byte> p)
    {
        ref var b = ref Ꮡb.val;
        return b.Read(p);
    }
}
