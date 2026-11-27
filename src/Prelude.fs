module Fss.Prelude

/// A small standard library written in fss itself.
///
/// The driver parses this source and prepends it to every program, so these
/// definitions are always in scope. They are ordinary polymorphic top-level
/// functions, which means each use is monomorphised exactly like user code —
/// there is no special-case typing or code generation for the library.
///
/// Names are mangled (`__list_map`, ...) so they can never collide with user
/// bindings; the parser rewrites `List.map` to `__list_map` (see the module
/// alias table in `Parser.fs`).
let source: string =
    """
// ---- List -----------------------------------------------------------------

let rec __list_length_go acc xs =
    match xs with
    | [] -> acc
    | _ :: t -> __list_length_go (acc + 1) t

let __list_length xs = __list_length_go 0 xs

let __list_isEmpty xs =
    match xs with
    | [] -> true
    | _ -> false

let __list_head xs =
    match xs with
    | [] -> failwith "List.head: the list is empty"
    | h :: _ -> h

let __list_tail xs =
    match xs with
    | [] -> failwith "List.tail: the list is empty"
    | _ :: t -> t

let __list_tryHead xs =
    match xs with
    | [] -> None
    | h :: _ -> Some h

let __list_exactlyOne xs =
    match xs with
    | [ x ] -> x
    | _ -> failwith "List.exactlyOne: the list does not have exactly one item"

let rec __list_map f xs =
    match xs with
    | [] -> []
    | h :: t -> f h :: __list_map f t

let rec __list_mapi_go i f xs =
    match xs with
    | [] -> []
    | h :: t -> f i h :: __list_mapi_go (i + 1) f t

let __list_mapi f xs = __list_mapi_go 0 f xs

let rec __list_filter p xs =
    match xs with
    | [] ->
        []
    | h :: t ->
        if p h then h :: __list_filter p t else __list_filter p t

let rec __list_fold f acc xs =
    match xs with
    | [] -> acc
    | h :: t -> __list_fold f (f acc h) t

let rec __list_foldBack f xs acc =
    match xs with
    | [] -> acc
    | h :: t -> f h (__list_foldBack f t acc)

let rec __list_iter f xs =
    match xs with
    | [] ->
        ()
    | h :: t ->
        f h
        __list_iter f t

let rec __list_exists p xs =
    match xs with
    | [] -> false
    | h :: t -> if p h then true else __list_exists p t

let rec __list_forall p xs =
    match xs with
    | [] -> true
    | h :: t -> if p h then __list_forall p t else false

let rec __list_contains v xs =
    match xs with
    | [] -> false
    | h :: t -> if h = v then true else __list_contains v t

let rec __list_tryFind p xs =
    match xs with
    | [] -> None
    | h :: t -> if p h then Some h else __list_tryFind p t

let __list_find p xs =
    match __list_tryFind p xs with
    | None -> failwith "List.find: no matching element"
    | Some x -> x

let rec __list_rev_go acc xs =
    match xs with
    | [] -> acc
    | h :: t -> __list_rev_go (h :: acc) t

let __list_rev xs = __list_rev_go [] xs

let __list_append xs ys = xs @ ys

let rec __list_concat_go acc xs =
    match xs with
    | [] -> acc
    | h :: t -> __list_concat_go (acc @ h) t

let __list_concat xs = __list_concat_go [] xs

let rec __list_choose f xs =
    match xs with
    | [] -> []
    | h :: t ->
        match f h with
        | None -> __list_choose f t
        | Some v -> v :: __list_choose f t

let rec __list_take n xs =
    match xs with
    | [] -> []
    | h :: t -> if n <= 0 then [] else h :: __list_take (n - 1) t

let rec __list_skip n xs =
    match xs with
    | [] -> []
    | _ :: t -> if n <= 0 then xs else __list_skip (n - 1) t

let rec __list_tryItem n xs =
    match xs with
    | [] -> None
    | h :: t -> if n = 0 then Some h else if n < 0 then None else __list_tryItem (n - 1) t

let __list_item n xs =
    match __list_tryItem n xs with
    | None -> failwith "List.item: index out of range"
    | Some x -> x

let rec __list_zip xs ys =
    match xs with
    | [] ->
        []
    | x :: xt ->
        match ys with
        | [] -> []
        | y :: yt -> (x, y) :: __list_zip xt yt

let rec __list_unzip_go xs left right =
    match xs with
    | [] -> (__list_rev left, __list_rev right)
    | (a, b) :: t -> __list_unzip_go t (a :: left) (b :: right)

let __list_unzip xs = __list_unzip_go xs [] []

let rec __list_distinct_go seen acc xs =
    match xs with
    | [] -> __list_rev acc
    | h :: t ->
        if __list_contains h seen then __list_distinct_go seen acc t
        else __list_distinct_go (h :: seen) (h :: acc) t

let __list_distinct xs = __list_distinct_go [] [] xs

// ---- Option ---------------------------------------------------------------

let __option_map f o =
    match o with
    | None -> None
    | Some x -> Some (f x)

let __option_defaultValue d o =
    match o with
    | None -> d
    | Some x -> x

let __option_defaultWith thunk o =
    match o with
    | None -> thunk ()
    | Some x -> x

let __option_isSome o =
    match o with
    | None -> false
    | Some _ -> true

let __option_isNone o =
    match o with
    | None -> true
    | Some _ -> false

let __option_bind f o =
    match o with
    | None -> None
    | Some x -> f x

let __option_iter f o =
    match o with
    | None -> ()
    | Some x -> f x

let __option_filter p o =
    match o with
    | None -> None
    | Some x -> if p x then Some x else None

let __option_toList o =
    match o with
    | None -> []
    | Some x -> [ x ]

// ---- String ---------------------------------------------------------------

let rec __string_concat_go (sep: string) (acc: string) first xs =
    match xs with
    | [] -> acc
    | h :: t ->
        if first then __string_concat_go sep (acc + h) false t
        else __string_concat_go sep (acc + sep + h) false t

let __string_concat (sep: string) xs = __string_concat_go sep "" true xs

let __string_isEmpty s = str_len s = 0
"""
