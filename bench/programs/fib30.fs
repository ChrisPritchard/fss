let rec fib n = if n < 2 then n else fib (n - 1) + fib (n - 2)
[<EntryPoint>]
let main _ = printf "%d\n" (fib 35); 0
