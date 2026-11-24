let mutable r = 0
let main () =
    (r <- 1; let y = r + 1 in (r <- y; print_int r; print_newline ()))
