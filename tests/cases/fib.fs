let rec fib n = if n <= 1 then n else fib (n - 1) + fib (n - 2)
let rec fibIter n a b = if n = 0 then a else fibIter (n - 1) b (a + b)

let main () =
    (print_int (fib 10); print_newline ();
     print_int (fibIter 20 0 1); print_newline ())
