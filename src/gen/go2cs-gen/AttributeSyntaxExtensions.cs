﻿//******************************************************************************************************
//  AttributeSyntaxExtensions.cs - Gbtc
//
//  Copyright © 2024, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  09/16/2024 - J.Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace go2cs;

public static class AttributeSyntaxExtensions
{
    public static (string name, string value)[] GetArgumentValues(this AttributeSyntax attribute)
    {
        SeparatedSyntaxList<AttributeArgumentSyntax> arguments = attribute.ArgumentList?.Arguments ?? default;

        return arguments.Select(argument => (
            name: argument.NameEquals?.Name.Identifier.Text ?? string.Empty,
            value: argument.Expression.NormalizeWhitespace().ToFullString()
        )).ToArray();
    }

    public static (ITypeSymbol? typeArg1, ITypeSymbol? typeArg2) Get2GenericTypeArguments(this AttributeSyntax attributeSyntax, GeneratorSyntaxContext context)
    {
        // Check if the attribute type is generic
        if (attributeSyntax.Name is not GenericNameSyntax genericName)
            return (null, null);

        // Get the type arguments
        SeparatedSyntaxList<TypeSyntax> typeArguments = genericName.TypeArgumentList.Arguments;

        if (typeArguments.Count != 2)
            return (null, null);

        // Get semantic information for each type argument
        ITypeSymbol? typeArg1 = context.SemanticModel.GetTypeInfo(typeArguments[0]).Type;
        ITypeSymbol? typeArg2 = context.SemanticModel.GetTypeInfo(typeArguments[1]).Type;

        return (typeArg1, typeArg2);
    }
}
