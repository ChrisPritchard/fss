// Local recursive functions (supported since the closure is held in a cell).
let main () =
    let rec fact n = if n <= 1 then 1 else n * fact (n - 1)
    println (fact 6)
    let rec fib n = if n < 2 then n else fib (n - 1) + fib (n - 2)
    println (fib 15)
    // capturing outer values in a local recursive function
    let bump = 100
    let rec addUp k acc = if k = 0 then acc + bump else addUp (k - 1) (acc + k)
    println (addUp 10 0)
    // list recursion
    let rec sum xs =
        match xs with
        | [] -> 0
        | h :: t -> h + sum t
    println (sum [1; 2; 3; 4])
    // mutual recursion
    let rec even n = if n = 0 then true else odd (n - 1)
    and odd n = if n = 0 then false else even (n - 1)
    println (even 10)
    println (odd 7)
    // ... and mutual recursion with a captured value
    let k = 3
    let rec f a = if a = 0 then k else g (a - 1)
    and g a = if a = 0 then k * 2 else f (a - 1)
    println (f 4)
    println (g 4)
    // shadowing: the recursive name is local to this scope
    let fact = 7
    println fact
    0
