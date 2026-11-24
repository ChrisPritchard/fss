let mutable a = 1
let mutable b = 2
let main () =
    (let t = a in (a <- b; b <- t; print_int a; print_newline (); print_int b; print_newline ()))
