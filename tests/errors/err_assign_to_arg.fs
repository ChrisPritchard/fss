let f x = (x <- 1; x)
let main () = print_int (f 0)
