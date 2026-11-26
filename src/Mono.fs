module Fss.Mono

open Fss.Ast
open Fss.Typer

/// Fully monomorphic program, ready for code generation.
type Program =
    { Types: Map<string, Ast.TypeDef>
      Ctors: Map<string, string * Ty>
      Consts: (string * Ty * TExpr * bool) list
      Main: TExpr option
      MainArg: string option
      Externs: (string * (string * Ty) list * Ty) list
      Includes: string list
      Links: string list
      Exports: (string * string) list
      Library: bool }

let monomorphize (tprog: TProgram) : Program =
    let schemes = Map.ofList (tprog.Consts |> List.map (fun (n, s, _, _) -> n, s))

    let isPoly (name: string) =
        match Map.tryFind name schemes with
        | Some s -> not (List.isEmpty (schemeVars s))
        | None -> false

    let usedNames = System.Collections.Generic.HashSet<string>(tprog.Consts |> List.map (fun (n, _, _, _) -> n))
    let instMap = System.Collections.Generic.Dictionary<string * Ty, string>()
    let outConsts = ResizeArray<string * Ty * TExpr * bool>()
    let mutable counter = 0

    let fresh (name: string) =
        counter <- counter + 1
        let rec loop i =
            let cand = name + "_s" + string i
            if usedNames.Contains cand then loop (i + 1) else cand
        let cand = loop counter
        usedNames.Add cand |> ignore
        cand

    // Match a scheme body against a resolved instance type, producing the
    // substitution for the quantified variables.
    let rec matchTy (pat: Ty) (inst: Ty) (acc: Map<int, Ty>) : Map<int, Ty> =
        match pat, inst with
        | TyVar q, t -> Map.add q t acc
        | FunTy (a, b), FunTy (c, d) -> matchTy b d (matchTy a c acc)
        | TNamed (n1, as1), TNamed (n2, as2) when n1 = n2 && List.length as1 = List.length as2 ->
            List.fold2 (fun acc p i -> matchTy p i acc) acc as1 as2
        | TupleTy ts1, TupleTy ts2 when List.length ts1 = List.length ts2 ->
            List.fold2 (fun acc p i -> matchTy p i acc) acc ts1 ts2
        | _ -> acc

    let rec specE (subst: Map<int, Ty>) (e: TExpr) : TExpr =
        let rt t = resolveTy (substTyVars subst (zonk t))
        match e with
        | TInt _ | TFloat _ | TBool _ | TUnit | TStr _ -> e
        | TNil t -> TNil(rt t)
        | TNone t -> TNone(rt t)
        | TVar (n, t) ->
            let t' = rt t
            if isPoly n then TVar(specialize n t', t')
            else TVar(n, t')
        | TUnary (op, t, x) -> TUnary(op, rt t, specE subst x)
        | TBinary (op, t, a, b) -> TBinary(op, rt t, specE subst a, specE subst b)
        | TIf (t, c, a, b) -> TIf(rt t, specE subst c, specE subst a, specE subst b)
        | TLet (n, t, v, b) -> TLet(n, rt t, specE subst v, specE subst b)
        | TLetRec (n, t, v, b) -> TLetRec(n, rt t, specE subst v, specE subst b)
        | TLetRecGroup (bs, b) -> TLetRecGroup(bs |> List.map (fun (n, t, v) -> (n, rt t, specE subst v)), specE subst b)
        | TLetMutable (n, t, v, b) -> TLetMutable(n, rt t, specE subst v, specE subst b)
        | TAssign (n, x) -> TAssign(n, specE subst x)
        | TWhile (c, b) -> TWhile(specE subst c, specE subst b)
        | TSeq (t, xs) -> TSeq(rt t, List.map (specE subst) xs)
        | TFun (p, ft, body) -> TFun(p, rt ft, specE subst body)
        | TApply (t, f, a) ->
            match f with
            | TVar (n, ft) when isPoly n ->
                let ft' = rt ft
                TApply(rt t, TVar(specialize n ft', ft'), specE subst a)
            | _ -> TApply(rt t, specE subst f, specE subst a)
        | TTuple (t, xs) -> TTuple(rt t, List.map (specE subst) xs)
        | TRecord (t, fields, baseE) ->
            TRecord(rt t, fields |> List.map (fun (f, v) -> f, specE subst v), Option.map (specE subst) baseE)
        | TField (t, x, f) -> TField(rt t, specE subst x, f)
        | TCons (t, h, tl) -> TCons(rt t, specE subst h, specE subst tl)
        | TSome (t, x) -> TSome(rt t, specE subst x)
        | TMatch (t, scrut, arms) ->
            TMatch(rt t, specE subst scrut, arms |> List.map (fun (p, b) -> specP subst p, specE subst b))
        | TFailwith (t, x) -> TFailwith(rt t, specE subst x)
        | TTry (t, body, arms) ->
            TTry(rt t, specE subst body, arms |> List.map (fun (p, b) -> specP subst p, specE subst b))

    and specP (subst: Map<int, Ty>) (p: TPattern) : TPattern =
        match p with
        | TPVar (n, t) -> TPVar(n, resolveTy (substTyVars subst (zonk t)))
        | TPTuple ps -> TPTuple(List.map (specP subst) ps)
        | TPCons (h, t) -> TPCons(specP subst h, specP subst t)
        | TPUnion (c, opt, t) -> TPUnion(c, Option.map (specP subst) opt, resolveTy (substTyVars subst (zonk t)))
        | TPStr _ -> p
        | _ -> p

    and specialize (name: string) (instTy: Ty) : string =
        let key = (name, instTy)
        match instMap.TryGetValue key with
        | true, c -> c
        | _ ->
            let cname = fresh name
            instMap.[key] <- cname
            let scheme = schemes.[name]
            let subst = matchTy (schemeBody scheme) instTy Map.empty
            let (_, _, expr, _) = tprog.Consts |> List.find (fun (n, _, _, _) -> n = name)
            let body = specE subst expr
            outConsts.Add(cname, instTy, body, false)
            cname

    // Emit monomorphic top-level bindings in source order; polymorphic ones
    // are emitted on demand when a use is specialized.
    for (name, scheme, expr, mutableFlag) in tprog.Consts do
        if List.isEmpty (schemeVars scheme) then
            let t = resolveTy (schemeBody scheme)
            outConsts.Add(name, t, specE Map.empty expr, mutableFlag)

    let main' = Option.map (specE Map.empty) tprog.Main

    { Types = tprog.Types
      Ctors = tprog.Ctors
      Consts = List.ofSeq outConsts
      Main = main'
      MainArg = tprog.MainArg
      Externs = tprog.Externs
      Includes = tprog.Includes
      Links = tprog.Links
      Exports = tprog.Exports
      Library = tprog.Library }
