module Fss.Scan

/// F# features that fss does not support (yet). This module reports them so a
/// source file can be checked for compatibility without a hard compile failure.
open Fss.Lexer

let private unsupportedIdents =
    [ "use"; "yield"; "async"; "task"; "seq"; "printfn"; "printf"; "sprintf"
      "raise"; "assert"; "interface"; "class"; "member"; "inherit"; "abstract"
      "new"; "override"; "static"; "failwithf"; "Array"; "List"; "Seq"; "Map"
      "Set"; "Dictionary"; "HashSet"; "Async"; "Task"; "Result"; "option"; "list" ]
    |> Set.ofList

/// Scan source text and return a list of "<line N>: message" strings describing
/// unsupported F# constructs. Empty list means nothing obviously unsupported.
let scan (src: string) : string list =
    let toks = Lexer.tokenize src
    let arr = Array.ofList toks
    [ for i in 0 .. arr.Length - 1 do
        let t = arr.[i]
        match t.Kind with
        | Lexer.Module -> yield sprintf "line %d: 'module' declarations are not supported" t.Line
        | Lexer.Namespace -> yield sprintf "line %d: 'namespace' declarations are ignored by fss" t.Line
        | Lexer.Caret -> yield sprintf "line %d: '^' operator is not supported" t.Line
        | Lexer.Dot when i + 1 < arr.Length && arr.[i + 1].Kind = Lexer.Dot ->
            yield sprintf "line %d: range syntax '..' is not supported; use 'for i = a to b'" t.Line
        | Lexer.Ident n when Set.contains n unsupportedIdents ->
            yield sprintf "line %d: '%s' is not supported" t.Line n
        | _ -> () ]
