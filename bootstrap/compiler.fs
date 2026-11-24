// A self-hosted compiler: type declarations + expression -> C.
// Covers int/bool/string, arithmetic, comparisons, logic, let, if, functions
// with closures, lists, tuples, and union types with pattern matching.

type Token = TkInt of int | TIdent of string | TSym of string | TkStr of string | TSep | TEnd

type Pattern =
    | PWildcard
    | PVar of string
    | PInt of int
    | PNil
    | PCons of Pattern * Pattern
    | PTuple of Pattern * Pattern
    | PUnion of string * Pattern option
    | PNone
    | PSome of Pattern
    | PStr of string

type Expr =
    | EInt of int
    | EStr of string
    | EBool of bool
    | EVar of string
    | EBin of string * Expr * Expr
    | ELet of string * Expr * Expr
    | EIf of Expr * Expr * Expr
    | EFun of string * Expr
    | EApp of Expr * Expr
    | ENil
    | ECons of Expr * Expr
    | ETuple of Expr * Expr
    | ERecord of (string * Expr) list
    | EField of Expr * string
    | EMatch of Expr * (Pattern * Expr) list
    | ENone
    | ESome of Expr
    | EUnit
    | ESeq of Expr * Expr
    | EAssign of string * Expr

type Ty = TInt | TBool | TStr | TUnit | TVar of int | TFun of Ty * Ty | TList of Ty | TTuple of Ty * Ty | TNamed of string | TOption of Ty

type Scheme = Scheme of int list * Ty

type TPattern =
    | TPWildcard
    | TPVar of string * Ty
    | TPInt of int
    | TPNil
    | TPCons of TPattern * TPattern
    | TPTuple of TPattern * TPattern
    | TPUnion of string * TPattern option * Ty
    | TPNone
    | TPSome of TPattern
    | TPStr of string

type Def = Def of string * bool * bool * Expr | DGroup of (string * Expr) list

type TDef = TDef of string * Scheme * TExpr

type TExpr =
    | TEInt of int
    | TEBool of bool
    | TEStr of string
    | TEVar of string * Ty
    | TEBin of string * TExpr * TExpr * Ty
    | TELet of string * Ty * TExpr * TExpr * Ty
    | TEIf of TExpr * TExpr * TExpr * Ty
    | TEFun of string * Ty * TExpr
    | TEApp of TExpr * TExpr * Ty
    | TENil of Ty
    | TECons of Ty * TExpr * TExpr
    | TETuple of TExpr * TExpr * Ty
    | TERecord of Ty * (string * TExpr) list
    | TEField of Ty * TExpr * string
    | TEMatch of Ty * TExpr * (TPattern * TExpr) list
    | TENone of Ty
    | TESome of Ty * TExpr
    | TEUnit
    | TSeq of TExpr * TExpr * Ty
    | TEAssign of string * TExpr * Ty

let isSpace c = c = 32 || c = 9 || c = 13
let isDigit c = 48 <= c && c <= 57
let isUpper c = 65 <= c && c <= 90
let isLower c = 97 <= c && c <= 122
let isAlpha c = isUpper c || isLower c || c = 95

let rec skip s i n inComment =
    if i >= n then i
    elif inComment then
        (let c = str_get s i in
         if c = 10 then skip s (i + 1) n false
         else skip s (i + 1) n true)
    else
        (let c = str_get s i in
         if isSpace c then skip s (i + 1) n false
         elif c = 47 && i + 1 < n && str_get s (i + 1) = 47 then skip s (i + 2) n true
         else i)

let rec scanIdent s i n =
    if i >= n then i
    else (let c = str_get s i in
          if isAlpha c || isDigit c then scanIdent s (i + 1) n else i)

let rec scanNum s i n =
    if i >= n then i
    else (let c = str_get s i in
          if isDigit c then scanNum s (i + 1) n else i)

let rec scanStr s i n =
    if i >= n then i
    else (let c = str_get s i in
          if c = 34 then i + 1
          elif c = 92 && i + 1 < n then scanStr s (i + 2) n
          else scanStr s (i + 1) n)

let rec unescape s i n =
    if i >= n then ""
    else (let c = str_get s i in
          if c = 92 && i + 1 < n then
              (let c2 = str_get s (i + 1) in
               if c2 = 110 then "\n" + unescape s (i + 2) n
               elif c2 = 116 then "\t" + unescape s (i + 2) n
               elif c2 = 114 then "\r" + unescape s (i + 2) n
               elif c2 = 92 then "\\" + unescape s (i + 2) n
               elif c2 = 34 then "\"" + unescape s (i + 2) n
               else str_sub s i 1 + unescape s (i + 1) n)
          else str_sub s i 1 + unescape s (i + 1) n)

let rec lexRev s i n acc =
    let j = skip s i n false in
    if j >= n then TEnd :: acc
    else
        (let c = str_get s j in
         if isAlpha c then
             (let k = scanIdent s (j + 1) n in
              lexRev s k n (TIdent (str_sub s j (k - j)) :: acc))
         elif isDigit c then
             (let k = scanNum s (j + 1) n in
              lexRev s k n (TkInt (int_of_string (str_sub s j (k - j))) :: acc))
         elif c = 34 then
             (let k = scanStr s (j + 1) n in
              lexRev s k n (TkStr (unescape s (j + 1) (k - 1)) :: acc))
         elif c = 10 then lexRev s (j + 1) n (TSep :: acc)
         else
             (let c2 = if j + 1 < n then str_get s (j + 1) else 0 in
              if c = 60 && c2 = 61 then lexRev s (j + 2) n (TSym "<=" :: acc)
              elif c = 60 && c2 = 45 then lexRev s (j + 2) n (TSym "<-" :: acc)
              elif c = 62 && c2 = 61 then lexRev s (j + 2) n (TSym ">=" :: acc)
              elif c = 60 && c2 = 62 then lexRev s (j + 2) n (TSym "<>" :: acc)
              elif c = 38 && c2 = 38 then lexRev s (j + 2) n (TSym "&&" :: acc)
              elif c = 124 && c2 = 124 then lexRev s (j + 2) n (TSym "||" :: acc)
              elif c = 45 && c2 = 62 then lexRev s (j + 2) n (TSym "->" :: acc)
              elif c = 58 && c2 = 58 then lexRev s (j + 2) n (TSym "::" :: acc)
              else lexRev s (j + 1) n (TSym (str_sub s j 1) :: acc)))

let rec reverse acc xs =
    match xs with
    | [] -> acc
    | h :: t -> reverse (h :: acc) t

let lex s = reverse [] (lexRev s 0 (str_len s) [])

let rec append xs ys =
    match xs with
    | [] -> ys
    | h :: t -> h :: append t ys

let rec mem x xs =
    match xs with
    | [] -> false
    | h :: t -> if h = x then true else mem x t

let rec assoc key xs =
    match xs with
    | [] -> None
    | (k, v) :: rest -> if k = key then Some v else assoc key rest

let rec union xs ys =
    match xs with
    | [] -> ys
    | h :: t -> if mem h ys then union t ys else h :: union t ys

let rec diff xs ys =
    match xs with
    | [] -> []
    | h :: t -> if mem h ys then diff t ys else h :: diff t ys

// --- type declarations ---

let mutable typeDefs = []
let mutable ctorEnv = []
let mutable recordDefs = []

let rec findField f defs =
    match defs with
    | [] -> None
    | (name, fields) :: rest ->
        (match assoc f fields with
         | Some t -> Some (name, t)
         | None -> findField f rest)

let rec parseType toks =
    match parseTypePostfix toks with
    | (t, r) -> parseTypeRest t r

and parseTypeRest acc toks =
    match toks with
    | TSym "*" :: r ->
        (match parseTypePostfix r with
         | (t2, r2) -> parseTypeRest (TTuple (acc, t2)) r2)
    | _ -> (acc, toks)

and parseTypePostfix toks =
    match parseTypeAtom toks with
    | (t, r) -> parseTypePostfixRest t r

and parseTypePostfixRest t toks =
    match toks with
    | TIdent "list" :: r -> parseTypePostfixRest (TList t) r
    | TIdent "option" :: r -> parseTypePostfixRest (TOption t) r
    | _ -> (t, toks)

and parseTypeAtom toks =
    match toks with
    | TIdent "int" :: r -> (TInt, r)
    | TIdent "bool" :: r -> (TBool, r)
    | TIdent "string" :: r -> (TStr, r)
    | TSym "(" :: r ->
        (match parseType r with
         | (t, TSym ")" :: r2) -> (t, r2)
         | _ -> failwith "expected ')'")
    | TIdent n :: r -> (TNamed n, r)
    | _ -> failwith "bad type"

let rec parseTypeDecls toks =
    match toks with
    | TSep :: r -> parseTypeDecls r
    | TIdent "type" :: TIdent name :: TSym "=" :: r ->
        (match r with
         | TSym "{" :: _ ->
             (match parseRecordFields r [] with
              | (fields, r2) ->
                  (recordDefs <- (name, fields) :: recordDefs;
                   parseTypeDecls r2))
         | _ ->
             (match parseCases r [] with
              | (cases, r2) ->
                  (typeDefs <- (name, cases) :: typeDefs;
                   ctorEnv <- addCtors name cases 0 ctorEnv;
                   parseTypeDecls r2)))
    | _ -> toks

and skipTypeSeps toks =
    match toks with
    | TSep :: r -> skipTypeSeps r
    | _ -> toks

and parseCases toks acc =
    match skipTypeSeps toks with
    | TSym "|" :: r -> parseCases r acc
    | TIdent c :: r ->
        (match parseCasePayload r with
         | (nullary, payload, r2) ->
             (match skipTypeSeps r2 with
              | TSym "|" :: r3 -> parseCases r3 ((c, nullary, payload) :: acc)
              | _ -> (reverse [] ((c, nullary, payload) :: acc), r2)))
    | _ -> (reverse [] acc, toks)

and parseRecordFields toks acc =
    match toks with
    | TSym "{" :: r -> parseRecordFields r acc
    | TSym "}" :: r -> (reverse [] acc, r)
    | TSym ";" :: r -> parseRecordFields r acc
    | TIdent f :: TSym ":" :: r ->
        (match parseType r with
         | (ft, r2) -> parseRecordFields r2 ((f, ft) :: acc))
    | _ -> failwith "expected record field"

and parseCasePayload toks =
    match toks with
    | TIdent "of" :: r ->
        (match parseType r with
         | (t, r2) -> (false, t, r2))
    | _ -> (true, TInt, toks)

and startsWithBar toks =
    match toks with
    | TSym "|" :: _ -> true
    | _ -> false

and dropBar toks =
    match toks with
    | TSym "|" :: r -> r
    | _ -> toks

and addCtors name cases tag acc =
    match cases with
    | [] -> acc
    | (c, nullary, payload) :: rest -> addCtors name rest (tag + 1) ((c, (name, nullary, payload, tag)) :: acc)

// --- expression parser ---

let precOf op =
    if op = "||" then 1
    elif op = "&&" then 2
    elif op = "=" then 3
    elif op = "<>" then 3
    elif op = "<" then 3
    elif op = "<=" then 3
    elif op = ">" then 3
    elif op = ">=" then 3
    elif op = "::" then 5
    elif op = "+" then 10
    elif op = "-" then 10
    elif op = "*" then 20
    elif op = "/" then 20
    else (-1)

let startsAtom toks =
    match toks with
    | TkInt _ :: _ -> true
    | TkStr _ :: _ -> true
    | TSym "(" :: _ -> true
    | TSym "[" :: _ -> true
    | TIdent x :: _ -> x <> "then" && x <> "else" && x <> "in" && x <> "let" && x <> "if" && x <> "fun" && x <> "match" && x <> "with"
    | _ -> false

let rec parseExpr toks = parseAssign toks

and parseAssign toks =
    match parseBin toks 0 with
    | (EVar x, TSym "<-" :: r) ->
        (match parseAssign r with
         | (e, r2) -> (EAssign (x, e), r2))
    | (e, r) -> (e, r)

and parseBin toks minPrec =
    match parseApp toks with
    | (lhs, r1) -> parseBinTail minPrec lhs r1

and parseBinTail minPrec lhs toks =
    match toks with
    | TSym op :: r ->
        if precOf op >= minPrec then
            if op = "::" then
                (match parseBin (skipSeps r) (precOf op) with
                 | (rhs, r2) -> parseBinTail minPrec (ECons (lhs, rhs)) r2)
            else
                (match parseBin (skipSeps r) (precOf op + 1) with
                 | (rhs, r2) -> parseBinTail minPrec (EBin (op, lhs, rhs)) r2)
        else (lhs, toks)
    | _ -> (lhs, toks)

and parseApp toks =
    match parseAtom toks with
    | (lhs, r1) -> parseAppTail lhs r1

and parseAppTail lhs toks =
    match toks with
    | TSym "." :: TIdent f :: r -> parseAppTail (EField (lhs, f)) r
    | _ ->
        if startsAtom toks then
            (match parseAtom toks with
             | (arg, r2) -> parseAppTail (EApp (lhs, arg)) r2)
        else (lhs, toks)

and parseAtom toks =
    match toks with
    | TkInt n :: r -> (EInt n, r)
    | TkStr s :: r -> (EStr s, r)
    | TIdent "true" :: r -> (EBool true, r)
    | TIdent "false" :: r -> (EBool false, r)
    | TIdent "let" :: TIdent name :: r ->
        (match parseParams r with
         | (params, TSym "=" :: r2) ->
             (match parseExpr (skipSeps r2) with
              | (v, TIdent "in" :: r3) ->
                  (match parseExpr (skipSeps r3) with
                   | (body, r4) -> (ELet (name, wrapLambdas params v, body), r4))
              | _ -> failwith "expected 'in'")
         | _ -> failwith "expected '=' in let")
    | TIdent "if" :: r ->
        (match parseExpr r with
         | (c, TIdent "then" :: r2) ->
             (match parseExpr (skipSeps r2) with
              | (t, r3) -> parseIfTail c t r3)
         | _ -> failwith "expected 'then'")
    | TIdent "match" :: r ->
        (match parseExpr r with
         | (scrut, TIdent "with" :: r2) ->
             (match parseArms r2 [] with
              | (arms, r3) -> (EMatch (scrut, arms), r3))
         | _ -> failwith "expected 'with'")
    | TIdent "fun" :: TIdent p :: TSym "->" :: r ->
        (match parseExpr (skipSeps r) with
         | (body, r2) -> (EFun (p, body), r2))
    | TIdent "None" :: r -> (ENone, r)
    | TIdent "Some" :: r ->
        (match parseApp r with
         | (e, r2) -> (ESome e, r2))
    | TIdent "not" :: r ->
        (match parseAtom r with
         | (e, r2) -> (EBin ("=", e, EBool false), r2))
    | TIdent x :: r -> (EVar x, r)
    | TSym "-" :: r ->
        (match parseAtom r with
         | (e, r2) -> (EBin ("-", EInt 0, e), r2))
    | TSym "(" :: TSym ")" :: r -> (EUnit, r)
    | TSym "(" :: r ->
        (match parseExpr r with
         | (e, TSym "," :: r2) ->
             (match parseTupleRest e r2 with
              | (tup, TSym ")" :: r3) -> (tup, r3)
              | _ -> failwith "expected ')'")
         | (e, TSym ";" :: r2) ->
             (match parseSeqRest e r2 with
              | (seq, TSym ")" :: r3) -> (seq, r3)
              | _ -> failwith "expected ')'")
         | (e, TSym ")" :: r2) -> (e, r2)
         | _ -> failwith "expected ')'")
    | TSym "[" :: TSym "]" :: r -> (ENil, r)
    | TSym "[" :: r ->
        (match parseListElems r [] with
         | (items, r2) -> (foldCons items, r2))
    | TSym "{" :: r ->
        (match parseRecordExprFields r [] with
         | (fields, r2) -> (ERecord fields, r2))
    | _ -> failwith "unexpected token"

and parseRecordExprFields toks acc =
    match toks with
    | TSym "{" :: r -> parseRecordExprFields r acc
    | TSym "}" :: r -> (reverse [] acc, r)
    | TSym ";" :: r -> parseRecordExprFields r acc
    | TIdent f :: TSym "=" :: r ->
        (match parseExpr r with
         | (v, r2) -> parseRecordExprFields r2 ((f, v) :: acc))
    | _ -> failwith "expected record field"

and parseListElems toks acc =
    match parseExpr (skipSeps toks) with
    | (e, r) ->
        (match skipSeps r with
         | TSym ";" :: r2 -> parseListElems r2 (e :: acc)
         | TSym "]" :: r2 -> (reverse [] (e :: acc), r2)
         | _ -> failwith "expected ';' or ']'")

and foldCons items =
    match items with
    | [] -> ENil
    | h :: t -> ECons (h, foldCons t)

and skipSeps toks =
    match toks with
    | TSep :: r -> skipSeps r
    | _ -> toks

and parseSeqRest acc toks =
    match parseAssign (skipSeps toks) with
    | (e, TSym ";" :: r2) -> parseSeqRest (ESeq (acc, e)) r2
    | (e, r2) -> (ESeq (acc, e), r2)

and parseTupleRest acc toks =
    match parseExpr toks with
    | (e, TSym "," :: r2) -> parseTupleRest (ETuple (acc, e)) r2
    | (e, r2) -> (ETuple (acc, e), r2)

and parseIfTail c t toks =
    match skipSeps toks with
    | TIdent "elif" :: r ->
        (match parseExpr r with
         | (c2, TIdent "then" :: r2) ->
             (match parseExpr (skipSeps r2) with
              | (t2, r3) ->
                  (match parseIfTail c2 t2 r3 with
                   | (e, r4) -> (EIf (c, t, e), r4)))
         | _ -> failwith "expected 'then'")
    | TIdent "else" :: r ->
        (match parseExpr (skipSeps r) with
         | (e, r4) -> (EIf (c, t, e), r4))
    | _ -> failwith "expected 'else'"

and parseParams toks =
    match toks with
    | TIdent p :: r ->
        (match parseParams r with
         | (ps, rest) -> (p :: ps, rest))
    | _ -> ([], toks)

and wrapLambdas params body =
    match params with
    | [] -> body
    | p :: rest -> EFun (p, wrapLambdas rest body)

and parseRecGroup toks members acc =
    match toks with
    | TIdent name :: r ->
        (match parseParams r with
         | (params, TSym "=" :: r2) ->
             (match parseExpr (skipSeps r2) with
              | (body, TIdent "in" :: _) -> (reverse [] acc, toks)
              | (body, r3) ->
                  (match skipSeps r3 with
                   | TIdent "and" :: r4 -> parseRecGroup r4 ((name, wrapLambdas params body) :: members) acc
                   | _ -> parseDefs r3 (DGroup (reverse [] ((name, wrapLambdas params body) :: members)) :: acc)))
         | _ -> failwith "expected '=' in let")
    | _ -> failwith "expected name after 'and'"

and parseDefs toks acc =
    match toks with
    | TSep :: r -> parseDefs r acc
    | TIdent "let" :: TIdent "rec" :: TIdent name :: r ->
        (match parseParams r with
         | (params, TSym "=" :: r2) ->
             (match parseExpr (skipSeps r2) with
              | (body, TIdent "in" :: _) -> (reverse [] acc, toks)
              | (body, r3) ->
                  (let first = (name, wrapLambdas params body) in
                   match skipSeps r3 with
                   | TIdent "and" :: r4 -> parseRecGroup r4 [first] acc
                   | _ -> parseDefs r3 (Def (name, true, false, wrapLambdas params body) :: acc)))
         | _ -> failwith "expected '=' in let")
    | TIdent "let" :: TIdent "mutable" :: TIdent name :: TSym "=" :: r ->
        (match parseExpr (skipSeps r) with
         | (body, TIdent "in" :: _) -> (reverse [] acc, toks)
         | (body, r2) -> parseDefs r2 (Def (name, false, true, body) :: acc))
    | TIdent "let" :: TIdent name :: r ->
        (match parseParams r with
         | (params, TSym "=" :: r2) ->
             (match parseExpr (skipSeps r2) with
              | (body, TIdent "in" :: _) -> (reverse [] acc, toks)
              | (body, r3) -> parseDefs r3 (Def (name, false, false, wrapLambdas params body) :: acc))
         | _ -> failwith "expected '=' in let")
    | _ -> (reverse [] acc, toks)

and parseArms toks acc =
    match toks with
    | TSym "|" :: r -> parseArm r acc
    | TSep :: r ->
        (match skipSeps r with
         | TSym "|" :: r2 -> parseArm r2 acc
         | _ -> (reverse [] acc, toks))
    | _ -> if acc = [] then parseArm toks acc else (reverse [] acc, toks)

and parseArm toks acc =
    match parsePattern toks with
    | (pat, TSym "->" :: r2) ->
        (match parseExpr (skipSeps r2) with
         | (body, r3) -> parseArms r3 ((pat, body) :: acc))
    | _ -> failwith "expected '->'"

and parsePattern toks = parsePatternCons toks

and parsePatTupleRest acc toks =
    match parsePatternCons toks with
    | (p, TSym "," :: r2) -> parsePatTupleRest (PTuple (acc, p)) r2
    | (p, r2) -> (PTuple (acc, p), r2)

and parseListPatElems toks acc =
    match parsePatternCons toks with
    | (p, r) ->
        (match skipSeps r with
         | TSym ";" :: r2 -> parseListPatElems r2 (p :: acc)
         | TSym "]" :: r2 -> (foldPatCons (reverse [] (p :: acc)), r2)
         | _ -> failwith "expected ';' or ']'")

and foldPatCons items =
    match items with
    | [] -> PNil
    | h :: t -> PCons (h, foldPatCons t)

and parsePatternCons toks =
    match parsePatternAtom toks with
    | (p, TSym "::" :: r) ->
        (match parsePatternCons r with
         | (t, r2) -> (PCons (p, t), r2))
    | (p, r) -> (p, r)

and parsePatternAtom toks =
    match toks with
    | TIdent "_" :: r -> (PWildcard, r)
    | TkInt n :: r -> (PInt n, r)
    | TkStr s :: r -> (PStr s, r)
    | TSym "[" :: TSym "]" :: r -> (PNil, r)
    | TSym "[" :: r -> parseListPatElems r []
    | TSym "(" :: r ->
        (match parsePatternCons r with
         | (p1, TSym "," :: r2) ->
             (match parsePatTupleRest p1 r2 with
              | (tup, TSym ")" :: r3) -> (tup, r3)
              | _ -> failwith "expected ')'")
         | (p1, TSym ")" :: r2) -> (p1, r2)
         | _ -> failwith "expected ')'")
    | TIdent "None" :: r -> (PNone, r)
    | TIdent "Some" :: r ->
        (match parsePatternAtom r with
         | (p, r2) -> (PSome p, r2))
    | TIdent x :: r ->
        (match assoc x ctorEnv with
         | Some (tname, nullary, payload, tag) ->
             if nullary then (PUnion (x, None), r)
             elif startsPattern r then
                 (match parsePatternAtom r with
                  | (p, r2) -> (PUnion (x, Some p), r2))
             else (PVar x, r)
         | None ->
             if startsPattern r then
                 (match parsePatternAtom r with
                  | (p, r2) -> (PUnion (x, Some p), r2))
             else (PVar x, r))
    | _ -> failwith "bad pattern"

and startsPattern toks =
    match toks with
    | TkInt _ :: _ -> true
    | TkStr _ :: _ -> true
    | TSym "(" :: _ -> true
    | TSym "[" :: _ -> true
    | TIdent _ :: _ -> true
    | _ -> false

// --- unification ---

let mutable subst = []
let mutable varN = 0
let mutable mutableNames = []
let freshVar dummy = (varN <- varN + 1; TVar varN)

let rec prune t =
    match t with
    | TVar n ->
        (match assoc n subst with
         | Some t2 -> (let p = prune t2 in (subst <- (n, p) :: subst; p))
         | None -> TVar n)
    | TFun (a, b) -> TFun (prune a, prune b)
    | TList e -> TList (prune e)
    | TTuple (a, b) -> TTuple (prune a, prune b)
    | TOption e -> TOption (prune e)
    | _ -> t

let rec occurs n t =
    match prune t with
    | TVar m -> m = n
    | TFun (a, b) -> occurs n a || occurs n b
    | TList e -> occurs n e
    | TTuple (a, b) -> occurs n a || occurs n b
    | TOption e -> occurs n e
    | _ -> false

let rec unify a b =
    let a = prune a in
    let b = prune b in
    if a = b then ()
    else
        match a with
        | TVar n ->
            if occurs n b then failwith "type error: infinite type"
            else subst <- (n, b) :: subst
        | _ ->
            (match b with
             | TVar n ->
                 if occurs n a then failwith "type error: infinite type"
                 else subst <- (n, a) :: subst
             | TFun (a1, a2) ->
                 (match a with
                  | TFun (b1, b2) -> (unify a1 b1; unify a2 b2)
                  | _ -> failwith "type error: type mismatch")
             | TList e1 ->
                 (match a with
                  | TList e2 -> unify e1 e2
                  | _ -> failwith "type error: type mismatch")
             | TTuple (c1, d1) ->
                 (match a with
                  | TTuple (c2, d2) -> (unify c1 c2; unify d1 d2)
                  | _ -> failwith "type error: type mismatch")
             | TOption e1 ->
                 (match a with
                  | TOption e2 -> unify e1 e2
                  | _ -> failwith "type error: type mismatch")
             | _ -> failwith "type error: type mismatch")

// --- type schemes (let-polymorphism) ---

let schemeBody sc =
    match sc with
    | Scheme (_, t) -> t

let schemeVars sc =
    match sc with
    | Scheme (qs, _) -> qs

let rec substTyVars m t =
    match t with
    | TVar n -> (match assoc n m with | Some t2 -> t2 | None -> TVar n)
    | TFun (a, b) -> TFun (substTyVars m a, substTyVars m b)
    | TList e -> TList (substTyVars m e)
    | TTuple (a, b) -> TTuple (substTyVars m a, substTyVars m b)
    | TOption e -> TOption (substTyVars m e)
    | _ -> t

let rec mapFresh qs =
    match qs with
    | [] -> []
    | q :: rest -> (q, freshVar 0) :: mapFresh rest

let instantiate sc =
    match sc with
    | Scheme (qs, body) ->
        if qs = [] then body
        else substTyVars (mapFresh qs) body

let rec freeTyVars t =
    match prune t with
    | TVar n -> [ n ]
    | TFun (a, b) -> union (freeTyVars a) (freeTyVars b)
    | TList e -> freeTyVars e
    | TTuple (a, b) -> union (freeTyVars a) (freeTyVars b)
    | TOption e -> freeTyVars e
    | _ -> []

let rec freeTyVarsEnv env =
    match env with
    | [] -> []
    | (_, sc) :: rest -> union (freeTyVars (schemeBody sc)) (freeTyVarsEnv rest)

let generalize env ty =
    let ty2 = prune ty in
    Scheme (diff (freeTyVars ty2) (freeTyVarsEnv env), ty2)

let schemeOf x env =
    match assoc x env with
    | Some sc -> sc
    | None -> failwith ("type error: unbound " + x)

// --- builtins ---

let rec envNames xs =
    match xs with
    | [] -> []
    | (n, _) :: rest -> n :: envNames rest

let failVar = 1000000

let builtinEnv =
    [ ("print_int", Scheme ([], TFun (TInt, TUnit)))
    ; ("print_bool", Scheme ([], TFun (TBool, TUnit)))
    ; ("print_newline", Scheme ([], TFun (TUnit, TUnit)))
    ; ("print_string", Scheme ([], TFun (TStr, TUnit)))
    ; ("str_len", Scheme ([], TFun (TStr, TInt)))
    ; ("str_get", Scheme ([], TFun (TStr, TFun (TInt, TInt))))
    ; ("str_sub", Scheme ([], TFun (TStr, TFun (TInt, TFun (TInt, TStr)))))
    ; ("int_of_string", Scheme ([], TFun (TStr, TInt)))
    ; ("int_to_string", Scheme ([], TFun (TInt, TStr)))
    ; ("read_file", Scheme ([], TFun (TStr, TStr)))
    ; ("failwith", Scheme ([failVar], TFun (TStr, TVar failVar)))
    ]

let builtinNames = envNames builtinEnv

// --- typed inference ---

let rec tyOf te =
    match te with
    | TEInt n -> TInt
    | TEBool b -> TBool
    | TEStr s -> TStr
    | TEVar (x, t) -> t
    | TEBin (op, a, b, t) -> t
    | TELet (x, bt, v, body, rt) -> rt
    | TEIf (c, t, e, rt) -> rt
    | TEFun (p, ft, body) -> ft
    | TEApp (f, a, rt) -> rt
    | TENil t -> t
    | TECons (t, h, tl) -> t
    | TETuple (a, b, t) -> t
    | TERecord (t, fields) -> t
    | TEField (t, e, f) -> t
    | TEMatch (rt, scrut, arms) -> rt
    | TENone t -> t
    | TESome (t, x) -> t
    | TEUnit -> TUnit
    | TSeq (a, b, t) -> t
    | TEAssign (x, e, t) -> t

let rec recordFieldsOf name defs =
    match defs with
    | [] -> []
    | (n, fields) :: rest -> if n = name then fields else recordFieldsOf name rest

let rec inferT env e =
    match e with
    | EInt n -> TEInt n
    | EStr s -> TEStr s
    | EBool b -> TEBool b
    | EVar x -> TEVar (x, instantiate (schemeOf x env))
    | EBin (op, a, b) ->
        let ta = inferT env a in
        let tb = inferT env b in
        if op = "+" && (prune (tyOf ta) = TStr || prune (tyOf tb) = TStr) then
            (unify (tyOf ta) TStr; unify (tyOf tb) TStr; TEBin (op, ta, tb, TStr))
        elif op = "&&" || op = "||" then
            (unify (tyOf ta) TBool; unify (tyOf tb) TBool; TEBin (op, ta, tb, TBool))
        elif op = "=" || op = "<>" || op = "<" || op = "<=" || op = ">" || op = ">=" then
            (unify (tyOf ta) (tyOf tb); TEBin (op, ta, tb, TBool))
        else (unify (tyOf ta) TInt; unify (tyOf tb) TInt; TEBin (op, ta, tb, TInt))
    | ELet (x, v, body) ->
        (let tv = inferT env v in
         let bt = tyOf tv in
         let tb = inferT ((x, Scheme ([], bt)) :: env) body in
         TELet (x, bt, tv, tb, tyOf tb))
    | EIf (c, t, e) ->
        (let tc = inferT env c in
         let tt = inferT env t in
         let te = inferT env e in
         (unify (tyOf tc) TBool; unify (tyOf tt) (tyOf te); TEIf (tc, tt, te, tyOf tt)))
    | EFun (p, body) ->
        (let pt = freshVar 0 in
         let bt = inferT ((p, Scheme ([], pt)) :: env) body in
         TEFun (p, TFun (pt, tyOf bt), bt))
    | EApp (f, a) ->
        (let tf = inferT env f in
         let ta = inferT env a in
         let rt = freshVar 0 in
         (unify (tyOf tf) (TFun (tyOf ta, rt)); TEApp (tf, ta, rt)))
    | ENil ->
        (let et = freshVar 0 in TENil (TList et))
    | ECons (h, t) ->
        (let th = inferT env h in
         let tt = inferT env t in
         (unify (tyOf tt) (TList (tyOf th)); TECons (tyOf tt, th, tt)))
    | ENone ->
        (let et = freshVar 0 in TENone (TOption et))
    | ESome e ->
        (let te = inferT env e in
         TESome (TOption (tyOf te), te))
    | EUnit -> TEUnit
    | ESeq (a, b) ->
        (let ta = inferT env a in
         let tb = inferT env b in
         TSeq (ta, tb, tyOf tb))
    | EAssign (x, e) ->
        (if mem x mutableNames then
             (let te = inferT env e in
              (unify (schemeBody (schemeOf x env)) (tyOf te);
               TEAssign (x, te, TUnit)))
         else failwith ("type error: " + x + " is not mutable"))
    | ETuple (e1, e2) ->
        (let t1 = inferT env e1 in
         let t2 = inferT env e2 in
         TETuple (t1, t2, TTuple (tyOf t1, tyOf t2)))
    | ERecord fields ->
        (match fields with
         | [] -> failwith "empty record"
         | (f0, _) :: _ ->
             (match findField f0 recordDefs with
              | Some (name, _) ->
                  (let typed = inferRecordFields env name fields in
                   TERecord (TNamed name, typed))
              | None -> failwith ("unknown field " + f0)))
    | EField (e, f) ->
        (let te = inferT env e in
         match findField f recordDefs with
         | Some (name, ft) -> (unify (tyOf te) (TNamed name); TEField (ft, te, f))
         | None -> failwith ("unknown field " + f))
    | EMatch (scrut, arms) ->
        (let ts = inferT env scrut in
         let st = tyOf ts in
         let typed = inferArms env st arms in
         match typed with
         | [] -> failwith "empty match"
         | (tp0, tb0) :: rest ->
             (unifyAll rest (tyOf tb0);
              TEMatch (tyOf tb0, ts, typed)))

and inferPattern env scrutTy p =
    match p with
    | PWildcard -> (env, TPWildcard)
    | PVar x -> ((x, Scheme ([], scrutTy)) :: env, TPVar (x, scrutTy))
    | PInt n -> (unify scrutTy TInt; (env, TPInt n))
    | PStr s -> (unify scrutTy TStr; (env, TPStr s))
    | PNone -> (unify scrutTy (TOption (freshVar 0)); (env, TPNone))
    | PSome p ->
        (let et = freshVar 0 in
         (unify scrutTy (TOption et);
          match inferPattern env et p with
          | (env1, tp) -> (env1, TPSome tp)))
    | PNil -> (unify scrutTy (TList (freshVar 0)); (env, TPNil))
    | PTuple (p1, p2) ->
        (let t1 = freshVar 0 in
         let t2 = freshVar 0 in
         (unify scrutTy (TTuple (t1, t2));
          match inferPattern env t1 p1 with
          | (env1, tp1) ->
              (match inferPattern env1 t2 p2 with
               | (env2, tp2) -> (env2, TPTuple (tp1, tp2)))))
    | PUnion (c, None) ->
        (match assoc c ctorEnv with
         | Some (tname, nullary, payload, tag) -> (unify scrutTy (TNamed tname); (env, TPUnion (c, None, payload)))
         | None -> failwith ("unknown case " + c))
    | PUnion (c, Some p) ->
        (match assoc c ctorEnv with
         | Some (tname, nullary, payload, tag) ->
             (unify scrutTy (TNamed tname);
              match inferPattern env payload p with
              | (env1, tp) -> (env1, TPUnion (c, Some tp, payload)))
         | None -> failwith ("unknown case " + c))
    | PCons (h, t) ->
        (let et = freshVar 0 in
         (unify scrutTy (TList et);
          match inferPattern env et h with
          | (env1, th) ->
              (match inferPattern env1 scrutTy t with
               | (env2, tt) -> (env2, TPCons (th, tt)))))

and inferRecordFields env name fields =
    match fields with
    | [] -> []
    | (f, v) :: rest ->
        (match assoc f (recordFieldsOf name recordDefs) with
         | Some ft ->
             (let tv = inferT env v in
              (unify (tyOf tv) ft; (f, tv) :: inferRecordFields env name rest))
         | None -> failwith ("unknown field " + f))

and inferDefs env defs =
    match defs with
    | [] -> (env, [])
    | DGroup members :: rest ->
        (let ftvs = allocFtvD [] members in
         let env1 = bindAllD env ftvs in
         let tds2 = inferEachD env1 ftvs [] members in
         let env2 = bindTdsD env tds2 in
         (match inferDefs env2 rest with
          | (env3, restTds) -> (env3, append tds2 restTds)))
    | Def (name, recFlag, mutableFlag, body) :: rest ->
        if name = "main" then
            (match body with
             | EFun (arg, b) ->
                 (let tb = inferT ((arg, Scheme ([], TList TStr)) :: env) b in
                  let t = tyOf tb in
                  let sc = Scheme ([], TFun (TList TStr, t)) in
                  let te = TEFun (arg, TFun (TList TStr, t), tb) in
                  (match inferDefs ((name, sc) :: env) rest with
                   | (env2, tds) -> (env2, TDef (name, sc, te) :: tds)))
             | _ ->
                 (let tb = inferT env body in
                  let t = tyOf tb in
                  let sc = Scheme ([], t) in
                  (match inferDefs ((name, sc) :: env) rest with
                   | (env2, tds) -> (env2, TDef (name, sc, tb) :: tds))))
        elif mutableFlag then
            (let tb = inferT env body in
             let t = tyOf tb in
             (mutableNames <- name :: mutableNames;
              let sc = Scheme ([], t) in
              match inferDefs ((name, sc) :: env) rest with
              | (env2, tds) -> (env2, TDef (name, sc, tb) :: tds)))
        elif recFlag then
            (let ftv = freshVar 0 in
             let env1 = (name, Scheme ([], ftv)) :: env in
             let tb = inferT env1 body in
             (unify ftv (tyOf tb);
              let t = tyOf tb in
              let sc = generalize env t in
              match inferDefs ((name, sc) :: env) rest with
              | (env2, tds) -> (env2, TDef (name, sc, tb) :: tds)))
        else
            (let tb = inferT env body in
             let t = tyOf tb in
             let sc = generalize env t in
             match inferDefs ((name, sc) :: env) rest with
             | (env2, tds) -> (env2, TDef (name, sc, tb) :: tds))

and inferArms env scrutTy arms =
    match arms with
    | [] -> []
    | (p, body) :: rest ->
        (match inferPattern env scrutTy p with
         | (env2, tp) ->
             let tb = inferT env2 body in
             (tp, tb) :: inferArms env scrutTy rest)

and unifyAll arms ty =
    match arms with
    | [] -> ()
    | (tp, tb) :: rest -> (unify ty (tyOf tb); unifyAll rest ty)

and allocFtvD acc ms =
    match ms with
    | [] -> acc
    | (n, _) :: t -> allocFtvD ((n, freshVar 0) :: acc) t

and bindAllD acc xs =
    match xs with
    | [] -> acc
    | (n, v) :: t -> bindAllD ((n, Scheme ([], v)) :: acc) t

and inferEachD env1 ftvs acc ms =
    match ms with
    | [] -> reverse [] acc
    | (n, body) :: t ->
        (let tb = inferT env1 body in
         (match assoc n ftvs with
          | Some ftv -> unify ftv (tyOf tb)
          | None -> ();
          inferEachD env1 ftvs (TDef (n, Scheme ([], tyOf tb), tb) :: acc) t))

and bindTdsD acc tds =
    match tds with
    | [] -> acc
    | TDef (n, sc, _) :: t -> bindTdsD ((n, sc) :: acc) t

// --- monomorphization ---

let mutable monoSchemes = []
let mutable monoOriginals = []
let mutable monoInsts = []
let mutable monoOut = []
let mutable monoUsed = []
let mutable monoCounter = 0

let rec schemesOf tds =
    match tds with
    | [] -> []
    | TDef (name, sc, body) :: rest -> (name, sc) :: schemesOf rest

let rec namesOfTds tds =
    match tds with
    | [] -> []
    | TDef (name, sc, body) :: rest -> name :: namesOfTds rest

let rec bodyOf name tds =
    match tds with
    | [] -> failwith "mono: missing def"
    | TDef (n, sc, b) :: rest -> if n = name then b else bodyOf name rest

let isPolyM name =
    match assoc name monoSchemes with
    | Some (Scheme (qs, _)) -> qs <> []
    | None -> false

let rec freshMonoLoop i name =
    let cand = name + "_s" + int_to_string i in
    if mem cand monoUsed then freshMonoLoop (i + 1) name
    else (monoUsed <- cand :: monoUsed; cand)

let freshMono name =
    (monoCounter <- monoCounter + 1;
     freshMonoLoop monoCounter name)

let rec resolveM t =
    match t with
    | TVar n -> TInt
    | TFun (a, b) -> TFun (resolveM a, resolveM b)
    | TList e -> TList (resolveM e)
    | TTuple (a, b) -> TTuple (resolveM a, resolveM b)
    | TOption e -> TOption (resolveM e)
    | _ -> t

let rtM subst t = resolveM (substTyVars subst (prune t))

let rec matchTy pat inst acc =
    match (prune pat, inst) with
    | (TVar q, t) -> (q, t) :: acc
    | (TFun (a, b), TFun (c, d)) -> matchTy b d (matchTy a c acc)
    | (TList a, TList b) -> matchTy a b acc
    | (TTuple (a1, a2), TTuple (b1, b2)) -> matchTy a2 b2 (matchTy a1 b1 acc)
    | (TOption a, TOption b) -> matchTy a b acc
    | _ -> acc

let rec specE subst te =
    match te with
    | TEInt n -> TEInt n
    | TEBool b -> TEBool b
    | TEStr s -> TEStr s
    | TENil t -> TENil (rtM subst t)
    | TEVar (x, t) ->
        (let t2 = rtM subst t in
         if isPolyM x then TEVar (specialize x t2, t2)
         else TEVar (x, t2))
    | TEBin (op, a, b, t) -> TEBin (op, specE subst a, specE subst b, rtM subst t)
    | TELet (x, bt, v, body, r) -> TELet (x, rtM subst bt, specE subst v, specE subst body, rtM subst r)
    | TEIf (c, t, e, r) -> TEIf (specE subst c, specE subst t, specE subst e, rtM subst r)
    | TEFun (p, ft, body) -> TEFun (p, rtM subst ft, specE subst body)
    | TEApp (f, a, r) ->
        (match f with
         | TEVar (x, ft) ->
             if isPolyM x then
                 (let ft2 = rtM subst ft in
                  TEApp (TEVar (specialize x ft2, ft2), specE subst a, rtM subst r))
             else TEApp (specE subst f, specE subst a, rtM subst r)
         | _ -> TEApp (specE subst f, specE subst a, rtM subst r))
    | TECons (t, h, tl) -> TECons (rtM subst t, specE subst h, specE subst tl)
    | TETuple (a, b, t) -> TETuple (specE subst a, specE subst b, rtM subst t)
    | TERecord (t, fields) -> TERecord (rtM subst t, specFields subst fields)
    | TEField (t, e, f) -> TEField (rtM subst t, specE subst e, f)
    | TEMatch (r, scrut, arms) -> TEMatch (rtM subst r, specE subst scrut, specArms subst arms)
    | TENone t -> TENone (rtM subst t)
    | TESome (t, x) -> TESome (rtM subst t, specE subst x)
    | TEUnit -> TEUnit
    | TSeq (a, b, t) -> TSeq (specE subst a, specE subst b, rtM subst t)
    | TEAssign (x, e, t) -> TEAssign (x, specE subst e, rtM subst t)

and specFields subst fields =
    match fields with
    | [] -> []
    | (f, v) :: rest -> (f, specE subst v) :: specFields subst rest

and specArms subst arms =
    match arms with
    | [] -> []
    | (p, body) :: rest -> (specP subst p, specE subst body) :: specArms subst rest

and specP subst p =
    match p with
    | TPVar (x, t) -> TPVar (x, rtM subst t)
    | TPCons (h, t) -> TPCons (specP subst h, specP subst t)
    | TPTuple (a, b) -> TPTuple (specP subst a, specP subst b)
    | TPUnion (c, Some p, t) -> TPUnion (c, Some (specP subst p), rtM subst t)
    | TPUnion (c, None, t) -> TPUnion (c, None, rtM subst t)
    | TPNone -> TPNone
    | TPSome p -> TPSome (specP subst p)
    | TPStr s -> TPStr s
    | TPWildcard -> TPWildcard
    | TPInt n -> TPInt n
    | TPNil -> TPNil

and specialize name instTy =
    (match assoc (name, instTy) monoInsts with
     | Some c -> c
     | None ->
         (match assoc name monoSchemes with
          | Some (Scheme (qs, bodyTy)) ->
              (let cname = freshMono name in
               (monoInsts <- ((name, instTy), cname) :: monoInsts;
                let s2 = matchTy bodyTy instTy [] in
                let b2 = specE s2 (bodyOf name monoOriginals) in
                (monoOut <- append monoOut [ TDef (cname, Scheme ([], instTy), b2) ];
                 cname)))
          | None -> name))

let rec emitMono tds =
    match tds with
    | [] -> ()
    | TDef (name, sc, body) :: rest ->
        (if schemeVars sc = [] then
             (let t = resolveM (prune (schemeBody sc)) in
              let b = specE [] body in
              monoOut <- append monoOut [ TDef (name, Scheme ([], t), b) ])
         else ();
         emitMono rest)

let monomorphize tds te =
    (monoSchemes <- schemesOf tds;
     monoOriginals <- tds;
     monoInsts <- [];
     monoOut <- [];
     monoUsed <- namesOfTds tds;
     monoCounter <- 0;
     emitMono tds;
     let te2 = specE [] te in
     (monoOut, te2))

// --- code generation ---

let mutable tmpN = 0
let mutable thunkN = 0
let mutable typedefs = ""
let mutable thunks = ""
let mutable emittedLists = []
let mutable emittedTuples = []
let mutable emittedOptions = []
let mutable funNames = []
let mutable eqSet = []
let mutable eqFwds = ""
let mutable eqBuf = ""

let fresh dummy = (tmpN <- tmpN + 1; "__t" + int_to_string tmpN)

let rec sanitize s =
    if str_len s = 0 then ""
    else (if str_get s 0 = 42 then "ptr" + sanitize (str_sub s 1 (str_len s - 1))
          else str_sub s 0 1 + sanitize (str_sub s 1 (str_len s - 1)))

let rec tupleName a b =
    let an = sanitize (ctype a) in
    let bn = sanitize (ctype b) in
    let name = "tuple_" + an + "_" + bn in
    (if not (mem name emittedTuples) then
        (emittedTuples <- name :: emittedTuples;
         typedefs <- typedefs + "typedef struct { " + ctype a + " a0; " + ctype b + " a1; } " + name + ";\n")
     else ();
     name)

and listName t =
    let en = sanitize (ctype t) in
    let name = "fslist_" + en in
    (if not (mem name emittedLists) then
        (emittedLists <- name :: emittedLists;
         typedefs <- typedefs + "typedef struct " + name + " { " + ctype t + " head; struct " + name + "* tail; } " + name + ";\n")
     else ();
     name)

and optionName t =
    let en = sanitize (ctype t) in
    let name = "fsoption_" + en in
    (if not (mem name emittedOptions) then
        (emittedOptions <- name :: emittedOptions;
         typedefs <- typedefs + "typedef struct " + name + " { " + ctype t + " payload; } " + name + ";\n")
     else ();
     name)

and unionName n =
    "fs_" + n

and ctype t =
    match prune t with
    | TInt -> "int32_t"
    | TBool -> "bool"
    | TStr -> "fsstring_t"
    | TUnit -> "int32_t"
    | TVar n -> "int32_t"
    | TFun (a, r) -> "closure_t"
    | TList e -> listName e + "*"
    | TTuple (a, b) -> tupleName a b
    | TNamed n -> unionName n + "*"
    | TOption e -> optionName e + "*"

let copOf op =
    if op = "=" then "=="
    elif op = "<>" then "!="
    else op

let strcmpOp op a b =
    if op = "=" then "fs_str_eq(" + a + ", " + b + ")"
    elif op = "<>" then "(!fs_str_eq(" + a + ", " + b + "))"
    elif op = "<" then "(fs_str_cmp(" + a + ", " + b + ") < 0)"
    elif op = "<=" then "(fs_str_cmp(" + a + ", " + b + ") <= 0)"
    elif op = ">" then "(fs_str_cmp(" + a + ", " + b + ") > 0)"
    else "(fs_str_cmp(" + a + ", " + b + ") >= 0)"

let rec removeAll x xs =
    match xs with
    | [] -> []
    | h :: t -> if h = x then removeAll x t else h :: removeAll x t

let rec ctorNamesOf xs =
    match xs with
    | [] -> []
    | (c, _) :: rest -> c :: ctorNamesOf rest

let rec fv te =
    match te with
    | TEInt n -> []
    | TEBool b -> []
    | TEStr s -> []
    | TENil t -> []
    | TEVar (x, t) -> if mem x builtinNames || mem x (ctorNamesOf ctorEnv) then [] else [ x ]
    | TEBin (op, a, b, t) -> union (fv a) (fv b)
    | TELet (x, bt, v, body, rt) -> union (fv v) (removeAll x (fv body))
    | TEIf (c, t, e, rt) -> union (fv c) (union (fv t) (fv e))
    | TEFun (p, ft, body) -> removeAll p (fv body)
    | TEApp (f, a, rt) -> union (fv f) (fv a)
    | TECons (t, h, tl) -> union (fv h) (fv tl)
    | TETuple (a, b, t) -> union (fv a) (fv b)
    | TERecord (t, fields) -> fvFields fields
    | TEField (t, e, f) -> fv e
    | TEMatch (rt, scrut, arms) -> union (fv scrut) (fvArms arms)
    | TENone t -> []
    | TESome (t, x) -> fv x
    | TEUnit -> []
    | TSeq (a, b, t) -> union (fv a) (fv b)
    | TEAssign (x, e, t) -> fv e

and fvFields fields =
    match fields with
    | [] -> []
    | (f, v) :: rest -> union (fv v) (fvFields rest)

and fvArms arms =
    match arms with
    | [] -> []
    | (p, body) :: rest -> union (removePats (patVars p) (fv body)) (fvArms rest)

and patVars p =
    match p with
    | TPVar (x, t) -> [ x ]
    | TPCons (h, t) -> union (patVars h) (patVars t)
    | TPTuple (a, b) -> union (patVars a) (patVars b)
    | TPUnion (c, Some p, t) -> patVars p
    | TPSome p -> patVars p
    | TPStr s -> []
    | _ -> []

and removePats xs ys =
    match xs with
    | [] -> ys
    | h :: t -> removePats t (removeAll h ys)

let rec lookupC x scope =
    match scope with
    | [] -> "v_" + x
    | (k, t, c) :: rest -> if k = x then c else lookupC x rest

let rec lookupTy x scope =
    match scope with
    | [] -> TInt
    | (k, t, c) :: rest -> if k = x then t else lookupTy x rest

let rec filterGlobalCaps scope vs =
    match vs with
    | [] -> []
    | v :: rest ->
        if mem v mutableNames && lookupC v scope = "v_" + v then filterGlobalCaps scope rest
        else v :: filterGlobalCaps scope rest

let rec join xs =
    match xs with
    | [] -> ""
    | [ x ] -> x
    | h :: t -> h + " " + join t

let rec joinComma xs =
    match xs with
    | [] -> ""
    | [ x ] -> x
    | h :: t -> h + ", " + joinComma t

let rec fieldsOf scope i vs =
    match vs with
    | [] -> []
    | v :: rest -> (ctype (lookupTy v scope) + " a" + int_to_string i + ";") :: fieldsOf scope (i + 1) rest

let rec initsOf ev scope i vs =
    match vs with
    | [] -> []
    | v :: rest ->
        (let rhs =
             if mem v funNames then "(closure_t){ .fn = (void*)v_" + v + ", .env = NULL }"
             else lookupC v scope in
         (ev + "->a" + int_to_string i + " = " + rhs + ";") :: initsOf ev scope (i + 1) rest)

let rec captureScope ev scope i vs acc =
    match vs with
    | [] -> reverse [] acc
    | v :: rest -> captureScope ev scope (i + 1) rest ((v, lookupTy v scope, ev + "->a" + int_to_string i) :: acc)

let rec tagOf c =
    match assoc c ctorEnv with
    | Some (tname, nullary, payload, tag) -> tag
    | None -> -1

let rec funNamesOf tds =
    match tds with
    | [] -> []
    | TDef (name, t, body) :: rest ->
        (match prune (schemeBody t) with
         | TFun (a, r) -> name :: funNamesOf rest
         | _ -> funNamesOf rest)

let rec scopeOf tds =
    match tds with
    | [] -> []
    | TDef (name, t, body) :: rest -> (name, schemeBody t, "v_" + name) :: scopeOf rest

let rec namesOf acc xs =
    match xs with
    | [] -> acc
    | (c, _) :: rest -> namesOf (c :: acc) rest

let rec ctorTypesOf acc xs =
    match xs with
    | [] -> acc
    | (c, (tname, nullary, payload, tag)) :: rest ->
        (let ty = if nullary then TNamed tname else TFun (payload, TNamed tname) in
         ctorTypesOf ((c, Scheme ([], ty)) :: acc) rest)

let rec cg scope te =
    match te with
    | TEInt n -> int_to_string n
    | TEBool b -> if b then "true" else "false"
    | TEStr s -> cstrOf s
    | TENil t -> "NULL"
    | TENone t -> "NULL"
    | TEUnit -> "0"
    | TSeq (a, b, t) -> "({ " + cg scope a + "; " + cg scope b + "; })"
    | TEAssign (x, e, t) -> "({ v_" + x + " = " + cg scope e + "; 0; })"
    | TESome (t, x) ->
        (let on = optionName (match prune t with TOption e -> e | _ -> TInt) in
         let nv = fresh 0 in
         "({ " + on + "* " + nv + " = (" + on + "*)malloc(sizeof(" + on + ")); " + nv + "->payload = " + cg scope x + "; " + nv + "; })")
    | TEVar (x, t) ->
        (match assoc x ctorEnv with
         | Some (tname, nullary, payload, tag) ->
             if nullary then
                 (let un = unionName tname in
                  let tv = fresh 0 in
                  "({ " + un + "* " + tv + " = (" + un + "*)malloc(sizeof(" + un + ")); " + tv + "->tag = " + int_to_string tag + "; " + tv + "; })")
             else lookupC x scope
         | None -> lookupC x scope)
    | TEBin (op, a, b, t) ->
        if t = TStr then "fs_str_concat(" + cg scope a + ", " + cg scope b + ")"
        elif t = TBool && op = "&&" then "(" + cg scope a + " && " + cg scope b + ")"
        elif t = TBool && op = "||" then "(" + cg scope a + " || " + cg scope b + ")"
        elif t = TBool && (op = "=" || op = "<>") && isDataTy (tyOf a) then
            (let e = eqCall (tyOf a) (cg scope a) (cg scope b) in
             if op = "=" then e else "(!" + e + ")")
        elif t = TBool && prune (tyOf a) = TStr then strcmpOp op (cg scope a) (cg scope b)
        elif t = TBool then "(" + cg scope a + " " + copOf op + " " + cg scope b + ")"
        else "(" + cg scope a + " " + op + " " + cg scope b + ")"
    | TELet (x, bt, v, body, rt) ->
        "({ " + ctype bt + " v_" + x + " = " + cg scope v + "; " + cg ((x, bt, "v_" + x) :: scope) body + "; })"
    | TEIf (c, t, e, rt) ->
        (let tv = fresh 0 in
         "({ " + ctype rt + " " + tv + "; if (" + cg scope c + ") " + tv + " = " + cg scope t + "; else " + tv + " = " + cg scope e + "; " + tv + "; })")
    | TETuple (a, b, t) ->
        (let tn = tupleName (tyOf a) (tyOf b) in
         "(" + tn + "){ .a0 = " + cg scope a + ", .a1 = " + cg scope b + " }")
    | TERecord (t, fields) ->
        (let un = unionName (match prune t with TNamed n -> n | _ -> "") in
         let tv = fresh 0 in
         let inits = join (recInits scope tv fields) in
         "({ " + un + "* " + tv + " = (" + un + "*)malloc(sizeof(" + un + ")); " + inits + " " + tv + "; })")
    | TEField (t, e, f) -> "(" + cg scope e + ")->f_" + f
    | TECons (t, h, tl) ->
        (let ln = listName (match prune t with TList e -> e | _ -> TInt) in
         let nv = fresh 0 in
         "({ " + ln + "* " + nv + " = (" + ln + "*)malloc(sizeof(" + ln + ")); " + nv + "->head = " + cg scope h + "; " + nv + "->tail = " + cg scope tl + "; " + nv + "; })")
    | TEMatch (rt, scrut, arms) ->
        (let st = ctype (tyOf scrut) in
         let sn = fresh 0 in
         let rv = fresh 0 in
         "({ " + st + " " + sn + " = " + cg scope scrut + "; " + ctype rt + " " + rv + "; " + matchArms scope sn (Some rv) arms + " " + rv + "; })")
    | TEFun (p, ft, body) ->
        (match ft with
         | TFun (argTy, retTy) ->
             let fvs = filterGlobalCaps scope (removeAll p (fv body)) in
             let id = (thunkN <- thunkN + 1; thunkN) in
             let envTy = "env_" + int_to_string id in
             let ev = "e" + int_to_string id in
             let fields = match fvs with [] -> "int32_t dummy" | _ -> join (fieldsOf scope 0 fvs) in
             let thunkScope = (p, argTy, "p") :: captureScope ev scope 0 fvs [] in
             let bodyStr = cg thunkScope body in
             let inits = match fvs with [] -> "" | _ -> join (initsOf ev scope 0 fvs) in
             (typedefs <- typedefs + "typedef struct { " + fields + " } " + envTy + ";\n";
              thunks <- thunks + "static " + ctype retTy + " thunk_" + int_to_string id + "(void* env, " + ctype argTy + " p) { " + envTy + "* " + ev + " = env; return " + bodyStr + "; }\n";
              "({ " + envTy + "* " + ev + " = malloc(sizeof(" + envTy + ")); " + inits + " (closure_t){ .fn = (void*)thunk_" + int_to_string id + ", .env = " + ev + " }; })")
         | _ -> failwith "bad function type")
    | TEApp (f, a, rt) ->
        (match collectBuiltin f [a] with
         | Some ("failwith", args) ->
             (match args with
              | msg :: _ -> "({ fs_failwith(" + cg scope msg + "); " + zeroOf rt + "; })"
              | [] -> "0")
         | Some (name, args) ->
             name + "(" + joinComma (cgList scope args) + ")"
         | None ->
             (match f with
              | TEVar (c, ft) ->
                  if mem c (ctorNames 0) then
                      (match assoc2 c ctorEnv with
                       | (tname, nullary, payload, tag) ->
                           (let un = unionName tname in
                            let tv = fresh 0 in
                            "({ " + un + "* " + tv + " = (" + un + "*)malloc(sizeof(" + un + ")); " + tv + "->tag = " + int_to_string tag + "; " + tv + "->u.u_" + c + " = " + cg scope a + "; " + tv + "; })"))
                  elif mem c funNames then
                      ("v_" + c + "(" + cg scope a + ")")
                  else
                      (let ct = ctype rt in
                       let at = ctype (tyOf a) in
                       let cv = fresh 0 in
                       "({ closure_t " + cv + " = " + cg scope f + "; ((" + ct + "(*)(void*, " + at + "))" + cv + ".fn)(" + cv + ".env, " + cg scope a + "); })")
              | _ ->
                  (let ct = ctype rt in
                   let at = ctype (tyOf a) in
                   let cv = fresh 0 in
                   "({ closure_t " + cv + " = " + cg scope f + "; ((" + ct + "(*)(void*, " + at + "))" + cv + ".fn)(" + cv + ".env, " + cg scope a + "); })")))

and ctorNames dummy = namesOf [] ctorEnv

and collectBuiltin f acc =
    match f with
    | TEVar (n, t) -> if mem n builtinNames then Some (n, acc) else None
    | TEApp (g, a, _) -> collectBuiltin g (a :: acc)
    | _ -> None

and cgList scope xs =
    match xs with
    | [] -> []
    | h :: t -> cg scope h :: cgList scope t

and cstrOf s =
    "(fsstring_t){ .len = " + int_to_string (str_len s) + ", .data = (char*)\"" + esc s 0 (str_len s) + "\" }"

and zeroOf t =
    match prune t with
    | TInt -> "0"
    | TBool -> "false"
    | TUnit -> "0"
    | TStr -> "(fsstring_t){0}"
    | TFun _ -> "(closure_t){0}"
    | TTuple (a, b) -> "(" + tupleName a b + "){0}"
    | TList _ -> "NULL"
    | TOption _ -> "NULL"
    | TNamed _ -> "NULL"
    | TVar n -> "0"

and isDataTy t =
    match prune t with
    | TList _ -> true
    | TOption _ -> true
    | TTuple _ -> true
    | TNamed _ -> true
    | _ -> false

and eqCall t a b =
    match prune t with
    | TInt -> a + " == " + b
    | TBool -> a + " == " + b
    | TUnit -> a + " == " + b
    | TStr -> "fs_str_eq(" + a + ", " + b + ")"
    | TList e -> eqList e a b + "(" + a + ", " + b + ")"
    | TOption e -> eqOption e a b + "(" + a + ", " + b + ")"
    | TTuple (x, y) -> eqTuple x y a b + "(" + a + ", " + b + ")"
    | TNamed n -> eqNamed n a b + "(" + a + ", " + b + ")"
    | TVar _ -> a + " == " + b
    | TFun _ -> a + " == " + b

and eqList e a b =
    let ln = listName e in
    let name = "eq_" + ln in
    (if not (mem name eqSet) then
        (eqSet <- name :: eqSet;
         eqFwds <- eqFwds + "static bool " + name + "(" + ln + "* a, " + ln + "* b);\n";
         let ee = eqCall e "a->head" "b->head" in
         eqBuf <- eqBuf + "static bool " + name + "(" + ln + "* a, " + ln + "* b) { if (a == NULL || b == NULL) return a == b; return " + ee + " && " + name + "(a->tail, b->tail); }\n")
     else ();
     name)

and eqOption e a b =
    let on = optionName e in
    let name = "eq_" + on in
    (if not (mem name eqSet) then
        (eqSet <- name :: eqSet;
         eqFwds <- eqFwds + "static bool " + name + "(" + on + "* a, " + on + "* b);\n";
         let ee = eqCall e "a->payload" "b->payload" in
         eqBuf <- eqBuf + "static bool " + name + "(" + on + "* a, " + on + "* b) { if (a == NULL || b == NULL) return a == b; return " + ee + "; }\n")
     else ();
     name)

and eqTuple x y a b =
    let tn = tupleName x y in
    let name = "eq_" + tn in
    (if not (mem name eqSet) then
        (eqSet <- name :: eqSet;
         eqFwds <- eqFwds + "static bool " + name + "(" + tn + " a, " + tn + " b);\n";
         let e1 = eqCall x "a.a0" "b.a0" in
         let e2 = eqCall y "a.a1" "b.a1" in
         eqBuf <- eqBuf + "static bool " + name + "(" + tn + " a, " + tn + " b) { return " + e1 + " && " + e2 + "; }\n")
     else ();
     name)

and eqNamed n a b =
    let un = unionName n in
    let name = "eq_" + un in
    (if not (mem name eqSet) then
        (eqSet <- name :: eqSet;
         eqFwds <- eqFwds + "static bool " + name + "(" + un + "* a, " + un + "* b);\n";
         let body = eqNamedBody n in
         eqBuf <- eqBuf + "static bool " + name + "(" + un + "* a, " + un + "* b) { if (a->tag != b->tag) return false; " + body + " return true; }\n")
     else ();
     name)

and eqNamedBody n =
    match assoc n typeDefs with
    | Some cases -> eqUnionBody cases
    | None ->
        (match assoc n recordDefs with
         | Some fields -> eqRecordBody fields
         | None -> "")

and eqUnionBody cases =
    match cases with
    | [] -> ""
    | (c, nullary, payload) :: rest ->
        (let this =
             if nullary then "if (a->tag == " + int_to_string (tagOf c) + ") return true;"
             else "if (a->tag == " + int_to_string (tagOf c) + ") return " + eqCall payload ("a->u.u_" + c) ("b->u.u_" + c) + ";" in
         this + " " + eqUnionBody rest)

and eqRecordConds fields =
    match fields with
    | [] -> []
    | (f, ft) :: rest -> eqCall ft ("a->f_" + f) ("b->f_" + f) :: eqRecordConds rest

and eqRecordBody fields =
    match eqRecordConds fields with
    | [] -> ""
    | conds -> "return " + joinAnd conds + ";"

and joinAnd xs =
    match xs with
    | [] -> "true"
    | [ x ] -> x
    | h :: t -> h + " && " + joinAnd t

and assoc2 key xs =
    match xs with
    | [] -> ("", false, TInt, -1)
    | (k, v) :: rest -> if k = key then v else assoc2 key rest

and recInits scope tv fields =
    match fields with
    | [] -> []
    | (f, v) :: rest -> (tv + "->f_" + f + " = " + cg scope v + ";") :: recInits scope tv rest

and matchArms scope sn rv arms =
    match arms with
    | [] -> ""
    | (p, body) :: rest ->
        (match patternTest sn p with
         | (cond, binds) ->
             let bodyStr =
                 match rv with
                 | Some r -> r + " = " + cg (append binds scope) body + ";"
                 | None -> "" in
             let restStr = matchArms scope sn rv rest in
             if restStr = "" then "if (" + cond + ") { " + bodyStr + " }"
             else "if (" + cond + ") { " + bodyStr + " } else { " + restStr + " }")

and patternTest sn p =
    match p with
    | TPWildcard -> ("1", [])
    | TPVar (x, t) -> ("1", [ (x, t, sn) ])
    | TPInt n -> (sn + " == " + int_to_string n, [])
    | TPStr s -> ("fs_str_eq(" + sn + ", " + cstrOf s + ")", [])
    | TPNil -> (sn + " == NULL", [])
    | TPNone -> (sn + " == NULL", [])
    | TPSome p ->
        (match patternTest (sn + "->payload") p with
         | (pc, pb) -> (sn + " != NULL && " + pc, pb))
    | TPTuple (p1, p2) ->
        (match patternTest (sn + ".a0") p1 with
         | (c1, b1) ->
             (match patternTest (sn + ".a1") p2 with
              | (c2, b2) -> (c1 + " && " + c2, append b1 b2)))
    | TPUnion (c, None, payload) -> (sn + "->tag == " + int_to_string (tagOf c), [])
    | TPUnion (c, Some p, payload) ->
        (match patternTest (sn + "->u.u_" + c) p with
         | (pc, pb) -> (sn + "->tag == " + int_to_string (tagOf c) + " && " + pc, pb))
    | TPCons (h, t) ->
        (match patternTest (sn + "->head") h with
         | (hc, hb) ->
             (match patternTest (sn + "->tail") t with
              | (tc, tb) -> (sn + " != NULL && " + hc + " && " + tc, append hb tb)))

and esc s i n =
    if i >= n then ""
    else (let c = str_get s i in
          if c = 34 then "\\\"" + esc s (i + 1) n
          elif c = 92 then "\\\\" + esc s (i + 1) n
          elif c = 10 then "\\n" + esc s (i + 1) n
          elif c = 9 then "\\t" + esc s (i + 1) n
          elif c = 13 then "\\r" + esc s (i + 1) n
          else str_sub s i 1 + esc s (i + 1) n)

and emitFuncFwds tds =
    match tds with
    | [] -> ""
    | TDef (name, t, body) :: rest ->
        (match prune (schemeBody t) with
         | TFun (argTy, retTy) -> "static " + ctype retTy + " v_" + name + "(" + ctype argTy + " p);\n" + emitFuncFwds rest
         | _ -> emitFuncFwds rest)

and emitFuncDefs scope0 tds =
    match tds with
    | [] -> ""
    | TDef (name, t, body) :: rest ->
        (match (prune (schemeBody t), body) with
         | (TFun (argTy, retTy), TEFun (p, ft, b)) ->
             (let bodyStr = cg ((p, argTy, "p") :: scope0) b in
              "static " + ctype retTy + " v_" + name + "(" + ctype argTy + " p) { return " + bodyStr + "; }\n" + emitFuncDefs scope0 rest)
         | _ -> emitFuncDefs scope0 rest)

and emitValDecls tds =
    match tds with
    | [] -> ""
    | TDef (name, t, body) :: rest ->
        (match prune (schemeBody t) with
         | TFun _ -> emitValDecls rest
         | _ -> "static " + ctype (schemeBody t) + " v_" + name + ";\n" + emitValDecls rest)

and emitValInits scope0 tds =
    match tds with
    | [] -> ""
    | TDef (name, t, body) :: rest ->
        (match prune (schemeBody t) with
         | TFun _ -> emitValInits scope0 rest
         | _ -> "v_" + name + " = " + cg scope0 body + "; " + emitValInits scope0 rest)

let rec caseMembers cases =
    match cases with
    | [] -> []
    | (c, _, payload) :: rest -> (ctype payload + " u_" + c + ";") :: caseMembers rest

and unionFwdsOf tds =
    match tds with
    | [] -> ()
    | (name, _) :: rest ->
        (let un = unionName name in
         typedefs <- typedefs + "typedef struct " + un + " " + un + ";\n";
         unionFwdsOf rest)

and unionBodiesOf tds =
    match tds with
    | [] -> ()
    | (name, cases) :: rest ->
        (let un = unionName name in
         let members = join (caseMembers cases) in
         typedefs <- typedefs + "struct " + un + " { int32_t tag; union { " + members + " } u; };\n";
         unionBodiesOf rest)

and emitUnionFwds dummy = unionFwdsOf typeDefs
and emitUnionBodies dummy = unionBodiesOf typeDefs

let rec recordMembers fields =
    match fields with
    | [] -> []
    | (f, ft) :: rest -> (ctype ft + " f_" + f + ";") :: recordMembers rest

and recordFwdsOf rds =
    match rds with
    | [] -> ()
    | (name, _) :: rest ->
        (let un = unionName name in
         typedefs <- typedefs + "typedef struct " + un + " " + un + ";\n";
         recordFwdsOf rest)

and recordBodiesOf rds =
    match rds with
    | [] -> ()
    | (name, fields) :: rest ->
        (let un = unionName name in
         let members = join (recordMembers fields) in
         typedefs <- typedefs + "struct " + un + " { " + members + " };\n";
         recordBodiesOf rest)

and emitRecordFwds dummy = recordFwdsOf recordDefs
and emitRecordBodies dummy = recordBodiesOf recordDefs

let runtimePre =
        "#include <stdint.h>\n" +
        "#include <stdbool.h>\n" +
        "#include <stdio.h>\n" +
        "#include <stdlib.h>\n" +
        "#include <string.h>\n" +
        "\n" +
        "typedef struct { int32_t len; char* data; } fsstring_t;\n" +
        "typedef struct { void* fn; void* env; } closure_t;\n" +
        "\n" +
        "static fsstring_t fs_str_concat(fsstring_t a, fsstring_t b) {\n" +
        "    fsstring_t r; r.len = a.len + b.len;\n" +
        "    r.data = (char*)malloc((size_t)r.len + 1);\n" +
        "    memcpy(r.data, a.data, (size_t)a.len);\n" +
        "    memcpy(r.data + a.len, b.data, (size_t)b.len);\n" +
        "    r.data[r.len] = 0; return r;\n" +
        "}\n" +
        "\n" +
        "static int32_t fs_str_cmp(fsstring_t a, fsstring_t b) {\n" +
        "    int32_t n = a.len < b.len ? a.len : b.len;\n" +
        "    int c = memcmp(a.data, b.data, (size_t)n);\n" +
        "    if (c != 0) return c < 0 ? -1 : 1;\n" +
        "    return a.len == b.len ? 0 : (a.len < b.len ? -1 : 1);\n" +
        "}\n" +
        "\n" +
        "static bool fs_str_eq(fsstring_t a, fsstring_t b) {\n" +
        "    return a.len == b.len && (a.len == 0 || memcmp(a.data, b.data, (size_t)a.len) == 0);\n" +
        "}\n" +
        "\n" +
        "static int32_t print_int(int32_t x) { printf(\"%d\", x); return 0; }\n" +
        "static int32_t print_bool(bool b) { fputs(b ? \"true\" : \"false\", stdout); return 0; }\n" +
        "static int32_t print_string(fsstring_t s) { fwrite(s.data, 1, (size_t)s.len, stdout); return 0; }\n" +
        "static int32_t print_newline(int32_t u) { fputc('\\n', stdout); return 0; }\n" +
        "static int32_t str_len(fsstring_t s) { return s.len; }\n" +
        "static int32_t str_get(fsstring_t s, int32_t i) { if (i < 0 || i >= s.len) { fputs(\"str_get: index out of range\\n\", stderr); exit(1); } return (int32_t)(unsigned char)s.data[i]; }\n" +
        "static fsstring_t str_sub(fsstring_t s, int32_t start, int32_t len) {\n" +
        "    if (start < 0) start = 0;\n" +
        "    if (len < 0) len = 0;\n" +
        "    if (start + len > s.len) len = s.len - start;\n" +
        "    if (len < 0) len = 0;\n" +
        "    fsstring_t r; r.len = len;\n" +
        "    r.data = (char*)malloc((size_t)len + 1);\n" +
        "    memcpy(r.data, s.data + start, (size_t)len);\n" +
        "    r.data[len] = 0; return r;\n" +
        "}\n" +
        "static int32_t int_of_string(fsstring_t s) { return (int32_t)atoi(s.data); }\n" +
        "static fsstring_t int_to_string(int32_t x) {\n" +
        "    char buf[16];\n" +
        "    int n = snprintf(buf, sizeof buf, \"%d\", x);\n" +
        "    fsstring_t r; r.len = n;\n" +
        "    r.data = (char*)malloc((size_t)n + 1);\n" +
        "    memcpy(r.data, buf, (size_t)n);\n" +
        "    r.data[n] = 0; return r;\n" +
        "}\n" +
        "static fsstring_t read_file(fsstring_t path) {\n" +
        "    char p[4096];\n" +
        "    if (path.len >= 4095) { fputs(\"read_file: path too long\\n\", stderr); exit(1); }\n" +
        "    memcpy(p, path.data, (size_t)path.len); p[path.len] = 0;\n" +
        "    FILE* fp = fopen(p, \"rb\");\n" +
        "    if (!fp) { fputs(\"read_file: cannot open \", stderr); fwrite(path.data, 1, (size_t)path.len, stderr); fputc('\\n', stderr); exit(1); }\n" +
        "    fseek(fp, 0, SEEK_END);\n" +
        "    long sz = ftell(fp);\n" +
        "    fseek(fp, 0, SEEK_SET);\n" +
        "    fsstring_t r; r.len = (int32_t)sz;\n" +
        "    r.data = (char*)malloc((size_t)sz + 1);\n" +
        "    if (sz > 0) fread(r.data, 1, (size_t)sz, fp);\n" +
        "    r.data[sz] = 0;\n" +
        "    fclose(fp); return r;\n" +
        "}\n" +
        "static void fs_failwith(fsstring_t msg) { fputs(\"error: \", stderr); fwrite(msg.data, 1, (size_t)msg.len, stderr); fputc('\\n', stderr); exit(1); }\n" +
        "\n"

let emitProgram tds te rt =
    let scope0 = scopeOf tds in
    (funNames <- funNamesOf tds;
     emitUnionFwds 0;
     emitRecordFwds 0;
     emitUnionBodies 0;
     emitRecordBodies 0;
     let e = cg scope0 te in
    let rt2 = prune rt in
    let body =
        if rt2 = TInt then "print_int(" + e + "); printf(\"\\n\");"
        elif rt2 = TStr then "print_string(" + e + "); printf(\"\\n\");"
        elif rt2 = TBool then "print_bool(" + e + "); printf(\"\\n\");"
        else "return 0;" in
    
    let fwds = emitFuncFwds tds in
    let fdefs = emitFuncDefs scope0 tds in
    let vdecls = emitValDecls tds in
    let vinits = emitValInits scope0 tds in
    runtimePre + typedefs + eqFwds + fwds + vdecls + thunks + fdefs + eqBuf + "int main(void) {\n    " + vinits + body + "\n    return 0;\n}\n")

let emitMainProgram tds =
    let scope0 = scopeOf tds in
    (funNames <- funNamesOf tds;
     emitUnionFwds 0;
     emitRecordFwds 0;
     emitUnionBodies 0;
     emitRecordBodies 0;
     let fwds = emitFuncFwds tds in
     let fdefs = emitFuncDefs scope0 tds in
     let vdecls = emitValDecls tds in
     let vinits = emitValInits scope0 tds in
     let ln = listName TStr in
     runtimePre + typedefs + eqFwds + fwds + vdecls + thunks + fdefs + eqBuf +
     "int main(int argc, char** argv) {\n" +
     "    " + vinits +
     "    " + ln + "* args = NULL;\n" +
     "    for (int i = argc - 1; i >= 0; i--) {\n" +
     "        fsstring_t s; s.len = (int32_t)strlen(argv[i]); s.data = argv[i];\n" +
     "        " + ln + "* n = (" + ln + "*)malloc(sizeof(" + ln + "));\n" +
     "        n->head = s; n->tail = args; args = n;\n" +
     "    }\n" +
     "    v_main(args);\n" +
     "    return 0;\n" +
     "}\n")

let rec hasMain defs =
    match defs with
    | [] -> false
    | Def ("main", _, _, _) :: _ -> true
    | _ :: rest -> hasMain rest

let rec ctorEnvTypes dummy = ctorTypesOf [] ctorEnv

let main argv =
    match argv with
    | _ :: f :: rest ->
        let s = read_file f in
        let toks = lex s in
        let toks2 = parseTypeDecls toks in
        (match parseDefs toks2 [] with
         | (defs, toks3) ->
             (match inferDefs (append builtinEnv (ctorEnvTypes 0)) defs with
              | (env0, tds) ->
                  if hasMain defs then
                      (match monomorphize tds TEUnit with
                       | (tds2, te2) -> print_string (emitMainProgram tds2))
                  else
                      (match parseExpr (skipSeps toks3) with
                       | (e, _) ->
                           let te = inferT env0 e in
                           (match monomorphize tds te with
                            | (tds2, te2) -> print_string (emitProgram tds2 te2 (tyOf te2))))))
    | _ -> (print_string "usage: compiler <file>"; print_newline ())

