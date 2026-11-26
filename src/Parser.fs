module Fss.Parser

open Fss.Ast

type P = { Toks: Lexer.Token array; mutable Idx: int; mutable Opens: string list; mutable Includes: string list; mutable Links: string list; mutable Exports: (string * string) list; mutable Fresh: int }

let peek (p: P) : Lexer.Token = p.Toks.[p.Idx]

let peek2 (p: P) : Lexer.Token =
    if p.Idx + 1 < p.Toks.Length then p.Toks.[p.Idx + 1]
    else p.Toks.[p.Toks.Length - 1]

let advance (p: P) : Lexer.Token =
    let t = p.Toks.[p.Idx]
    p.Idx <- p.Idx + 1
    t

let expect (p: P) (k: Lexer.TokKind) : unit =
    let t = peek p
    if t.Kind = k then advance p |> ignore
    else failwith ("line " + string t.Line + ": expected " + Lexer.tokKindName k + " but found " + Lexer.tokKindName t.Kind)

let startsAtom (k: Lexer.TokKind) : bool =
    match k with
    | Lexer.IntLit _ | Lexer.FloatLit _ | Lexer.StrLit _ | Lexer.True | Lexer.False | Lexer.LParen | Lexer.Ident _
    | Lexer.LBracket | Lexer.LBrace -> true
    | _ -> false

let startsPattern (k: Lexer.TokKind) : bool =
    match k with
    | Lexer.IntLit _ | Lexer.StrLit _ | Lexer.True | Lexer.False | Lexer.LParen | Lexer.Ident _
    | Lexer.LBracket -> true
    | _ -> false

// --- Namespaces / dotted names ---------------------------------------------

let private namespaceRoots =
    [ "System"; "Console"; "IO"; "File"; "Collections"; "Generic"; "Math"
      "Text"; "Threading"; "Linq"; "Diagnostics"; "Runtime" ]
    |> Set.ofList

let private isNamespaceRoot (n: string) = Set.contains n namespaceRoots

/// Parse `Ident (. Ident)*` into "A.B.C" (the leading Ident must already be the
/// current token).
let private parseDottedName (p: P) : string =
    match (peek p).Kind with
    | Lexer.Ident n ->
        advance p |> ignore
        let sb = System.Text.StringBuilder(n)
        while (peek p).Kind = Lexer.Dot do
            advance p |> ignore
            match (peek p).Kind with
            | Lexer.Ident m -> advance p |> ignore; sb.Append('.').Append(m) |> ignore
            | k -> failwith ("line " + string (peek p).Line + ": expected name after '.', found " + Lexer.tokKindName k)
        sb.ToString()
    | k -> failwith ("line " + string (peek p).Line + ": expected name, found " + Lexer.tokKindName k)

/// Map a namespace-qualified .NET call to an fss builtin.
let private resolveValueName (full: string) : string option =
    match full with
    | "System.Console.WriteLine" | "Console.WriteLine" -> Some "println"
    | "System.Console.Write" | "Console.Write" -> Some "print_string"
    | "System.Console.ReadLine" | "Console.ReadLine" -> Some "read_line"
    | "System.IO.File.ReadAllText" | "File.ReadAllText" -> Some "read_file"
    | "System.IO.File.WriteAllText" | "File.WriteAllText" -> Some "write_file"
    // System.Math -> the libm-backed builtins, so a `Math.*` program compiles
    // unchanged under both F# and fss.
    | "System.Math.Sqrt" | "Math.Sqrt" -> Some "sqrt"
    | "System.Math.Sin" | "Math.Sin" -> Some "sin"
    | "System.Math.Cos" | "Math.Cos" -> Some "cos"
    | "System.Math.Tan" | "Math.Tan" -> Some "tan"
    | "System.Math.Log" | "Math.Log" -> Some "log"
    | "System.Math.Exp" | "Math.Exp" -> Some "exp"
    | "System.Math.Floor" | "Math.Floor" -> Some "floor"
    | "System.Math.Ceiling" | "Math.Ceiling" -> Some "ceil"
    | "System.Math.Pow" | "Math.Pow" -> Some "pow"
    | "System.Math.Abs" | "Math.Abs" -> Some "abs_double"
    | _ -> None

/// Map a (possibly namespace-qualified) .NET type name to a builtin fss type.
let private resolveTypeName (opens: string list) (name: string) : Ty option =
    let hasOpen ns = List.contains ns opens
    match name with
    | "System.String" | "System.Int32" | "System.Boolean" -> Some(if name = "System.String" then StringTy elif name = "System.Int32" then IntTy else BoolTy)
    | "String" when hasOpen "System" -> Some StringTy
    | "Int32" when hasOpen "System" -> Some IntTy
    | "Boolean" when hasOpen "System" -> Some BoolTy
    | "System.Object" | "System.Char" | "System.Double" | "System.Decimal" | "System.Int64" -> None
    | _ -> None

// --- Types -----------------------------------------------------------------

let rec parseType (p: P) : Ty = parseTypeArrow p

and parseTypeArrow (p: P) : Ty =
    let l = parseTypeStar p
    if (peek p).Kind = Lexer.Arrow then
        advance p |> ignore
        FunTy(l, parseTypeArrow p)
    else
        l

and parseTypeStar (p: P) : Ty =
    let first = parseTypeApp p
    let mutable items = [ first ]
    while (peek p).Kind = Lexer.Star do
        advance p |> ignore
        items <- items @ [ parseTypeApp p ]
    match items with
    | [ t ] -> t
    | _ -> TupleTy items

and parseTypeApp (p: P) : Ty =
    let mutable t = parseTypeAtom p
    let mutable go = true
    while go do
        match (peek p).Kind with
        | Lexer.List -> advance p |> ignore; t <- TNamed("list", [ t ])
        | Lexer.Option -> advance p |> ignore; t <- TNamed("option", [ t ])
        | _ -> go <- false
    t

and parseTypeAtom (p: P) : Ty =
    match (peek p).Kind with
    | Lexer.Ident "int" -> advance p |> ignore; IntTy
    | Lexer.Ident "bool" -> advance p |> ignore; BoolTy
    | Lexer.Ident "unit" -> advance p |> ignore; UnitTy
    | Lexer.Ident "string" -> advance p |> ignore; StringTy
    | Lexer.Ident "double" -> advance p |> ignore; DoubleTy
    | Lexer.Ident "float" -> advance p |> ignore; DoubleTy
    | Lexer.Ident "cstring" -> advance p |> ignore; CStrTy
    | Lexer.Ident "char*" -> advance p |> ignore; CStrTy
    | Lexer.Ident "void" -> advance p |> ignore; UnitTy
    | Lexer.Ident _ ->
        let name = parseDottedName p
        match resolveTypeName p.Opens name with
        | Some t -> t
        | None -> TNamed(name, [])
    | Lexer.LParen ->
        advance p |> ignore
        let t = parseType p
        expect p Lexer.RParen
        t
    | k -> failwith ("line " + string (peek p).Line + ": expected type, found " + Lexer.tokKindName k)

let parseTypeDecl (p: P) : (string * TypeDef) =
    expect p Lexer.Type
    match (peek p).Kind with
    | Lexer.Ident name ->
        advance p |> ignore
        expect p Lexer.Eq
        if (peek p).Kind = Lexer.LBrace then
            advance p |> ignore
            let rec fieldsLoop acc =
                match (peek p).Kind with
                | Lexer.RBrace -> advance p |> ignore; List.rev acc
                | Lexer.Ident f ->
                    advance p |> ignore
                    expect p Lexer.Colon
                    let ft = parseType p
                    if (peek p).Kind = Lexer.Semi then advance p |> ignore
                    fieldsLoop ((f, ft) :: acc)
                | k -> failwith ("line " + string (peek p).Line + ": expected record field, found " + Lexer.tokKindName k)
            (name, RecordDef(fieldsLoop []))
        else
            if (peek p).Kind = Lexer.Bar then advance p |> ignore
            let rec casesLoop acc =
                match (peek p).Kind with
                | Lexer.Ident c ->
                    advance p |> ignore
                    let payload = if (peek p).Kind = Lexer.Of then (advance p |> ignore; parseType p) else UnitTy
                    if (peek p).Kind = Lexer.Bar then advance p |> ignore; casesLoop ((c, payload) :: acc)
                    else List.rev ((c, payload) :: acc)
                | k -> failwith ("line " + string (peek p).Line + ": expected union case, found " + Lexer.tokKindName k)
            (name, UnionDef(casesLoop []))
    | k -> failwith ("line " + string (peek p).Line + ": expected type name, found " + Lexer.tokKindName k)

/// Parse one function parameter: a bare name, or `(name : Type)`. The
/// annotation is passed through and used as a type constraint.
let parseParam (p: P) : string * Ty option =
    match (peek p).Kind with
    | Lexer.Ident name -> advance p |> ignore; (name, None)
    | Lexer.LParen ->
        advance p |> ignore
        match (peek p).Kind with
        | Lexer.Ident name ->
            advance p |> ignore
            let ann =
                if (peek p).Kind = Lexer.Colon then
                    advance p |> ignore
                    let t = parseType p
                    if (peek p).Kind = Lexer.Star then
                        failwith ("line " + string (peek p).Line + ": only single-parameter type annotations are supported")
                    Some t
                else None
            expect p Lexer.RParen
            (name, ann)
        | k -> failwith ("line " + string (peek p).Line + ": expected parameter name, found " + Lexer.tokKindName k)
    | k -> failwith ("line " + string (peek p).Line + ": expected parameter, found " + Lexer.tokKindName k)

// --- Patterns --------------------------------------------------------------

let rec parsePattern (p: P) : Pattern =
    let atom = parsePatternAtom p
    if (peek p).Kind = Lexer.Cons then
        advance p |> ignore
        PCons(atom, parsePattern p)
    else
        atom

and parsePatternAtom (p: P) : Pattern =
    match (peek p).Kind with
    | Lexer.Ident "_" -> advance p |> ignore; PWildcard
    | Lexer.Ident n ->
        advance p |> ignore
        if startsPattern (peek p).Kind then PUnion(n, Some(parsePatternAtom p))
        else PVar n
    | Lexer.IntLit n -> advance p |> ignore; PInt n
    | Lexer.StrLit s -> advance p |> ignore; PStr s
    | Lexer.True -> advance p |> ignore; PBool true
    | Lexer.False -> advance p |> ignore; PBool false
    | Lexer.LParen ->
        advance p |> ignore
        if (peek p).Kind = Lexer.RParen then
            advance p |> ignore
            PUnit
        else
            let first = parsePattern p
            if (peek p).Kind = Lexer.Comma then
                let mutable items = [ first ]
                while (peek p).Kind = Lexer.Comma do
                    advance p |> ignore
                    items <- items @ [ parsePattern p ]
                expect p Lexer.RParen
                PTuple items
            else
                expect p Lexer.RParen
                first
    | Lexer.LBracket ->
        advance p |> ignore
        if (peek p).Kind = Lexer.RBracket then
            advance p |> ignore
            PNil
        else
            let first = parsePattern p
            let mutable items = [ first ]
            while (peek p).Kind = Lexer.Semi do
                advance p |> ignore
                items <- items @ [ parsePattern p ]
            expect p Lexer.RBracket
            List.foldBack (fun h t -> PCons(h, t)) items PNil
    | k -> failwith ("line " + string (peek p).Line + ": expected pattern, found " + Lexer.tokKindName k)

// --- Expressions -----------------------------------------------------------

let rec parseExpr (p: P) : Expr = parseSeq p

and parseSeq (p: P) : Expr =
    let first = parseStmt p
    let mutable items = [ first ]
    while (peek p).Kind = Lexer.Semi do
        advance p |> ignore
        items <- items @ [ parseStmt p ]
    match items with
    | [ x ] -> x
    | _ -> Seq items

and parseStmt (p: P) : Expr =
    match (peek p).Kind with
    | Lexer.If ->
        advance p |> ignore
        let c = parseOr p
        expect p Lexer.Then
        let t = parseStmt p
        let rest = parseIfTail p
        If(c, t, rest)

    | Lexer.While ->
        advance p |> ignore
        let c = parseOr p
        expect p Lexer.Do
        let b = parseStmt p
        While(c, b)

    | Lexer.For -> parseFor p

    | Lexer.Match -> parseMatch p

    | Lexer.Try -> parseTry p

    | Lexer.Let ->
        advance p |> ignore
        let mutableFlag =
            if (peek p).Kind = Lexer.Mutable then
                advance p |> ignore
                true
            else
                false
        let recFlag =
            if (peek p).Kind = Lexer.Rec then
                advance p |> ignore
                true
            else
                false

        match (peek p).Kind with
        | Lexer.Ident name ->
            advance p |> ignore

            if (peek p).Kind = Lexer.Eq then
                expect p Lexer.Eq
                let bound = parseStmt p
                expect p Lexer.In
                let body = parseStmt p
                if mutableFlag && recFlag then
                    failwith ("line " + string (peek p).Line + ": 'let mutable rec' is not valid")
                elif mutableFlag then LetMutable(name, bound, body)
                elif recFlag then LetRec(name, bound, body)
                else Let(name, bound, body)
            else
                if mutableFlag then
                    failwith ("line " + string (peek p).Line + ": 'let mutable' cannot define a function")

                let rec paramsLoop acc =
                    match (peek p).Kind with
                    | Lexer.Ident _ | Lexer.LParen -> paramsLoop (parseParam p :: acc)
                    | _ -> List.rev acc

                let ps = paramsLoop []
                if List.isEmpty ps then
                    failwith ("line " + string (peek p).Line + ": expected '=' or function parameters after '" + name + "'")
                expect p Lexer.Eq
                let fnBody = parseStmt p
                let fn = List.foldBack (fun (pn, an) b -> Fun(pn, an, b)) ps fnBody
                if recFlag && (peek p).Kind = Lexer.AndKw then
                    let rec andLoop acc =
                        if (peek p).Kind = Lexer.AndKw then
                            advance p |> ignore
                            match (peek p).Kind with
                            | Lexer.Ident bn ->
                                advance p |> ignore
                                let bps = paramsLoop []
                                if List.isEmpty bps then
                                    failwith ("line " + string (peek p).Line + ": expected parameters after 'and " + bn + "'")
                                expect p Lexer.Eq
                                let bbody = parseStmt p
                                andLoop ((bn, List.foldBack (fun (pn, an) b -> Fun(pn, an, b)) bps bbody) :: acc)
                            | k -> failwith ("line " + string (peek p).Line + ": expected name after 'and', found " + Lexer.tokKindName k)
                        else List.rev acc
                    let group = (name, fn) :: andLoop []
                    expect p Lexer.In
                    let body = parseStmt p
                    LetRecGroup(group, body)
                else
                    expect p Lexer.In
                    let body = parseStmt p
                    if recFlag then LetRec(name, fn, body)
                    else Let(name, fn, body)
        | k -> failwith ("line " + string (peek p).Line + ": expected name after 'let', found " + Lexer.tokKindName k)

    | Lexer.Ident name when (peek2 p).Kind = Lexer.Assign ->
        advance p |> ignore
        advance p |> ignore
        let e = parseStmt p
        Assign(name, e)

    | _ -> parseOr p

and parseMatch (p: P) : Expr =
    advance p |> ignore
    let scrut = parseOr p
    expect p Lexer.With
    if (peek p).Kind = Lexer.Bar then advance p |> ignore

    let rec armsLoop acc =
        let pat = parsePattern p
        expect p Lexer.Arrow
        let body = parseStmt p
        let acc' = (pat, body) :: acc
        if (peek p).Kind = Lexer.Bar then
            advance p |> ignore
            armsLoop acc'
        else
            List.rev acc'

    Match(scrut, armsLoop [])

and parseIfTail (p: P) : Expr =
    match (peek p).Kind with
    | Lexer.Elif ->
        advance p |> ignore
        let c = parseOr p
        expect p Lexer.Then
        let t = parseStmt p
        If(c, t, parseIfTail p)
    | Lexer.Else ->
        advance p |> ignore
        parseStmt p
    | _ ->
        // `if c then x` with no else: F# unit-if, desugars to `if c then x else ()`.
        Unit

and parseTry (p: P) : Expr =
    advance p |> ignore
    let body = parseStmt p
    expect p Lexer.With
    if (peek p).Kind = Lexer.Bar then advance p |> ignore

    let rec armsLoop acc =
        let pat = parsePattern p
        expect p Lexer.Arrow
        let handler = parseStmt p
        let acc' = (pat, handler) :: acc
        if (peek p).Kind = Lexer.Bar then
            advance p |> ignore
            armsLoop acc'
        else
            List.rev acc'

    Try(body, armsLoop [])

/// `for i = a to b do body`, `for i = a downto b do body` and
/// `for x in xs do body`, lowered to `let`/`while`/`match` so the rest of the
/// compiler needs to know nothing about them.
and parseFor (p: P) : Expr =
    let fresh (stem: string) =
        p.Fresh <- p.Fresh + 1
        stem + "__" + string p.Fresh
    advance p |> ignore
    match (peek p).Kind with
    | Lexer.Ident v ->
        advance p |> ignore
        match (peek p).Kind with
        | Lexer.Eq ->
            advance p |> ignore
            let startE = parseOr p
            let up =
                match (peek p).Kind with
                | Lexer.To -> advance p |> ignore; true
                | Lexer.Downto -> advance p |> ignore; false
                | k -> failwith ("line " + string (peek p).Line + ": expected 'to' or 'downto', found " + Lexer.tokKindName k)
            let limitE = parseOr p
            expect p Lexer.Do
            let body = parseStmt p
            let loN = fresh "lo"
            let hiN = fresh "hi"
            let cmp = if up then Le else Ge
            let step = if up then Binary(Add, Var v, Int 1L) else Binary(Sub, Var v, Int 1L)
            Let(loN, startE,
                Let(hiN, limitE,
                    LetMutable(v, Var loN,
                        While(Binary(cmp, Var v, Var hiN),
                              Seq [ body; Assign(v, step) ]))))
        | Lexer.In ->
            advance p |> ignore
            let srcE = parseOr p
            expect p Lexer.Do
            let body = parseStmt p
            let restN = fresh "rest"
            let goN = fresh "go"
            let tlN = fresh "tl"
            LetMutable(restN, srcE,
                LetMutable(goN, Bool true,
                    While(Var goN,
                          Match(Var restN,
                                [ (PNil, Assign(goN, Bool false))
                                  (PCons(PVar v, PVar tlN), Seq [ Assign(restN, Var tlN); body ]) ]))))
        | k -> failwith ("line " + string (peek p).Line + ": expected '=' or 'in' after 'for " + v + "', found " + Lexer.tokKindName k)
    | k -> failwith ("line " + string (peek p).Line + ": expected loop variable after 'for', found " + Lexer.tokKindName k)

and parseOr (p: P) : Expr =
    let mutable left = parseAnd p
    let mutable go = true
    while go do
        match (peek p).Kind with
        | Lexer.Or -> advance p |> ignore; left <- Binary(Or, left, parseAnd p)
        | _ -> go <- false
    left

and parseAnd (p: P) : Expr =
    let mutable left = parsePipe p
    let mutable go = true
    while go do
        match (peek p).Kind with
        | Lexer.And -> advance p |> ignore; left <- Binary(And, left, parsePipe p)
        | _ -> go <- false
    left

/// `x |> f` is `f x` (left associative); `f <| x` is `f x` (right associative).
/// Like F#, pipes bind looser than arithmetic and comparisons but tighter than
/// `&&`/`||`.
and parsePipe (p: P) : Expr =
    let mutable left = parseCmp p
    let mutable go = true
    while go do
        match (peek p).Kind with
        | Lexer.PipeRight ->
            advance p |> ignore
            left <- Apply(parseCmp p, left)
        | Lexer.PipeLeft ->
            advance p |> ignore
            left <- Apply(left, parsePipe p)
            go <- false
        | _ -> go <- false
    left

and parseCmp (p: P) : Expr =
    let mutable left = parseCons p
    let mutable go = true
    while go do
        let op =
            match (peek p).Kind with
            | Lexer.Eq -> Some Eq
            | Lexer.Ne -> Some Ne
            | Lexer.Lt -> Some Lt
            | Lexer.Le -> Some Le
            | Lexer.Gt -> Some Gt
            | Lexer.Ge -> Some Ge
            | _ -> None
        match op with
        | Some o -> advance p |> ignore; left <- Binary(o, left, parseCons p)
        | None -> go <- false
    left

and parseCons (p: P) : Expr =
    let l = parseAdd p
    if (peek p).Kind = Lexer.Cons then
        advance p |> ignore
        Cons(l, parseCons p)
    else
        l

and parseAdd (p: P) : Expr =
    let mutable left = parseMul p
    let mutable go = true
    while go do
        match (peek p).Kind with
        | Lexer.Plus -> advance p |> ignore; left <- Binary(Add, left, parseMul p)
        | Lexer.Minus -> advance p |> ignore; left <- Binary(Sub, left, parseMul p)
        | Lexer.Append -> advance p |> ignore; left <- Apply(Apply(Var "__op_append", left), parseMul p)
        | _ -> go <- false
    left

and parseMul (p: P) : Expr =
    let mutable left = parseUnary p
    let mutable go = true
    while go do
        match (peek p).Kind with
        | Lexer.Star -> advance p |> ignore; left <- Binary(Mul, left, parseUnary p)
        | Lexer.Slash -> advance p |> ignore; left <- Binary(Div, left, parseUnary p)
        | Lexer.Percent -> advance p |> ignore; left <- Binary(Mod, left, parseUnary p)
        | _ -> go <- false
    left

and parseUnary (p: P) : Expr =
    match (peek p).Kind with
    | Lexer.Minus -> advance p |> ignore; Unary(Neg, parseUnary p)
    | Lexer.Not -> advance p |> ignore; Unary(Not, parseUnary p)
    | _ -> parseApply p

and parseApply (p: P) : Expr =
    let mutable e = parseAtom p
    while startsAtom (peek p).Kind do
        e <- Apply(e, parseAtom p)
    e

and parseAtom (p: P) : Expr =
    let mutable e = parsePrimary p
    while (peek p).Kind = Lexer.Dot do
        advance p |> ignore
        match (peek p).Kind with
        | Lexer.Ident f -> advance p |> ignore; e <- Field(e, f)
        | k -> failwith ("line " + string (peek p).Line + ": expected field name after '.', found " + Lexer.tokKindName k)
    e

and parsePrimary (p: P) : Expr =
    match (peek p).Kind with
    | Lexer.IntLit n -> advance p |> ignore; Int n
    | Lexer.FloatLit f -> advance p |> ignore; Float f
    | Lexer.StrLit s -> advance p |> ignore; Str s
    | Lexer.True -> advance p |> ignore; Bool true
    | Lexer.False -> advance p |> ignore; Bool false
    | Lexer.Ident "None" -> advance p |> ignore; ENone
    | Lexer.Ident "Some" ->
        advance p |> ignore
        ESome(parseAtom p)
    | Lexer.Ident name ->
        advance p |> ignore
        if isNamespaceRoot name && (peek p).Kind = Lexer.Dot then
            // namespace-qualified call, e.g. System.Console.WriteLine
            let sb = System.Text.StringBuilder(name)
            while (peek p).Kind = Lexer.Dot do
                advance p |> ignore
                match (peek p).Kind with
                | Lexer.Ident m -> advance p |> ignore; sb.Append('.').Append(m) |> ignore
                | k -> failwith ("line " + string (peek p).Line + ": expected name after '.', found " + Lexer.tokKindName k)
            let full = sb.ToString()
            match resolveValueName full with
            | Some b -> Var b
            | None -> Var full
        else
            Var name
    | Lexer.Fun ->
        advance p |> ignore
        match (peek p).Kind with
        | Lexer.LParen when (peek2 p).Kind = Lexer.RParen ->
            advance p |> ignore; advance p |> ignore
            expect p Lexer.Arrow
            let body = parseStmt p
            Fun("__unit", Some UnitTy, body)
        | Lexer.Ident pn ->
            advance p |> ignore
            expect p Lexer.Arrow
            let body = parseStmt p
            Fun(pn, None, body)
        | k -> failwith ("line " + string (peek p).Line + ": expected parameter name after 'fun', found " + Lexer.tokKindName k)
    | Lexer.LParen ->
        advance p |> ignore
        if (peek p).Kind = Lexer.RParen then
            advance p |> ignore
            Unit
        else
            let first = parseExpr p
            if (peek p).Kind = Lexer.Comma then
                let mutable items = [ first ]
                while (peek p).Kind = Lexer.Comma do
                    advance p |> ignore
                    items <- items @ [ parseExpr p ]
                expect p Lexer.RParen
                Tuple items
            else
                expect p Lexer.RParen
                first
    | Lexer.LBracket ->
        advance p |> ignore
        if (peek p).Kind = Lexer.RBracket then
            advance p |> ignore
            Nil
        else
            let first = parseStmt p
            let mutable items = [ first ]
            while (peek p).Kind = Lexer.Semi do
                advance p |> ignore
                items <- items @ [ parseStmt p ]
            expect p Lexer.RBracket
            List.foldBack (fun h t -> Cons(h, t)) items Nil
    | Lexer.LBrace ->
        advance p |> ignore
        let isFieldStart =
            match (peek p).Kind, (peek2 p).Kind with
            | Lexer.Ident _, Lexer.Eq -> true
            | _ -> false
        if isFieldStart then
            let rec fieldsLoop acc =
                match (peek p).Kind with
                | Lexer.RBrace -> advance p |> ignore; List.rev acc
                | Lexer.Ident f ->
                    advance p |> ignore
                    expect p Lexer.Eq
                    let v = parseStmt p
                    if (peek p).Kind = Lexer.Semi then advance p |> ignore
                    fieldsLoop ((f, v) :: acc)
                | k -> failwith ("line " + string (peek p).Line + ": expected record field, found " + Lexer.tokKindName k)
            Record(None, fieldsLoop [])
        else
            let baseE = parseStmt p
            expect p Lexer.With
            let rec fieldsLoop acc =
                match (peek p).Kind with
                | Lexer.RBrace -> advance p |> ignore; List.rev acc
                | Lexer.Ident f ->
                    advance p |> ignore
                    expect p Lexer.Eq
                    let v = parseStmt p
                    if (peek p).Kind = Lexer.Semi then advance p |> ignore
                    fieldsLoop ((f, v) :: acc)
                | k -> failwith ("line " + string (peek p).Line + ": expected record field, found " + Lexer.tokKindName k)
            Record(Some baseE, fieldsLoop [])
    | k -> failwith ("line " + string (peek p).Line + ": unexpected token " + Lexer.tokKindName k)

// --- Top level -------------------------------------------------------------

/// Parse `extern name(p1: t1, p2: t2) : ret` — a foreign C function declaration.
let parseExtern (p: P) : Def =
    advance p |> ignore
    match (peek p).Kind with
    | Lexer.Ident name ->
        advance p |> ignore
        let rec paramsLoop acc =
            match (peek p).Kind with
            | Lexer.Ident pn ->
                advance p |> ignore
                expect p Lexer.Colon
                let t = parseType p
                if (peek p).Kind = Lexer.Comma then (advance p |> ignore; paramsLoop ((pn, t) :: acc))
                else List.rev ((pn, t) :: acc)
            | _ -> List.rev acc
        expect p Lexer.LParen
        let ps = paramsLoop []
        expect p Lexer.RParen
        expect p Lexer.Colon
        let ret = parseType p
        Extern(name, ps, ret)
    | k -> failwith ("line " + string (peek p).Line + ": expected extern name, found " + Lexer.tokKindName k)

let parseDef (p: P) : Def =
    expect p Lexer.Let
    let mutableFlag =
        if (peek p).Kind = Lexer.Mutable then
            advance p |> ignore
            true
        else
            false
    let recFlag =
        if (peek p).Kind = Lexer.Rec then
            advance p |> ignore
            true
        else
            false

    match (peek p).Kind with
    | Lexer.Ident name ->
        advance p |> ignore

        if (peek p).Kind = Lexer.Eq then
            expect p Lexer.Eq
            let body = parseExpr p
            if mutableFlag && recFlag then failwith "'let mutable rec' is not valid"
            elif mutableFlag then MutableConst(name, body)
            elif recFlag then ConstRec(name, body)
            else Const(name, body)
        else
            if mutableFlag then
                failwith ("line " + string (peek p).Line + ": 'let mutable' cannot define a function")

            let rec paramsLoop acc =
                match (peek p).Kind with
                | Lexer.LParen when (peek2 p).Kind = Lexer.RParen ->
                    advance p |> ignore; advance p |> ignore
                    // `let main () = ...` is the entry point (no parameter);
                    // for other functions `()` is a real unit parameter.
                    if name = "main" && not recFlag then List.rev acc
                    else List.rev (("__unit", Some UnitTy) :: acc)
                | Lexer.Ident _ | Lexer.LParen -> paramsLoop (parseParam p :: acc)
                | _ -> List.rev acc

            let ps = paramsLoop []
            if ps.IsEmpty && not (name = "main" && not recFlag) then
                failwith ("line " + string (peek p).Line + ": expected function parameters after '" + name + "'")
            expect p Lexer.Eq
            let body = if name = "main" && not recFlag then parseExpr p else parseStmt p
            if name = "main" && not recFlag then
                match ps with
                | [] -> Main(None, body)
                | [ (pname, _) ] -> Main(Some pname, body)
                | _ -> failwith ("line " + string (peek p).Line + ": 'main' takes at most one argument")
            elif recFlag then
                let rec andLoop acc =
                    if (peek p).Kind = Lexer.AndKw then
                        advance p |> ignore
                        match (peek p).Kind with
                        | Lexer.Ident bn ->
                            advance p |> ignore
                            let bps = paramsLoop []
                            if List.isEmpty bps then failwith ("line " + string (peek p).Line + ": expected parameters")
                            expect p Lexer.Eq
                            let bbody = parseStmt p
                            let bfn = List.foldBack (fun (pn, an) b -> Fun(pn, an, b)) bps bbody
                            andLoop ((bn, bfn) :: acc)
                        | k -> failwith ("line " + string (peek p).Line + ": expected name after 'and', found " + Lexer.tokKindName k)
                    else List.rev acc
                (let group = (name, List.foldBack (fun (pn, an) b -> Fun(pn, an, b)) ps body) :: andLoop []
                 match group with
                 | [ (n, f) ] -> ConstRec(n, f)
                 | _ -> RecGroup group)
            else
                let fn = List.foldBack (fun (pn, an) b -> Fun(pn, an, b)) ps body
                Const(name, fn)
    | k -> failwith ("line " + string (peek p).Line + ": expected name after 'let', found " + Lexer.tokKindName k)

let parse (src: string) : Program =
    let toks = Lexer.layout (Lexer.tokenize src)
    let p = { Toks = Array.ofList toks; Idx = 0; Opens = []; Includes = []; Links = []; Exports = []; Fresh = 0 }

    let rec loop types defs =
        match (peek p).Kind with
        | Lexer.EOF ->
            { Types = List.rev types
              Defs = List.rev defs
              Opens = List.rev p.Opens
              Includes = List.rev p.Includes
              Links = List.rev p.Links
              Exports = List.rev p.Exports }
        | Lexer.Type -> loop (parseTypeDecl p :: types) defs
        | Lexer.Semi | Lexer.In -> advance p |> ignore; loop types defs
        | Lexer.Open ->
            advance p |> ignore
            let ns = parseDottedName p
            p.Opens <- ns :: p.Opens
            loop types defs
        | Lexer.Namespace ->
            advance p |> ignore
            parseDottedName p |> ignore
            loop types defs
        | Lexer.Include h ->
            advance p |> ignore
            p.Includes <- h :: p.Includes
            loop types defs
        | Lexer.Link l ->
            advance p |> ignore
            p.Links <- l :: p.Links
            loop types defs
        | Lexer.Extern -> loop types (parseExtern p :: defs)
        | Lexer.ExportAttr cname ->
            advance p |> ignore
            let d = parseDef p
            let names =
                match d with
                | Const (n, _) | ConstRec (n, _) | MutableConst (n, _) -> [ n ]
                | RecGroup bs -> bs |> List.map fst
                | _ -> []
            for n in names do
                p.Exports <- (n, (if cname = "" then n else cname)) :: p.Exports
            loop types (d :: defs)
        | _ -> loop types (parseDef p :: defs)

    loop [] []
