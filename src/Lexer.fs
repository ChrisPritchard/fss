module Fss.Lexer

type TokKind =
    | IntLit of int64
    | FloatLit of float
    | StrLit of string
    | Ident of string
    | Let | Mutable | Rec | In | If | Then | Elif | Else | While | Do
    | AndKw | Try | Fun | Arrow
    | True | False
    | Type | Match | With | Of | List | Option
    | Open | Namespace | Module | For | To | Downto
    | Extern
    | Include of string | Link of string
    | LParen | RParen | Eq | Semi
    | Plus | Minus | Star | Slash | Percent
    | Lt | Le | Gt | Ge | Ne
    | Assign          // <-
    | And | Or        // &&  ||
    | Not
    | PipeRight | PipeLeft   // |>  <|
    | Append | Caret         // @   ^
    | Bar | Cons       // |  ::
    | LBrace | RBrace | LBracket | RBracket
    | Colon | Comma | Dot
    | EOF

type Token = { Kind: TokKind; Line: int; Col: int }

/// Human-readable token name for error messages (AOT-safe: no %A / %d).
let tokKindName =
    function
    | IntLit n -> "integer " + string n
    | FloatLit f -> "float " + f.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
    | StrLit s -> "string \"" + s + "\""
    | Ident s -> "identifier '" + s + "'"
    | Let -> "let" | Mutable -> "mutable" | Rec -> "rec" | In -> "in"
    | If -> "if" | Then -> "then" | Elif -> "elif" | Else -> "else" | While -> "while" | Do -> "do"
    | AndKw -> "and"
    | Try -> "try"
    | Fun -> "fun" | Arrow -> "->"
    | Type -> "type" | Match -> "match" | With -> "with" | Of -> "of"
    | List -> "list" | Option -> "option"
    | Open -> "open" | Namespace -> "namespace" | Module -> "module"
    | For -> "for" | To -> "to" | Downto -> "downto"
    | Extern -> "extern" | Include s -> "#include \"" + s + "\"" | Link s -> "#link \"" + s + "\""
    | Bar -> "|" | Cons -> "::"
    | LBrace -> "{" | RBrace -> "}" | LBracket -> "[" | RBracket -> "]"
    | Colon -> ":" | Comma -> "," | Dot -> "."
    | True -> "true" | False -> "false"
    | LParen -> "(" | RParen -> ")" | Eq -> "=" | Semi -> ";"
    | Plus -> "+" | Minus -> "-" | Star -> "*" | Slash -> "/" | Percent -> "%"
    | Lt -> "<" | Le -> "<=" | Gt -> ">" | Ge -> ">=" | Ne -> "<>"
    | Assign -> "<-" | And -> "&&" | Or -> "||" | Not -> "not"
    | PipeRight -> "|>" | PipeLeft -> "<|" | Append -> "@" | Caret -> "^"
    | EOF -> "end of file"

let private keywords : Map<string, TokKind> =
    Map.ofList [
        "let", Let
        "mutable", Mutable
        "rec", Rec
        "and", AndKw
        "in", In
        "if", If
        "then", Then
        "elif", Elif
        "else", Else
        "while", While
        "do", Do
        "try", Try
        "fun", Fun
        "true", True
        "false", False
        "not", Not
        "type", Type
        "match", Match
        "with", With
        "of", Of
        "list", List
        "option", Option
        "open", Open
        "namespace", Namespace
        "module", Module
        "for", For
        "to", To
        "downto", Downto
        "extern", Extern
    ]

/// Raw tokenization. Newlines are recorded implicitly as a Line change between
/// tokens; Col records the indentation column of each token. Attributes of the
/// form `[<...>]` (with nested `<...>`) are skipped entirely.
let tokenize (src: string) : Token list =
    let tokens = ResizeArray<Token>()
    let n = src.Length
    let mutable i = 0
    let mutable line = 1
    let mutable col = 0

    let isDigit (c: char) = c >= '0' && c <= '9'
    let isIdentStart (c: char) = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c = '_'
    let isIdentChar (c: char) = isIdentStart c || isDigit c

    let emit (k: TokKind) (c: int) = tokens.Add { Kind = k; Line = line; Col = c }

    // Attributes are ignored, except that `[<Include("x.h")>]` and
    // `[<Link("m")>]` act as C interop directives (an F#-compatible spelling
    // of the `#include` / `#link` directives).
    let rec skipAttr (depth: int) (sb: System.Text.StringBuilder) =
        if i >= n then failwith ("unterminated attribute at line " + string line)
        elif src.[i] = '[' && i + 1 < n && src.[i + 1] = '<' then
            sb.Append('[').Append('<') |> ignore
            i <- i + 2; col <- col + 2; skipAttr (depth + 1) sb
        elif src.[i] = '>' && i + 1 < n && src.[i + 1] = ']' then
            i <- i + 2; col <- col + 2
            if depth > 1 then
                sb.Append('>').Append(']') |> ignore
                skipAttr (depth - 1) sb
        else
            if src.[i] = '\n' then line <- line + 1; col <- 0 else col <- col + 1
            sb.Append(src.[i]) |> ignore
            i <- i + 1
            skipAttr depth sb

    /// Recognise `Include("...")` / `Link("...")` inside attribute text.
    let attrDirective (text: string) : TokKind option =
        let tryOne (nm: string) : TokKind option =
            let idx = text.IndexOf(nm + "(")
            if idx < 0 then None
            else
                let q1 = text.IndexOf('"', idx)
                if q1 < 0 then None
                else
                    let q2 = text.IndexOf('"', q1 + 1)
                    if q2 < 0 then None
                    else
                        let v = text.Substring(q1 + 1, q2 - q1 - 1)
                        Some(if nm = "Include" then Include v else Link v)
        match tryOne "Include" with
        | Some k -> Some k
        | None -> tryOne "Link"

    while i < n do
        let c = src.[i]
        if c = '\n' then
            line <- line + 1
            col <- 0
            i <- i + 1
        elif c = ' ' then
            col <- col + 1
            i <- i + 1
        elif c = '\t' then
            col <- col + 8
            i <- i + 1
        elif c = '\r' then
            i <- i + 1
        elif c = '/' && i + 1 < n && src.[i + 1] = '/' then
            while i < n && src.[i] <> '\n' do i <- i + 1
        elif c = '(' && i + 1 < n && src.[i + 1] = '*' then
            let startLine = line
            i <- i + 2
            col <- col + 2
            let mutable closed = false
            while i < n && not closed do
                if src.[i] = '*' && i + 1 < n && src.[i + 1] = ')' then
                    i <- i + 2
                    col <- col + 2
                    closed <- true
                else
                    if src.[i] = '\n' then line <- line + 1; col <- 0 else col <- col + 1
                    i <- i + 1
            if not closed then failwith ("unterminated comment starting at line " + string startLine)
        elif c = '[' && i + 1 < n && src.[i + 1] = '<' then
            let startCol = col
            i <- i + 2
            col <- col + 2
            let sb = System.Text.StringBuilder()
            skipAttr 1 sb
            match attrDirective (sb.ToString()) with
            | Some k -> emit k startCol
            | None -> ()
        elif c = '#' then
            let startCol = col
            let start = i
            i <- i + 1
            col <- col + 1
            while i < n && isIdentStart src.[i] do i <- i + 1; col <- col + 1
            let word = src.Substring(start + 1, i - start - 1)
            while i < n && (src.[i] = ' ' || src.[i] = '\t') do i <- i + 1; col <- col + 1
            if i < n && src.[i] = '"' then
                i <- i + 1; col <- col + 1
                let s2 = i
                while i < n && src.[i] <> '"' do i <- i + 1; col <- col + 1
                let str = src.Substring(s2, i - s2)
                i <- i + 1; col <- col + 1
                if word = "include" then emit (Include str) startCol
                elif word = "link" then emit (Link str) startCol
                else failwith ("unknown directive '#" + word + "' at line " + string line)
            elif i < n && src.[i] = '<' then
                // `#include <stdio.h>`: keep the angle brackets for the emitter.
                i <- i + 1; col <- col + 1
                let s2 = i
                while i < n && src.[i] <> '>' do i <- i + 1; col <- col + 1
                let str = src.Substring(s2, i - s2)
                i <- i + 1; col <- col + 1
                if word = "include" then emit (Include ("<" + str + ">")) startCol
                else failwith ("unknown directive '#" + word + "' at line " + string line)
            else failwith ("expected string after '#" + word + "' at line " + string line)
        elif isDigit c then
            let start = i
            let startCol = col
            while i < n && isDigit src.[i] do i <- i + 1; col <- col + 1
            // A '.' followed by a digit makes this a float literal.
            if i + 1 < n && src.[i] = '.' && isDigit src.[i + 1] then
                i <- i + 1; col <- col + 1
                while i < n && isDigit src.[i] do i <- i + 1; col <- col + 1
                if i < n && (src.[i] = 'e' || src.[i] = 'E') then
                    let save = i
                    i <- i + 1; col <- col + 1
                    if i < n && (src.[i] = '+' || src.[i] = '-') then i <- i + 1; col <- col + 1
                    if i < n && isDigit src.[i] then
                        while i < n && isDigit src.[i] do i <- i + 1; col <- col + 1
                    else
                        i <- save
                        col <- col - 1
                let text = src.Substring(start, i - start)
                emit (FloatLit(float text)) startCol
            else
                let text = src.Substring(start, i - start)
                emit (IntLit(int64 text)) startCol
        elif c = '"' then
            let startCol = col
            i <- i + 1
            col <- col + 1
            let sb = System.Text.StringBuilder()
            let mutable closed = false
            while i < n && not closed do
                let ch = src.[i]
                if ch = '"' then
                    i <- i + 1; col <- col + 1
                    closed <- true
                elif ch = '\\' && i + 1 < n then
                    match src.[i + 1] with
                    | 'n' -> sb.Append('\n') |> ignore
                    | 't' -> sb.Append('\t') |> ignore
                    | 'r' -> sb.Append('\r') |> ignore
                    | '\\' -> sb.Append('\\') |> ignore
                    | '"' -> sb.Append('"') |> ignore
                    | other -> failwith ("unsupported string escape \\" + string other + " at line " + string line)
                    i <- i + 2; col <- col + 2
                elif ch = '\n' then
                    failwith ("unterminated string at line " + string line)
                else
                    sb.Append(ch) |> ignore
                    i <- i + 1; col <- col + 1
            if not closed then failwith ("unterminated string at line " + string line)
            emit (StrLit(sb.ToString())) startCol
        elif isIdentStart c then
            let start = i
            let startCol = col
            while i < n && isIdentChar src.[i] do i <- i + 1; col <- col + 1
            let text = src.Substring(start, i - start)
            let kind =
                match Map.tryFind text keywords with
                | Some k -> k
                | None -> Ident text
            emit kind startCol
        else
            let startCol = col
            match c with
            | '(' -> emit LParen startCol; i <- i + 1; col <- col + 1
            | ')' -> emit RParen startCol; i <- i + 1; col <- col + 1
            | '=' -> emit Eq startCol; i <- i + 1; col <- col + 1
            | ';' -> emit Semi startCol; i <- i + 1; col <- col + 1
            | '+' -> emit Plus startCol; i <- i + 1; col <- col + 1
            | '-' ->
                if i + 1 < n && src.[i + 1] = '>' then emit Arrow startCol; i <- i + 2; col <- col + 2
                else emit Minus startCol; i <- i + 1; col <- col + 1
            | '*' -> emit Star startCol; i <- i + 1; col <- col + 1
            | '/' -> emit Slash startCol; i <- i + 1; col <- col + 1
            | '%' -> emit Percent startCol; i <- i + 1; col <- col + 1
            | '<' ->
                if i + 1 < n && src.[i + 1] = '=' then emit Le startCol; i <- i + 2; col <- col + 2
                elif i + 1 < n && src.[i + 1] = '>' then emit Ne startCol; i <- i + 2; col <- col + 2
                elif i + 1 < n && src.[i + 1] = '-' then emit Assign startCol; i <- i + 2; col <- col + 2
                elif i + 1 < n && src.[i + 1] = '|' then emit PipeLeft startCol; i <- i + 2; col <- col + 2
                else emit Lt startCol; i <- i + 1; col <- col + 1
            | '>' ->
                if i + 1 < n && src.[i + 1] = '=' then emit Ge startCol; i <- i + 2; col <- col + 2
                else emit Gt startCol; i <- i + 1; col <- col + 1
            | '&' when i + 1 < n && src.[i + 1] = '&' -> emit And startCol; i <- i + 2; col <- col + 2
            | '|' when i + 1 < n && src.[i + 1] = '|' -> emit Or startCol; i <- i + 2; col <- col + 2
            | '|' when i + 1 < n && src.[i + 1] = '>' -> emit PipeRight startCol; i <- i + 2; col <- col + 2
            | '|' -> emit Bar startCol; i <- i + 1; col <- col + 1
            | '@' -> emit Append startCol; i <- i + 1; col <- col + 1
            | '^' -> emit Caret startCol; i <- i + 1; col <- col + 1
            | ':' ->
                if i + 1 < n && src.[i + 1] = ':' then emit Cons startCol; i <- i + 2; col <- col + 2
                else emit Colon startCol; i <- i + 1; col <- col + 1
            | '{' -> emit LBrace startCol; i <- i + 1; col <- col + 1
            | '}' -> emit RBrace startCol; i <- i + 1; col <- col + 1
            | '[' -> emit LBracket startCol; i <- i + 1; col <- col + 1
            | ']' -> emit RBracket startCol; i <- i + 1; col <- col + 1
            | ',' -> emit Comma startCol; i <- i + 1; col <- col + 1
            | '.' -> emit Dot startCol; i <- i + 1; col <- col + 1
            | _ -> failwith ("unexpected character '" + string c + "' at line " + string line)

    tokens.Add { Kind = EOF; Line = line; Col = col }
    List.ofSeq tokens

/// Tokens that may open an indented block on the following line.
let private blockOpener k =
    match k with
    | Then | Else | Do | Arrow | Eq -> true
    | _ -> false

/// Tokens after which a newline is a continuation rather than a statement end.
let private continuesLine k =
    match k with
    | Semi | In | Then | Else | Do | Arrow | Eq | With | Of | Bar | Comma
    | Cons | And | Or | Plus | Minus | Star | Slash | Percent
    | Lt | Le | Gt | Ge | Ne | Assign | Dot | LParen | LBrace | LBracket | Colon -> true
    | _ -> false

/// Rewrite newlines + indentation into explicit `;`, `in`, and `( )` so the
/// parser remains whitespace-insensitive. Explicit `;`/`in` (the existing
/// verbose syntax) is left alone.
let layout (toks: Token list) : Token list =
    let arr = List.toArray toks
    let out = ResizeArray<Token>()
    let st = ResizeArray<int>()
    st.Add 0

    let mk k line = { Kind = k; Line = line; Col = 0 }

    // Is out[s..fin) of the form `let [rec] [mutable] <name> =` (a value binding)?
    let isValueLet (s: int) (fin: int) : bool =
        let mutable k = s
        if k >= fin || out.[k].Kind <> Let then false
        else
            k <- k + 1
            if k < fin && out.[k].Kind = Rec then k <- k + 1
            if k < fin && out.[k].Kind = Mutable then k <- k + 1
            k + 1 < fin
            && (match out.[k].Kind with Ident _ -> true | _ -> false)
            && out.[k + 1].Kind = Eq

    let isExplicitOpener k =
        match k with
        | LParen | LBracket | LBrace -> true
        | _ -> false

    let mutable prevLine = -1
    let mutable stmtStart = 0
    let mutable bracketDepth = 0
    let mutable i = 0

    while i < arr.Length do
        let t = arr.[i]

        if t.Kind = EOF then
            while st.Count > 1 do
                st.RemoveAt(st.Count - 1)
                out.Add(mk RParen t.Line)
            out.Add t

        elif prevLine >= 0 && t.Line > prevLine && bracketDepth = 0 then
            let cur = st.[st.Count - 1]
            let ni = t.Col
            let prev = if out.Count > 0 then out.[out.Count - 1].Kind else EOF

            // dedent: close blocks until the indentation fits
            while st.Count > 1 && ni < st.[st.Count - 1] do
                st.RemoveAt(st.Count - 1)
                out.Add(mk RParen t.Line)

            if ni > st.[st.Count - 1] then
                if blockOpener prev && not (isExplicitOpener t.Kind) && t.Kind <> Bar then
                    st.Add ni
                    out.Add(mk LParen t.Line)
                    stmtStart <- out.Count
                // else: expression continuation across lines; ignore the newline

            else if ni = st.[st.Count - 1] then
                // same indentation: statement boundary (unless continuation).
                // At the top level (depth 1) there are no separators between defs.
                let continuesNext =
                    match t.Kind with
                    | Elif | Else | AndKw | Bar | With -> true
                    | _ -> false
                if st.Count > 1 && not (continuesLine prev) && not continuesNext then
                    if isValueLet stmtStart (out.Count) then
                        // `let x = e` extends to the end of the enclosing block,
                        // so wrap the rest of the block as its body.
                        out.Add(mk In t.Line)
                        st.Add ni
                        out.Add(mk LParen t.Line)
                    else
                        out.Add(mk Semi t.Line)
                    stmtStart <- out.Count

            out.Add t
            prevLine <- t.Line

        else
            out.Add t
            prevLine <- t.Line

        // Explicit brackets suppress layout inside themselves.
        match t.Kind with
        | LParen | LBracket | LBrace -> bracketDepth <- bracketDepth + 1
        | RParen | RBracket | RBrace -> bracketDepth <- bracketDepth - 1
        | _ -> ()

        i <- i + 1

    List.ofSeq out
