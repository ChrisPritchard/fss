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
    [ for t in toks do
        match t.Kind with
        | Lexer.For -> yield sprintf "line %d: 'for' loops are not supported; use 'while'" t.Line
        | Lexer.To | Lexer.Downto -> yield sprintf "line %d: 'to'/'downto' (part of a 'for' loop)" t.Line
        | Lexer.Module -> yield sprintf "line %d: 'module' declarations are not supported" t.Line
        | Lexer.Namespace -> yield sprintf "line %d: 'namespace' declarations are ignored by fss" t.Line
        | Lexer.PipeRight -> yield sprintf "line %d: '|>' pipe operator is not supported" t.Line
        | Lexer.PipeLeft -> yield sprintf "line %d: '<|' pipe operator is not supported" t.Line
        | Lexer.Append -> yield sprintf "line %d: '@' list-append operator is not supported" t.Line
        | Lexer.Caret -> yield sprintf "line %d: '^' operator is not supported" t.Line
        | Lexer.Ident n when Set.contains n unsupportedIdents ->
            yield sprintf "line %d: '%s' is not supported" t.Line n
        | _ -> () ]
