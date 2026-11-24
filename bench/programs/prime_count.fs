let is_prime n =
    let mutable d = 2
    let mutable p = n > 1
    while p && d * d <= n do
        if n % d = 0 then p <- false
        d <- d + 1
    p

[<EntryPoint>]
let main _ =
    let mutable count = 0
    let mutable i = 2
    while i < 100000 do
        if is_prime i then count <- count + 1
        i <- i + 1
    printf "%d\n" count
    0
