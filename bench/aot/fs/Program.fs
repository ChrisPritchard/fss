type Tree = Leaf of int | Node of Tree * Tree

type Point = { X: int; Y: int }
type PairR = { A: Point; B: Point }

type Expr =
    | Num of int
    | Add of Expr * Expr
    | Mul of Expr * Expr
    | Neg of Expr

let rec fib n = if n < 2 then n else fib (n - 1) + fib (n - 2)

let rec buildList n acc = if n = 0 then acc else buildList (n - 1) (n :: acc)
let rec sumList acc xs = match xs with [] -> acc | h :: t -> sumList (acc + h) t

let rec insert x xs =
    match xs with
    | [] -> [x]
    | h :: t -> if x <= h then x :: h :: t else h :: insert x t

let rec sort xs =
    match xs with
    | [] -> []
    | h :: t -> insert h (sort t)

let rec buildDesc n = if n = 0 then [] else n :: buildDesc (n - 1)

let rec treeSum t = match t with Leaf n -> n | Node (l, r) -> treeSum l + treeSum r
let rec buildTree n = if n = 0 then Leaf 0 else Node (buildTree (n - 1), Leaf n)


let rec mapL f xs =
    match xs with
    | [] -> []
    | h :: t -> f h :: mapL f t

let rec foldL f acc xs =
    match xs with
    | [] -> acc
    | h :: t -> foldL f (f acc h) t

let compose f g = fun x -> f (g x)

let rec norm2 (xs: float list) acc =
    match xs with
    | [] -> acc
    | h :: t -> norm2 t (acc + h * h)

let rec buildFloats n acc = if n = 0 then acc else buildFloats (n - 1) (float n :: acc)

let rec numbers n acc =
    if n = 0 then acc else numbers (n - 1) (string n + "," + acc)

let rec evalExpr e =
    match e with
    | Num n -> n
    | Add (a, b) -> evalExpr a + evalExpr b
    | Mul (a, b) -> evalExpr a * evalExpr b
    | Neg a -> -(evalExpr a)

let rec buildExpr d n =
    if d = 0 then Num n
    elif n % 2 = 0 then Add (buildExpr (d - 1) (n - 1), Mul (buildExpr (d - 1) n, Num 2))
    else Neg (Add (buildExpr (d - 1) (n - 2), Num n))

let shiftPoint (p: Point) (dx: int) (dy: int) = { p with X = p.X + dx; Y = p.Y + dy }

let dist2 (p: Point) (q: Point) =
    let dx = p.X - q.X
    let dy = p.Y - q.Y
    dx * dx + dy * dy

[<EntryPoint>]
let main argv =
    match argv.[0] with
    | "fib30" -> System.Console.WriteLine(fib 35)
    | "loop_count" ->
        let mutable x = 0
        let mutable i = 0
        while i < 100000000 do
            x <- x + 1
            i <- i + 1
        System.Console.WriteLine(x)
    | "sum_list" ->
        let mutable s = 0
        let mutable i = 0
        while i < 10 do
            s <- s + sumList 0 (buildList 20000 [])
            i <- i + 1
        System.Console.WriteLine(s)
    | "collatz" ->
        let mutable total = 0
        let mutable i = 1
        while i <= 100000 do
            let mutable m = int64 i
            let mutable steps = 0
            while m <> 1L do
                m <- if m % 2L = 0L then m / 2L else 3L * m + 1L
                steps <- steps + 1
            total <- total + steps
            i <- i + 1
        System.Console.WriteLine(total)
    | "str_concat" ->
        let mutable s = ""
        let mutable i = 0
        while i < 20000 do
            s <- s + "x"
            i <- i + 1
        System.Console.WriteLine(s)
    | "prime_count" ->
        let mutable c = 0
        let mutable i = 2
        while i < 100000 do
            let mutable p = true
            let mutable d = 2
            while p && d * d <= i do
                if i % d = 0 then p <- false
                d <- d + 1
            if p then c <- c + 1
            i <- i + 1
        System.Console.WriteLine(c)
    | "sort_list" ->
        match sort (buildDesc 1500) with
        | a :: b :: _ -> System.Console.WriteLine(a * 1000 + b)
        | _ -> System.Console.WriteLine(0)
    | "tree_sum" -> System.Console.WriteLine(treeSum (buildTree 1000))
    | "records" ->
        let origin = { X = 0; Y = 0 }
        let mutable acc0 = 0
        let mutable i = 0
        while i < 2000000 do
            let p = { X = i; Y = i * 2 }
            let q = shiftPoint p 1 1
            let r = { A = p; B = q }
            acc0 <- acc0 + dist2 r.A r.B
            i <- i + 1
        System.Console.WriteLine(acc0)
    | "du_eval" ->
        let mutable acc0 = 0
        let mutable i = 0
        while i < 200000 do
            acc0 <- acc0 + evalExpr (buildExpr 6 i)
            i <- i + 1
        System.Console.WriteLine(acc0)
    | "closures" ->
        let xs = buildDesc 2000
        let inc = fun x -> x + 1
        let dbl = fun x -> x * 2
        let both = compose inc dbl
        let mutable total = 0
        let mutable i = 0
        while i < 200 do
            total <- total + foldL (fun acc0 -> fun x -> acc0 + both x) 0 (mapL both xs)
            i <- i + 1
        System.Console.WriteLine(total)
    | "string_build" ->
        let mutable s2 = ""
        let mutable i = 0
        while i < 400 do
            s2 <- s2 + numbers 20 ""
            i <- i + 1
        System.Console.WriteLine(s2)
    | "float_math" ->
        let xs = buildFloats 200 []
        let mutable total = 0.0
        let mutable i = 0
        while i < 20000 do
            total <- total + sqrt (norm2 xs 0.0) + System.Math.Sin 0.5 + System.Math.Log 2.0
            i <- i + 1
        System.Console.WriteLine(int total)
    | _ -> System.Console.WriteLine("?")
    0
