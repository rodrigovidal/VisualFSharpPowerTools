﻿module FSharpVSPowerTools.Core.SourceCodeClassifier

open System
open System.Collections.Generic
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharpVSPowerTools

type Category =
    | ReferenceType
    | ValueType
    | PatternCase
    | TypeParameter
    | Other

let internal getCategory (symbolUse: FSharpSymbolUse) =
    let symbol = symbolUse.Symbol

    match symbol with
    | :? FSharpGenericParameter
    | :? FSharpStaticParameter -> 
        TypeParameter
    | :? FSharpUnionCase
    | :? FSharpField 
    | :? FSharpActivePatternCase -> 
        PatternCase

    | :? FSharpEntity as e ->
        debug "%A (type: %s)" e (e.GetType().Name)
        if e.IsClass || e.IsDelegate || e.IsFSharpAbbreviation || e.IsFSharpExceptionDeclaration
           || e.IsFSharpRecord || e.IsFSharpUnion || e.IsInterface || e.IsMeasure || e.IsProvided
           || e.IsProvidedAndErased || e.IsProvidedAndGenerated then
            ReferenceType
        elif e.IsEnum || e.IsValueType then
            ValueType
        else Other
    
    | :? FSharpMemberFunctionOrValue as mfov ->
        debug "%A (type: %s)" mfov (mfov.GetType().Name)
        if not symbolUse.IsDefinition && mfov.IsImplicitConstructor then 
            ReferenceType
        else
            Other
    
    | _ ->
        debug "Unknown symbol: %A (type: %s)" symbol (symbol.GetType().Name)
        Other

let getTypeLocations (allSymbolsUses: FSharpSymbolUse[]) =
    allSymbolsUses
    |> Array.map (fun x ->
        let r = x.RangeAlternate
        let fullName = x.Symbol.FullName
        let name = x.Symbol.DisplayName
        let namespaceLength = fullName.Length - name.Length
        let symbolLength = r.EndColumn - r.StartColumn
        let location = 
            if namespaceLength > 0 && symbolLength > name.Length && fullName.EndsWith name then
                let startCol = r.StartColumn + namespaceLength
                let startCol = 
                    if startCol < r.EndColumn then startCol
                    else r.StartColumn
                r.StartLine, startCol, r.EndLine, r.EndColumn
            else 
                r.StartLine, r.StartColumn, r.EndLine, r.EndColumn
        //debug "-=O=- FullName = %s, Name = %s, range = %A" fullName name res
        getCategory x, location)
