module Fss.Typer

open Fss.Ast

exception TypeError of string

/// Typed expression tree.
type TExpr =
    | TInt of int64
    | TFloat of float
    | TBool of bool
    | TUnit
    | TStr of string
    | TVar of string * Ty
    | TUnary of UnaryOp * Ty * TExpr
    | TBinary of BinOp * Ty * TExpr * TExpr
    | TIf of Ty * TExpr * TExpr * TExpr
    | TLet of string * Ty * TExpr * TExpr
    | TLetRec of string * Ty * TExpr * TExpr
    | TLetMutable of string * Ty * TExpr * TExpr
    | TAssign of string * TExpr
    | TWhile of TExpr * TExpr
    | TSeq of Ty * TExpr list
    | TFun of string * Ty * TExpr
    | TApply of Ty * TExpr * TExpr
    | TTuple of Ty * TExpr list
    | TRecord of Ty * (string * TExpr) list * TExpr option
    | TField of Ty * TExpr * string
    | TNil of Ty
    | TCons of Ty * TExpr * TExpr
    | TNone of Ty
    | TSome of Ty * TExpr
    | TMatch of Ty * TExpr * (TPattern * TExpr) list
    | TFailwith of Ty * TExpr
    | TTry of Ty * TExpr * (TPattern * TExpr) list

and TPattern =
    | TPWildcard
    | TPVar of string * Ty
    | TPInt of int64
    | TPBool of bool
    | TPUnit
    | TPStr of string
    | TPTuple of TPattern list
    | TPNil
    | TPCons of TPattern * TPattern
    | TPUnion of string * TPattern option * Ty

type Scheme = Scheme of int list * Ty

type Binding = { Scheme: Scheme; Mutable: bool }

/// Type-checker output; `Consts` still carry polymorphic schemes.
type TProgram =
    { Types: Map<string, Ast.TypeDef>
      Ctors: Map<string, string * Ty>
      Consts: (string * Scheme * TExpr * bool) list
      Main: TExpr option
      MainArg: string option
      Externs: (string * (string * Ty) list * Ty) list
      Includes: string list
      Links: string list }

let rec tyOf (e: TExpr) : Ty =
    match e with
    | TInt _ -> IntTy
    | TFloat _ -> DoubleTy
    | TBool _ -> BoolTy
    | TUnit -> UnitTy
    | TStr _ -> StringTy
    | TVar (_, t) -> t
    | TUnary (_, t, _) -> t
    | TBinary (_, t, _, _) -> t
    | TIf (t, _, _, _) -> t
    | TLet (_, _, _, body) -> tyOf body
    | TLetRec (_, _, _, body) -> tyOf body
    | TLetMutable (_, _, _, body) -> tyOf body
    | TAssign _ -> UnitTy
    | TWhile _ -> UnitTy
    | TSeq (t, _) -> t
    | TFun (_, t, _) -> t
    | TApply (t, _, _) -> t
    | TTuple (t, _) -> t
    | TRecord (t, _, _) -> t
    | TField (t, _, _) -> t
    | TNil t -> t
    | TCons (t, _, _) -> t
    | TNone t -> t
    | TSome (t, _) -> t
    | TMatch (t, _, _) -> t
    | TFailwith (t, _) -> t
    | TTry (t, _, _) -> t

let private err msg = raise (TypeError msg)

// --- Unification -----------------------------------------------------------

let mutable private nextVar = 0
let mutable private subst : Map<int, Ty> = Map.empty

let private freshVar () =
    nextVar <- nextVar + 1
    TyVar nextVar

let rec private prune (t: Ty) : Ty =
    match t with
    | TyVar n ->
        let found : Ty option = Map.tryFind n subst
        match found with
        | Some t2 ->
            let p = prune t2
            subst <- Map.add n p subst
            p
        | None -> TyVar n
    | _ -> t

let rec private occurs (n: int) (t: Ty) : bool =
    match prune t with
    | TyVar m -> m = n
    | FunTy (a, b) -> occurs n a || occurs n b
    | TNamed (_, args) -> List.exists (occurs n) args
    | TupleTy ts -> List.exists (occurs n) ts
    | _ -> false

let rec private unify (a: Ty) (b: Ty) : unit =
    let a = prune a
    let b = prune b
    if a = b then ()
    else
        match a, b with
        | TyVar n, t
        | t, TyVar n ->
            if occurs n t then err "infinite type"
            subst <- Map.add n t subst
        | FunTy (a1, a2), FunTy (b1, b2) ->
            unify a1 b1
            unify a2 b2
        | TNamed (n1, args1), TNamed (n2, args2) when n1 = n2 && List.length args1 = List.length args2 ->
            List.iter2 unify args1 args2
        | TupleTy ts1, TupleTy ts2 when List.length ts1 = List.length ts2 ->
            List.iter2 unify ts1 ts2
        | _ -> err ("type mismatch: " + tyToString a + " vs " + tyToString b)

let rec resolveTy (t: Ty) : Ty =
    match prune t with
    | TyVar _ -> IntTy
    | FunTy (a, b) -> FunTy(resolveTy a, resolveTy b)
    | TNamed (n, args) -> TNamed(n, List.map resolveTy args)
    | TupleTy ts -> TupleTy(List.map resolveTy ts)
    | t -> t

/// Fully resolve linked variables, leaving unbound variables as-is.
let rec zonk (t: Ty) : Ty =
    match prune t with
    | TyVar n -> TyVar n
    | FunTy (a, b) -> FunTy(zonk a, zonk b)
    | TNamed (n, args) -> TNamed(n, List.map zonk args)
    | TupleTy ts -> TupleTy(List.map zonk ts)
    | t -> t

let rec freeTyVars (t: Ty) : Set<int> =
    match prune t with
    | TyVar n -> Set.singleton n
    | FunTy (a, b) -> Set.union (freeTyVars a) (freeTyVars b)
    | TNamed (_, args) -> Set.unionMany (List.map freeTyVars args)
    | TupleTy ts -> Set.unionMany (List.map freeTyVars ts)
    | _ -> Set.empty

let rec substTyVars (m: Map<int, Ty>) (t: Ty) : Ty =
    match t with
    | TyVar n -> match Map.tryFind n m with Some t2 -> t2 | None -> TyVar n
    | FunTy (a, b) -> FunTy(substTyVars m a, substTyVars m b)
    | TNamed (n, args) -> TNamed(n, List.map (substTyVars m) args)
    | TupleTy ts -> TupleTy(List.map (substTyVars m) ts)
    | t -> t

let schemeBody (Scheme (_, t)) = t
let schemeVars (Scheme (qs, _)) = qs

let instantiate (Scheme (qs, body)) : Ty =
    if List.isEmpty qs then body
    else substTyVars (qs |> List.map (fun q -> q, freshVar ()) |> Map.ofList) body

let generalize (env: Map<string, Binding>) (ty: Ty) : Scheme =
    let ty = zonk ty
    let fv = freeTyVars ty
    let envFv =
        env
        |> Map.toSeq
        |> Seq.collect (fun (_, b) -> freeTyVars (schemeBody b.Scheme))
        |> Set.ofSeq
    Scheme(Set.difference fv envFv |> Set.toList, ty)

// --- Type definitions ------------------------------------------------------

let private builtinEnv : Map<string, Binding> =
    Map.ofList [
        "print_int", { Scheme = Scheme([], FunTy(IntTy, UnitTy)); Mutable = false }
        "print_bool", { Scheme = Scheme([], FunTy(BoolTy, UnitTy)); Mutable = false }
        "print_newline", { Scheme = Scheme([], FunTy(UnitTy, UnitTy)); Mutable = false }
        "print_string", { Scheme = Scheme([], FunTy(StringTy, UnitTy)); Mutable = false }
        "println", { Scheme = Scheme([999999], FunTy(TyVar 999999, UnitTy)); Mutable = false }
        "str_len", { Scheme = Scheme([], FunTy(StringTy, IntTy)); Mutable = false }
        "int_to_string", { Scheme = Scheme([], FunTy(IntTy, StringTy)); Mutable = false }
        "bool_to_string", { Scheme = Scheme([], FunTy(BoolTy, StringTy)); Mutable = false }
        "read_line", { Scheme = Scheme([], FunTy(UnitTy, StringTy)); Mutable = false }
        "read_file", { Scheme = Scheme([], FunTy(StringTy, StringTy)); Mutable = false }
        "write_file", { Scheme = Scheme([], FunTy(StringTy, FunTy(StringTy, UnitTy))); Mutable = false }
        "str_get", { Scheme = Scheme([], FunTy(StringTy, FunTy(IntTy, IntTy))); Mutable = false }
        "str_sub", { Scheme = Scheme([], FunTy(StringTy, FunTy(IntTy, FunTy(IntTy, StringTy)))); Mutable = false }
        "int_of_string", { Scheme = Scheme([], FunTy(StringTy, IntTy)); Mutable = false }
        "print_double", { Scheme = Scheme([], FunTy(DoubleTy, UnitTy)); Mutable = false }
        "double_to_string", { Scheme = Scheme([], FunTy(DoubleTy, StringTy)); Mutable = false }
        "int_to_double", { Scheme = Scheme([], FunTy(IntTy, DoubleTy)); Mutable = false }
        "double_to_int", { Scheme = Scheme([], FunTy(DoubleTy, IntTy)); Mutable = false }
    ]

let private buildCaseInfo (types: (string * TypeDef) list) : Map<string, string * Ty> =
    types
    |> List.collect (fun (n, d) ->
        match d with
        | UnionDef cases -> cases |> List.map (fun (c, t) -> c, (n, t))
        | _ -> [])
    |> Map.ofList

let private buildCtorEnv (types: (string * TypeDef) list) : Map<string, Binding> =
    types
    |> List.collect (fun (n, d) ->
        match d with
        | UnionDef cases ->
            cases
            |> List.map (fun (c, t) ->
                let ty =
                    if t = UnitTy then TNamed(n, [])
                    else FunTy(t, TNamed(n, []))
                c, { Scheme = Scheme([], ty); Mutable = false })
        | _ -> [])
    |> Map.ofList

let private validateTypes (types: (string * TypeDef) list) : unit =
    let rec dup xs =
        match xs with
        | [] -> None
        | x :: rest -> if List.contains x rest then Some x else dup rest

    match dup (List.map fst types) with
    | Some n -> err ("duplicate type definition '" + n + "'")
    | None -> ()

    let caseNames = types |> List.collect (fun (_, d) -> match d with UnionDef cs -> List.map fst cs | _ -> [])
    match dup caseNames with
    | Some c -> err ("duplicate union case '" + c + "'")
    | None -> ()

    let fieldNames = types |> List.collect (fun (_, d) -> match d with RecordDef fs -> List.map fst fs | _ -> [])
    match dup fieldNames with
    | Some f -> err ("duplicate record field '" + f + "'")
    | None -> ()

    match types |> List.tryPick (fun (n, d) -> match d with RecordDef [] -> Some n | _ -> None) with
    | Some n -> err ("record '" + n + "' has no fields")
    | None -> ()

    match types |> List.tryPick (fun (n, d) -> match d with UnionDef [] -> Some n | _ -> None) with
    | Some n -> err ("union '" + n + "' has no cases")
    | None -> ()

// --- Inference -------------------------------------------------------------

let rec private infer (env: Map<string, Binding>) (typeDefs: Map<string, TypeDef>) (caseInfo: Map<string, string * Ty>) (e: Expr) : (Ty * TExpr) =
    let fieldType (recName: string) (field: string) : Ty =
        match Map.tryFind recName typeDefs with
        | Some (RecordDef fields) ->
            match fields |> List.tryPick (fun (f, t) -> if f = field then Some t else None) with
            | Some t -> t
            | None -> err ("record '" + recName + "' has no field '" + field + "'")
        | _ -> err ("'" + recName + "' is not a record")

    let uniqueField (field: string) : string * Ty =
        let hits =
            typeDefs
            |> Map.toList
            |> List.choose (fun (n, d) ->
                match d with
                | RecordDef fields -> fields |> List.tryPick (fun (f, t) -> if f = field then Some(n, t) else None)
                | _ -> None)
        match hits with
        | [ h ] -> h
        | [] -> err ("unknown field '" + field + "'")
        | _ -> err ("ambiguous field '" + field + "'")

    match e with
    | Int n ->
        if n < int64 System.Int32.MinValue || n > int64 System.Int32.MaxValue then
            err ("integer literal " + string n + " is out of range for int (32-bit)")
        (IntTy, TInt n)

    | Float f -> (DoubleTy, TFloat f)

    | Str s -> (StringTy, TStr s)

    | Bool b -> (BoolTy, TBool b)
    | Unit -> (UnitTy, TUnit)

    | Var n ->
        match Map.tryFind n env with
        | Some b ->
            let t = instantiate b.Scheme
            (t, TVar(n, t))
        | None -> err ("unbound name '" + n + "'")

    | Unary (Neg, x) ->
        let (tx, txExpr) = infer env typeDefs caseInfo x
        let t = if prune tx = DoubleTy then DoubleTy else IntTy
        unify tx t
        (t, TUnary(Neg, t, txExpr))

    | Unary (Not, x) ->
        let (tx, txExpr) = infer env typeDefs caseInfo x
        unify tx BoolTy
        (BoolTy, TUnary(Not, BoolTy, txExpr))

    | Binary (op, a, b) ->
        let (ta, taExpr) = infer env typeDefs caseInfo a
        let (tb, tbExpr) = infer env typeDefs caseInfo b
        match op with
        | Add when (prune ta = StringTy || prune tb = StringTy) ->
            unify ta StringTy
            unify tb StringTy
            (StringTy, TBinary(Add, StringTy, taExpr, tbExpr))
        | Add | Sub | Mul | Div | Mod ->
            let t = if prune ta = DoubleTy || prune tb = DoubleTy then DoubleTy else IntTy
            if t = DoubleTy && op = Mod then err "the '%' operator is not defined for double"
            unify ta t
            unify tb t
            (t, TBinary(op, t, taExpr, tbExpr))
        | And | Or ->
            unify ta BoolTy
            unify tb BoolTy
            (BoolTy, TBinary(op, BoolTy, taExpr, tbExpr))
        | Eq | Ne ->
            unify ta tb
            match prune ta with
            | FunTy _ -> err "cannot compare functions"
            | _ -> ()
            (BoolTy, TBinary(op, BoolTy, taExpr, tbExpr))
        | Lt | Le | Gt | Ge ->
            let t = if prune ta = DoubleTy || prune tb = DoubleTy then DoubleTy else IntTy
            unify ta t
            unify tb t
            (BoolTy, TBinary(op, BoolTy, taExpr, tbExpr))

    | If (c, t, e) ->
        let (tc, tcExpr) = infer env typeDefs caseInfo c
        unify tc BoolTy
        let (tt, ttExpr) = infer env typeDefs caseInfo t
        let (te, teExpr) = infer env typeDefs caseInfo e
        unify tt te
        (tt, TIf(tt, tcExpr, ttExpr, teExpr))

    | Let (n, v, b) ->
        let (tv, tvExpr) = infer env typeDefs caseInfo v
        if prune tv = UnitTy then err ("cannot bind unit value to name '" + n + "' in v0.1")
        let (tb, tbExpr) = infer (Map.add n { Scheme = Scheme([], tv); Mutable = false } env) typeDefs caseInfo b
        (tb, TLet(n, tv, tvExpr, tbExpr))

    | LetRec (n, _, _) -> err ("local 'let rec' is not supported yet; move '" + n + "' to top level")

    | LetMutable (n, v, b) ->
        let (tv, tvExpr) = infer env typeDefs caseInfo v
        if prune tv = UnitTy then err ("cannot bind unit value to name '" + n + "' in v0.1")
        let (tb, tbExpr) = infer (Map.add n { Scheme = Scheme([], tv); Mutable = true } env) typeDefs caseInfo b
        (tb, TLetMutable(n, tv, tvExpr, tbExpr))

    | Assign (n, e) ->
        match Map.tryFind n env with
        | Some b when b.Mutable ->
            let (te, teExpr) = infer env typeDefs caseInfo e
            unify (schemeBody b.Scheme) te
            (UnitTy, TAssign(n, teExpr))
        | Some _ -> err ("'" + n + "' is not mutable")
        | None -> err ("unbound name '" + n + "'")

    | While (c, b) ->
        let (tc, tcExpr) = infer env typeDefs caseInfo c
        unify tc BoolTy
        let (tb, tbExpr) = infer env typeDefs caseInfo b
        unify tb UnitTy
        (UnitTy, TWhile(tcExpr, tbExpr))

    | Seq xs ->
        let txs = List.map (infer env typeDefs caseInfo) xs
        let t =
            match List.rev txs with
            | [] -> UnitTy
            | (lt, _) :: _ -> lt
        (t, TSeq(t, List.map snd txs))

    | Fun (param, body) ->
        let pt = freshVar ()
        let (bt, btExpr) = infer (Map.add param { Scheme = Scheme([], pt); Mutable = false } env) typeDefs caseInfo body
        (FunTy(pt, bt), TFun(param, FunTy(pt, bt), btExpr))

    | Apply (Var "failwith", arg) ->
        let (mt, mExpr) = infer env typeDefs caseInfo arg
        unify mt StringTy
        let rt = freshVar ()
        (rt, TFailwith(rt, mExpr))

    | Apply (f, arg) ->
        let (ft, ftExpr) = infer env typeDefs caseInfo f
        let (at, atExpr) = infer env typeDefs caseInfo arg
        let rt = freshVar ()
        unify ft (FunTy(at, rt))
        (rt, TApply(rt, ftExpr, atExpr))

    | Tuple xs ->
        let txs = List.map (infer env typeDefs caseInfo) xs
        let ty = TupleTy(List.map fst txs)
        (ty, TTuple(ty, List.map snd txs))

    | Record (None, fields) ->
        match fields with
        | [] -> err "empty record literal"
        | (f0, _) :: _ ->
            let (recName, _) = uniqueField f0
            let declared =
                match Map.tryFind recName typeDefs with
                | Some (RecordDef fs) -> fs |> List.map fst
                | _ -> []
            let provided = fields |> List.map fst
            let missing = declared |> List.filter (fun f -> not (List.contains f provided))
            if not missing.IsEmpty then
                err ("missing record field(s): " + String.concat ", " missing)
            let tfields =
                fields
                |> List.map (fun (f, v) ->
                    let ft = fieldType recName f
                    let (vt, vExpr) = infer env typeDefs caseInfo v
                    unify vt ft
                    (f, vExpr))
            (TNamed(recName, []), TRecord(TNamed(recName, []), tfields, None))

    | Record (Some baseE, fields) ->
        let (bt, btExpr) = infer env typeDefs caseInfo baseE
        let (recName, _) =
            match fields with
            | [] -> err "empty record update"
            | (f0, _) :: _ -> uniqueField f0
        unify bt (TNamed(recName, []))
        let tfields =
            fields
            |> List.map (fun (f, v) ->
                let ft = fieldType recName f
                let (vt, vExpr) = infer env typeDefs caseInfo v
                unify vt ft
                (f, vExpr))
        (TNamed(recName, []), TRecord(TNamed(recName, []), tfields, Some btExpr))

    | Field (target, f) ->
        let (tt, ttExpr) = infer env typeDefs caseInfo target
        let (recName, ft) = uniqueField f
        unify tt (TNamed(recName, []))
        (ft, TField(ft, ttExpr, f))

    | Nil ->
        let tv = freshVar ()
        (TNamed("list", [ tv ]), TNil(TNamed("list", [ tv ])))

    | Cons (h, t) ->
        let (ht, hExpr) = infer env typeDefs caseInfo h
        let (tt, tExpr) = infer env typeDefs caseInfo t
        unify tt (TNamed("list", [ ht ]))
        (tt, TCons(tt, hExpr, tExpr))

    | ENone ->
        let tv = freshVar ()
        (TNamed("option", [ tv ]), TNone(TNamed("option", [ tv ])))

    | ESome x ->
        let (xt, xExpr) = infer env typeDefs caseInfo x
        (TNamed("option", [ xt ]), TSome(TNamed("option", [ xt ]), xExpr))

    | Match (scrut, arms) ->
        let (st, stExpr) = infer env typeDefs caseInfo scrut
        let (rt, tArms) = inferArms env typeDefs caseInfo st arms
        (rt, TMatch(rt, stExpr, tArms))

    | Try (body, arms) ->
        let (bt, btExpr) = infer env typeDefs caseInfo body
        let tArms =
            arms
            |> List.map (fun (p, h) ->
                let (env', tp) = inferPattern env typeDefs caseInfo p StringTy
                let (ht, hExpr) = infer env' typeDefs caseInfo h
                (tp, ht, hExpr))
        match tArms with
        | [] -> err "empty try"
        | (_, ht0, _) :: rest ->
            unify bt ht0
            for (_, ht, _) in rest do
                unify bt ht
            (bt, TTry(bt, btExpr, tArms |> List.map (fun (tp, _, he) -> tp, he)))

and private inferPattern
    (env: Map<string, Binding>)
    (typeDefs: Map<string, TypeDef>)
    (caseInfo: Map<string, string * Ty>)
    (p: Pattern)
    (expected: Ty)
    : (Map<string, Binding> * TPattern) =

    let addVar n t e = Map.add n { Scheme = Scheme([], t); Mutable = false } e

    match p with
    | PWildcard -> (env, TPWildcard)

    | PVar "None" ->
        let ev = freshVar ()
        unify expected (TNamed("option", [ ev ]))
        (env, TPUnion("None", None, UnitTy))

    | PUnion ("None", Some _) -> err "'None' takes no payload pattern"

    | PUnion ("Some", Some p) ->
        let ev = freshVar ()
        unify expected (TNamed("option", [ ev ]))
        let (env', tp) = inferPattern env typeDefs caseInfo p ev
        (env', TPUnion("Some", Some tp, ev))

    | PVar n when Map.containsKey n caseInfo ->
        let (tname, payload) = caseInfo.[n]
        if payload = UnitTy then
            unify expected (TNamed(tname, []))
            (env, TPUnion(n, None, UnitTy))
        else
            err ("union case '" + n + "' expects a payload pattern")

    | PVar n ->
        (addVar n expected env, TPVar(n, expected))

    | PInt n ->
        unify expected IntTy
        (env, TPInt n)

    | PBool b ->
        unify expected BoolTy
        (env, TPBool b)

    | PUnit ->
        unify expected UnitTy
        (env, TPUnit)

    | PStr s ->
        unify expected StringTy
        (env, TPStr s)

    | PTuple ps ->
        let elemTys = ps |> List.map (fun _ -> freshVar ())
        unify expected (TupleTy elemTys)
        let (env', tps) =
            List.fold2
                (fun (e, acc) p t ->
                    let (e2, tp) = inferPattern e typeDefs caseInfo p t
                    (e2, acc @ [ tp ]))
                (env, []) ps elemTys
        (env', TPTuple tps)

    | PNil ->
        unify expected (TNamed("list", [ freshVar () ]))
        (env, TPNil)

    | PCons (h, t) ->
        let eh = freshVar ()
        unify expected (TNamed("list", [ eh ]))
        let (env1, th) = inferPattern env typeDefs caseInfo h eh
        let (env2, tt) = inferPattern env1 typeDefs caseInfo t expected
        (env2, TPCons(th, tt))

    | PUnion (case, payloadPat) ->
        match Map.tryFind case caseInfo with
        | Some (tname, payload) ->
            unify expected (TNamed(tname, []))
            match payloadPat with
            | None ->
                if payload = UnitTy then (env, TPUnion(case, None, payload))
                else err ("union case '" + case + "' expects a payload pattern")
            | Some p ->
                let (env', tp) = inferPattern env typeDefs caseInfo p payload
                (env', TPUnion(case, Some tp, payload))
        | None -> err ("unknown union case '" + case + "'")

and private inferArms
    (env: Map<string, Binding>)
    (typeDefs: Map<string, TypeDef>)
    (caseInfo: Map<string, string * Ty>)
    (scrutTy: Ty)
    (arms: (Pattern * Expr) list)
    : (Ty * (TPattern * TExpr) list) =

    let typed =
        arms
        |> List.map (fun (p, b) ->
            let (env', tp) = inferPattern env typeDefs caseInfo p scrutTy
            let (bt, btExpr) = infer env' typeDefs caseInfo b
            (tp, bt, btExpr))

    match typed with
    | [] -> err "empty match"
    | (_, firstTy, _) :: rest ->
        for (_, bt, _) in rest do
            unify firstTy bt
        checkExhaustive typeDefs caseInfo scrutTy arms
        (firstTy, typed |> List.map (fun (tp, _, be) -> (tp, be)))

and private checkExhaustive
    (typeDefs: Map<string, TypeDef>)
    (caseInfo: Map<string, string * Ty>)
    (scrutTy: Ty)
    (arms: (Pattern * Expr) list)
    : unit =

    let isCatchAll (p: Pattern) =
        match p with
        | PWildcard -> true
        | PVar n -> not (Map.containsKey n caseInfo)
        | _ -> false

    if List.exists (fst >> isCatchAll) arms then ()
    else
        match resolveTy scrutTy with
        | BoolTy ->
            let has b = arms |> List.exists (fun (p, _) -> match p with PBool x -> x = b | _ -> false)
            if not (has true && has false) then err "inexhaustive match on bool"
        | TNamed ("option", [ _ ]) ->
            let hasNone = arms |> List.exists (fun (p, _) -> match p with PUnion ("None", _) | PVar "None" -> true | _ -> false)
            let hasSome = arms |> List.exists (fun (p, _) -> match p with PUnion ("Some", _) -> true | _ -> false)
            if not (hasNone && hasSome) then err "inexhaustive match on option"
        | TNamed (tname, []) ->
            match Map.tryFind tname typeDefs with
            | Some (UnionDef cases) ->
                let missing =
                    cases
                    |> List.map fst
                    |> List.filter (fun c ->
                        not (arms |> List.exists (fun (p, _) ->
                            match p with
                            | PUnion (c', _) -> c' = c
                            | PVar n -> n = c
                            | _ -> false)))
                if not missing.IsEmpty then err ("inexhaustive match: missing case(s) " + String.concat ", " missing)
            | _ -> ()
        | _ -> ()

let check (prog: Program) : TProgram =
    subst <- Map.empty
    nextVar <- 0

    validateTypes prog.Types
    let typeDefs = Map.ofList prog.Types
    let caseInfo = buildCaseInfo prog.Types
    let ctorEnv = buildCtorEnv prog.Types

    let env0 =
        Map.fold (fun acc k v -> Map.add k v acc) builtinEnv ctorEnv

    let rec loop (env: Map<string, Binding>) (consts: (string * Scheme * TExpr * bool) list) (externs: (string * (string * Ty) list * Ty) list) (main: TExpr option) (mainArg: string option) : (Def list -> TProgram) =
        function
        | [] -> { Types = typeDefs; Ctors = caseInfo; Consts = List.rev consts; Main = main; MainArg = mainArg; Externs = List.rev externs; Includes = prog.Includes; Links = prog.Links }
        | Const (n, e) :: rest ->
            let (t, te) = infer env typeDefs caseInfo e
            if prune t = UnitTy then err ("top-level binding '" + n + "' must not have type unit")
            if Map.containsKey n env then err ("duplicate definition of '" + n + "'")
            let scheme = generalize env t
            loop (Map.add n { Scheme = scheme; Mutable = false } env) ((n, scheme, te, false) :: consts) externs main mainArg rest
        | ConstRec (n, e) :: rest ->
            if Map.containsKey n env then err ("duplicate definition of '" + n + "'")
            let ftv = freshVar ()
            let env' = Map.add n { Scheme = Scheme([], ftv); Mutable = false } env
            let (t, te) = infer env' typeDefs caseInfo e
            unify ftv t
            match resolveTy t with
            | FunTy _ -> ()
            | _ -> err ("'let rec' value '" + n + "' is not supported; use a function")
            let scheme = generalize env t
            loop (Map.add n { Scheme = scheme; Mutable = false } env) ((n, scheme, te, false) :: consts) externs main mainArg rest
        | RecGroup bindings :: rest ->
            for (n, _) in bindings do
                if Map.containsKey n env then err ("duplicate definition of '" + n + "'")
            let ftvs = bindings |> List.map (fun (n, _) -> n, freshVar ())
            let env' =
                List.fold (fun e (n, ftv) -> Map.add n { Scheme = Scheme([], ftv); Mutable = false } e) env ftvs
            let typed =
                bindings
                |> List.map (fun (n, expr) ->
                    let (t, te) = infer env' typeDefs caseInfo expr
                    let ftv = ftvs |> List.find (fun (m, _) -> m = n) |> snd
                    unify ftv t
                    (n, t, te))
            for (n, t, _) in typed do
                match resolveTy t with
                | FunTy _ -> ()
                | _ -> err ("'let rec' value '" + n + "' is not supported; use a function")
            let schemes = typed |> List.map (fun (n, t, _) -> n, Scheme([], t))
            let env'' = List.fold (fun e (n, s) -> Map.add n { Scheme = s; Mutable = false } e) env schemes
            let newConsts = typed |> List.map (fun (n, t, te) -> n, Scheme([], t), te, false)
            loop env'' (newConsts @ consts) externs main mainArg rest
        | MutableConst (n, e) :: rest ->
            let (t, te) = infer env typeDefs caseInfo e
            if prune t = UnitTy then err ("top-level binding '" + n + "' must not have type unit")
            if Map.containsKey n env then err ("duplicate definition of '" + n + "'")
            let scheme = Scheme([], t)
            loop (Map.add n { Scheme = scheme; Mutable = true } env) ((n, scheme, te, true) :: consts) externs main mainArg rest
        | Extern (n, ps, ret) :: rest ->
            if Map.containsKey n env then err ("duplicate definition of '" + n + "'")
            let t = List.foldBack (fun (_, pt) acc -> FunTy(pt, acc)) ps ret
            let scheme = Scheme([], t)
            loop (Map.add n { Scheme = scheme; Mutable = false } env) consts ((n, ps, ret) :: externs) main mainArg rest
        | Main (arg, body) :: rest ->
            if main.IsSome then err "more than one entry point (let main () = ...)"
            let env' =
                match arg with
                | Some a -> Map.add a { Scheme = Scheme([], TNamed("list", [ StringTy ])); Mutable = false } env
                | None -> env
            // main's return value is discarded by the C runtime; accept any
            // type so `[<EntryPoint>] let main argv = ... 0` also works.
            let (_, tb) = infer env' typeDefs caseInfo body
            loop env consts externs (Some tb) arg rest

    let tprog = loop env0 [] [] None None prog.Defs
    if tprog.Main.IsNone then err "no entry point: expected 'let main () = ...'"
    tprog
