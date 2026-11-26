// Higher-order functions and closures: map / fold / compose by hand.
let rec map f xs =
    match xs with
    | [] -> []
    | h :: t -> f h :: map f t

let rec fold f acc xs =
    match xs with
    | [] -> acc
    | h :: t -> fold f (f acc h) t

let rec build n acc = if n = 0 then acc else build (n - 1) (n :: acc)

let compose f g = fun x -> f (g x)

[<EntryPoint>]
let main _ =
    let xs = build 2000 []
    let inc = fun x -> x + 1
    let dbl = fun x -> x * 2
    let both = compose inc dbl
    let mutable total = 0
    let mutable i = 0
    while i < 200 do
        total <- total + fold (fun acc -> fun x -> acc + both x) 0 (map both xs)
        i <- i + 1
    System.Console.WriteLine(total)
    0
