// A type checker written in fss itself: lexes, parses, and infers a type.

type Token = TkInt of int | TIdent of string | TSym of string | TkStr of string | TEnd

type Expr =
    | EInt of int
    | EStr of string
    | EBool of bool
    | EVar of string
    | EBin of string * Expr * Expr
    | ELet of string * Expr * Expr
    | EIf of Expr * Expr * Expr

type Ty = TInt | TBool | TStr | TVar of int

let isSpace c = c = 32 || c = 9 || c = 10 || c = 13
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
          if c = 34 then i + 1 else scanStr s (i + 1) n)

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
              lexRev s k n (TkStr (str_sub s (j + 1) (k - j - 2)) :: acc))
         else
             (lexRev s (j + 1) n (TSym (str_sub s j 1) :: acc)))

let rec reverse acc xs =
    match xs with
    | [] -> acc
    | h :: t -> reverse (h :: acc) t

let lex s = reverse [] (lexRev s 0 (str_len s) [])

let precOf op =
    if op = "+" then 10
    elif op = "-" then 10
    elif op = "*" then 20
    elif op = "/" then 20
    else 0

let rec parse toks lhsOpt minPrec =
    match lhsOpt with
    | None ->
        (match toks with
         | TkInt n :: r -> parse r (Some (EInt n)) minPrec
         | TkStr s :: r -> parse r (Some (EStr s)) minPrec
         | TIdent x :: r ->
             if x = "true" then parse r (Some (EBool true)) minPrec
             elif x = "false" then parse r (Some (EBool false)) minPrec
             elif x = "let" then
                 (match r with
                  | TIdent name :: TSym "=" :: r2 ->
                      (match parse r2 None 0 with
                       | (v, r3) ->
                           (match r3 with
                            | TIdent "in" :: r4 ->
                                (match parse r4 None 0 with
                                 | (body, r5) -> parse r5 (Some (ELet (name, v, body))) minPrec)
                            | _ -> failwith "expected 'in'"))
                  | _ -> failwith "bad let")
             elif x = "if" then
                 (match parse r None 0 with
                  | (c, r1) ->
                      (match r1 with
                       | TIdent "then" :: r2 ->
                           (match parse r2 None 0 with
                            | (t, r3) ->
                                (match r3 with
                                 | TIdent "else" :: r4 ->
                                     (match parse r4 None 0 with
                                      | (e, r5) -> parse r5 (Some (EIf (c, t, e))) minPrec)
                                 | _ -> failwith "expected 'else'"))
                       | _ -> failwith "expected 'then'"))
             else
                 parse r (Some (EVar x)) minPrec
         | TSym "(" :: r ->
             (match parse r None 0 with
              | (e, r2) ->
                  (match r2 with
                   | TSym ")" :: r3 -> parse r3 (Some e) minPrec
                   | _ -> failwith "expected ')'"))
         | _ -> failwith "unexpected token")
    | Some lhs ->
        (match toks with
         | TSym op :: r ->
             if precOf op >= minPrec then
                 (match parse r None (precOf op + 1) with
                  | (rhs, r2) -> parse r2 (Some (EBin (op, lhs, rhs))) minPrec)
             else (lhs, toks)
         | _ -> (lhs, toks))

// --- unification ---

let mutable subst = []

let rec assoc key xs =
    match xs with
    | [] -> None
    | (k, v) :: rest -> if k = key then Some v else assoc key rest

let rec prune t =
    match t with
    | TVar n ->
        (match assoc n subst with
         | Some t2 -> (let p = prune t2 in (subst <- (n, p) :: subst; p))
         | None -> TVar n)
    | _ -> t

let rec occurs n t =
    match prune t with
    | TVar m -> m = n
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
             | _ -> failwith "type error: type mismatch")

// --- inference ---

let rec infer env e =
    match e with
    | EInt n -> TInt
    | EStr s -> TStr
    | EBool b -> TBool
    | EVar x -> (match assoc x env with | Some t -> t | None -> failwith ("type error: unbound " + x))
    | EBin (op, a, b) ->
        let ta = infer env a in
        let tb = infer env b in
        if op = "+" && ta = TStr && tb = TStr then TStr
        else (unify ta TInt; unify tb TInt; TInt)
    | ELet (x, v, body) ->
        (let tv = infer env v in
         infer ((x, tv) :: env) body)
    | EIf (c, t, e) ->
        (unify (infer env c) TBool;
         let tt = infer env t in
         let te = infer env e in
         (unify tt te; tt))

let rec tyToStr t =
    match prune t with
    | TInt -> "int"
    | TBool -> "bool"
    | TStr -> "string"
    | TVar n -> "t" + int_to_string n

let main argv =
    match argv with
    | _ :: f :: rest ->
        let s = read_file f in
        let toks = lex s in
        (match parse toks None 0 with
         | (e, _) -> (print_string (tyToStr (infer [] e)); print_newline ()))
    | _ -> (print_string "usage: typer <file>"; print_newline ())
