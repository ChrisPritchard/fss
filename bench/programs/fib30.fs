let rec fib n = if n < 2 then n else fib (n - 1) + fib (n - 2)
[<EntryPoint>]
let main _ = System.Console.WriteLine(fib 35); 0
