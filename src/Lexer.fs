module Fss.Lexer

type TokKind =
    | IntLit of int64
    | StrLit of string
    | Ident of string
    | Let | Mutable | Rec | In | If | Then | Elif | Else | While | Do
    | AndKw | Try | Fun | Arrow
    | True | False
    | Type | Match | With | Of | List | Option
    | LParen | RParen | Eq | Semi
    | Plus | Minus | Star | Slash | Percent
    | Lt | Le | Gt | Ge | Ne
    | Assign          // <-
    | And | Or        // &&  ||
    | Not
    | Bar | Cons       // |  ::
    | LBrace | RBrace | LBracket | RBracket
    | Colon | Comma | Dot
    | EOF

type Token = { Kind: TokKind; Line: int }

/// Human-readable token name for error messages (AOT-safe: no %A / %d).
let tokKindName =
    function
    | IntLit n -> "integer " + string n
    | StrLit s -> "string \"" + s + "\""
    | Ident s -> "identifier '" + s + "'"
    | Let -> "let" | Mutable -> "mutable" | Rec -> "rec" | In -> "in"
    | If -> "if" | Then -> "then" | Elif -> "elif" | Else -> "else" | While -> "while" | Do -> "do"
    | AndKw -> "and"
    | Try -> "try"
    | Fun -> "fun" | Arrow -> "->"
    | Type -> "type" | Match -> "match" | With -> "with" | Of -> "of"
    | List -> "list" | Option -> "option"
    | Bar -> "|" | Cons -> "::"
    | LBrace -> "{" | RBrace -> "}" | LBracket -> "[" | RBracket -> "]"
    | Colon -> ":" | Comma -> "," | Dot -> "."
    | True -> "true" | False -> "false"
    | LParen -> "(" | RParen -> ")" | Eq -> "=" | Semi -> ";"
    | Plus -> "+" | Minus -> "-" | Star -> "*" | Slash -> "/" | Percent -> "%"
    | Lt -> "<" | Le -> "<=" | Gt -> ">" | Ge -> ">=" | Ne -> "<>"
    | Assign -> "<-" | And -> "&&" | Or -> "||" | Not -> "not"
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
    ]

let tokenize (src: string) : Token list =
    let tokens = ResizeArray<Token>()
    let n = src.Length
    let mutable i = 0
    let mutable line = 1

    let isDigit (c: char) = c >= '0' && c <= '9'
    let isIdentStart (c: char) = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c = '_'
    let isIdentChar (c: char) = isIdentStart c || isDigit c

    let emit (k: TokKind) = tokens.Add { Kind = k; Line = line }

    while i < n do
        let c = src.[i]
        if c = '\n' then
            line <- line + 1
            i <- i + 1
        elif c = ' ' || c = '\t' || c = '\r' then
            i <- i + 1
        elif c = '/' && i + 1 < n && src.[i + 1] = '/' then
            // line comment
            while i < n && src.[i] <> '\n' do i <- i + 1
        elif c = '(' && i + 1 < n && src.[i + 1] = '*' then
            // block comment
            let startLine = line
            i <- i + 2
            let mutable closed = false
            while i < n && not closed do
                if src.[i] = '*' && i + 1 < n && src.[i + 1] = ')' then
                    i <- i + 2
                    closed <- true
                else
                    if src.[i] = '\n' then line <- line + 1
                    i <- i + 1
            if not closed then failwith ("unterminated comment starting at line " + string startLine)
        elif isDigit c then
            let start = i
            while i < n && isDigit src.[i] do i <- i + 1
            let text = src.Substring(start, i - start)
            emit (IntLit(int64 text))
        elif c = '"' then
            // string literal with minimal escapes
            i <- i + 1
            let sb = System.Text.StringBuilder()
            let mutable closed = false
            while i < n && not closed do
                let ch = src.[i]
                if ch = '"' then
                    i <- i + 1
                    closed <- true
                elif ch = '\\' && i + 1 < n then
                    match src.[i + 1] with
                    | 'n' -> sb.Append('\n') |> ignore
                    | 't' -> sb.Append('\t') |> ignore
                    | 'r' -> sb.Append('\r') |> ignore
                    | '\\' -> sb.Append('\\') |> ignore
                    | '"' -> sb.Append('"') |> ignore
                    | other -> failwith ("unsupported string escape \\" + string other + " at line " + string line)
                    i <- i + 2
                elif ch = '\n' then
                    failwith ("unterminated string at line " + string line)
                else
                    sb.Append(ch) |> ignore
                    i <- i + 1
            if not closed then failwith ("unterminated string at line " + string line)
            emit (StrLit(sb.ToString()))
        elif isIdentStart c then
            let start = i
            while i < n && isIdentChar src.[i] do i <- i + 1
            let text = src.Substring(start, i - start)
            let kind =
                match Map.tryFind text keywords with
                | Some k -> k
                | None -> Ident text
            emit kind
        else
            match c with
            | '(' -> emit LParen; i <- i + 1
            | ')' -> emit RParen; i <- i + 1
            | '=' -> emit Eq; i <- i + 1
            | ';' -> emit Semi; i <- i + 1
            | '+' -> emit Plus; i <- i + 1
            | '-' ->
                if i + 1 < n && src.[i + 1] = '>' then emit Arrow; i <- i + 2
                else emit Minus; i <- i + 1
            | '*' -> emit Star; i <- i + 1
            | '/' -> emit Slash; i <- i + 1
            | '%' -> emit Percent; i <- i + 1
            | '<' ->
                if i + 1 < n && src.[i + 1] = '=' then emit Le; i <- i + 2
                elif i + 1 < n && src.[i + 1] = '>' then emit Ne; i <- i + 2
                elif i + 1 < n && src.[i + 1] = '-' then emit Assign; i <- i + 2
                else emit Lt; i <- i + 1
            | '>' ->
                if i + 1 < n && src.[i + 1] = '=' then emit Ge; i <- i + 2
                else emit Gt; i <- i + 1
            | '&' when i + 1 < n && src.[i + 1] = '&' -> emit And; i <- i + 2
            | '|' when i + 1 < n && src.[i + 1] = '|' -> emit Or; i <- i + 2
            | '|' -> emit Bar; i <- i + 1
            | ':' ->
                if i + 1 < n && src.[i + 1] = ':' then emit Cons; i <- i + 2
                else emit Colon; i <- i + 1
            | '{' -> emit LBrace; i <- i + 1
            | '}' -> emit RBrace; i <- i + 1
            | '[' -> emit LBracket; i <- i + 1
            | ']' -> emit RBracket; i <- i + 1
            | ',' -> emit Comma; i <- i + 1
            | '.' -> emit Dot; i <- i + 1
            | _ -> failwith ("unexpected character '" + string c + "' at line " + string line)

    tokens.Add { Kind = EOF; Line = line }
    List.ofSeq tokens
