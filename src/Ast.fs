module Fss.Ast

/// Types. `TyVar` is a unification variable used only during inference.
type Ty =
    | IntTy
    | BoolTy
    | UnitTy
    | StringTy
    | DoubleTy
    | FunTy of Ty * Ty
    | TyVar of int
    | TNamed of string * Ty list   // records, DUs, list, option
    | TupleTy of Ty list

let rec tyToString (t: Ty) : string =
    match t with
    | IntTy -> "int"
    | BoolTy -> "bool"
    | UnitTy -> "unit"
    | StringTy -> "string"
    | DoubleTy -> "double"
    | TyVar n -> "'a" + string n
    | FunTy (a, b) -> "(" + tyToString a + " -> " + tyToString b + ")"
    | TNamed (n, []) -> n
    | TNamed ("list", [ t ]) -> "(" + tyToString t + " list)"
    | TNamed ("option", [ t ]) -> "(" + tyToString t + " option)"
    | TNamed (n, args) -> n + "<" + (args |> List.map tyToString |> String.concat ", ") + ">"
    | TupleTy ts -> "(" + (ts |> List.map tyToString |> String.concat " * ") + ")"

/// Binary operators.
type BinOp =
    | Add | Sub | Mul | Div | Mod
    | Eq | Ne | Lt | Le | Gt | Ge
    | And | Or

/// Unary operators.
type UnaryOp =
    | Neg
    | Not

/// Builtin effectful functions, seeded into the initial environment.
let builtinNames = [ "print_int"; "print_bool"; "print_newline"; "print_string"; "println"; "str_len"; "int_to_string"; "bool_to_string"; "read_line"; "read_file"; "write_file"; "str_get"; "str_sub"; "int_of_string"; "print_double"; "double_to_string"; "int_to_double"; "double_to_int" ]

/// Patterns (used in match arms).
type Pattern =
    | PWildcard
    | PVar of string
    | PInt of int64
    | PBool of bool
    | PUnit
    | PStr of string
    | PTuple of Pattern list
    | PNil
    | PCons of Pattern * Pattern
    | PUnion of string * Pattern option

/// Untyped source expressions.
type Expr =
    | Int of int64
    | Float of float
    | Bool of bool
    | Unit
    | Str of string
    | Var of string
    | Unary of UnaryOp * Expr
    | Binary of BinOp * Expr * Expr
    | If of Expr * Expr * Expr
    | Let of string * Expr * Expr          // let x = e1 in e2
    | LetRec of string * Expr * Expr       // let rec x = e1 in e2 (top level only)
    | LetMutable of string * Expr * Expr   // let mutable x = e1 in e2
    | Assign of string * Expr              // x <- e
    | While of Expr * Expr                 // while e1 do e2
    | Seq of Expr list                     // e1; e2; ... (value is last)
    | Fun of string * Expr                 // fun x -> e
    | Apply of Expr * Expr                 // f a
    | Tuple of Expr list                   // (e1, e2, ...)
    | Record of Expr option * (string * Expr) list   // { ... } / { e with ... }
    | Field of Expr * string               // e.X
    | Nil                                  // []
    | Cons of Expr * Expr                  // h :: t
    | ENone                                // None
    | ESome of Expr                        // Some e
    | Match of Expr * (Pattern * Expr) list
    | Try of Expr * (Pattern * Expr) list   // try e with | pat -> ...

/// Named type definitions.
type TypeDef =
    | RecordDef of (string * Ty) list
    | UnionDef of (string * Ty) list

/// Top-level value definitions.
type Def =
    | Const of string * Expr     // let name = expr
    | ConstRec of string * Expr  // let rec name = expr
    | MutableConst of string * Expr  // let mutable name = expr
    | RecGroup of (string * Expr) list  // let rec f ... and g ...
    | Main of string option * Expr  // let main () = expr | let main argv = expr
    | Extern of string * (string * Ty) list * Ty  // extern name(p1: t1, ...) : ret

type Program = { Types: (string * TypeDef) list; Defs: Def list; Opens: string list; Includes: string list; Links: string list }

/// Pretty-printer, used in tests and error messages.
let rec exprToString (e: Expr) : string =
    match e with
    | Int n -> string n
    | Bool b -> if b then "true" else "false"
    | Unit -> "()"
    | Str s -> "\"" + s + "\""
    | Var n -> n
    | Unary (Neg, x) -> sprintf "(-%s)" (exprToString x)
    | Unary (Not, x) -> sprintf "(not %s)" (exprToString x)
    | Binary (op, a, b) ->
        let s =
            match op with
            | Add -> "+" | Sub -> "-" | Mul -> "*" | Div -> "/" | Mod -> "%"
            | Eq -> "=" | Ne -> "<>" | Lt -> "<" | Le -> "<=" | Gt -> ">" | Ge -> ">="
            | And -> "&&" | Or -> "||"
        sprintf "(%s %s %s)" (exprToString a) s (exprToString b)
    | If (c, t, e) -> sprintf "(if %s then %s else %s)" (exprToString c) (exprToString t) (exprToString e)
    | Let (n, v, b) -> sprintf "(let %s = %s in %s)" n (exprToString v) (exprToString b)
    | LetRec (n, v, b) -> sprintf "(let rec %s = %s in %s)" n (exprToString v) (exprToString b)
    | LetMutable (n, v, b) -> sprintf "(let mutable %s = %s in %s)" n (exprToString v) (exprToString b)
    | Assign (n, e) -> sprintf "(%s <- %s)" n (exprToString e)
    | While (c, b) -> sprintf "(while %s do %s)" (exprToString c) (exprToString b)
    | Seq xs -> xs |> List.map exprToString |> String.concat "; " |> sprintf "(seq %s)"
    | Fun (p, b) -> sprintf "(fun %s -> %s)" p (exprToString b)
    | Apply (f, a) -> sprintf "(%s %s)" (exprToString f) (exprToString a)
    | Tuple xs -> "(" + (xs |> List.map exprToString |> String.concat ", ") + ")"
    | Record (baseE, fields) ->
        let body =
            fields |> List.map (fun (n, v) -> n + " = " + exprToString v) |> String.concat "; "
        match baseE with
        | Some b -> sprintf "{%s with %s}" (exprToString b) body
        | None -> sprintf "{%s}" body
    | Field (e, n) -> sprintf "(%s.%s)" (exprToString e) n
    | Nil -> "[]"
    | Cons (h, t) -> sprintf "(%s :: %s)" (exprToString h) (exprToString t)
    | ENone -> "None"
    | ESome x -> sprintf "(Some %s)" (exprToString x)
    | Match (e, arms) ->
        let asStr =
            arms
            |> List.map (fun (p, b) -> patternToString p + " -> " + exprToString b)
            |> String.concat " | "
        sprintf "(match %s with | %s)" (exprToString e) asStr
    | Try (e, arms) ->
        let asStr =
            arms
            |> List.map (fun (p, b) -> patternToString p + " -> " + exprToString b)
            |> String.concat " | "
        sprintf "(try %s with | %s)" (exprToString e) asStr

and patternToString (p: Pattern) : string =
    match p with
    | PWildcard -> "_"
    | PVar n -> n
    | PInt n -> string n
    | PBool b -> if b then "true" else "false"
    | PUnit -> "()"
    | PStr s -> "\"" + s + "\""
    | PTuple ps -> "(" + (ps |> List.map patternToString |> String.concat ", ") + ")"
    | PNil -> "[]"
    | PCons (h, t) -> patternToString h + " :: " + patternToString t
    | PUnion (n, None) -> n
    | PUnion (n, Some p) -> n + " " + patternToString p
