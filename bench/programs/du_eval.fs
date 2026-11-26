// Union types + deep pattern matching: a tiny expression interpreter.
type Expr =
    | Num of int
    | Add of Expr * Expr
    | Mul of Expr * Expr
    | Neg of Expr

let rec eval e =
    match e with
    | Num n -> n
    | Add (a, b) -> eval a + eval b
    | Mul (a, b) -> eval a * eval b
    | Neg a -> -(eval a)

let rec build d n =
    if d = 0 then Num n
    elif n % 2 = 0 then Add (build (d - 1) (n - 1), Mul (build (d - 1) n, Num 2))
    else Neg (Add (build (d - 1) (n - 2), Num n))

[<EntryPoint>]
let main _ =
    let mutable acc = 0
    let mutable i = 0
    while i < 200000 do
        acc <- acc + eval (build 6 i)
        i <- i + 1
    System.Console.WriteLine(acc)
    0
