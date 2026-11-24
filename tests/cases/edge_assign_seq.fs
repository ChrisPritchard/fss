let mutable n = 0
let main () =
    (n <- 1; n <- n + 10; print_int n; print_newline ())
