let rec fib n = if n < 2 then n else fib (n - 1) + fib (n - 2)

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
        System.Console.WriteLine(s.Length)
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
    | _ -> System.Console.WriteLine("?")
    0
