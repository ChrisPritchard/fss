let main () =
    let mutable i = 0 in
    while i < 5 do (print_int (i * i); print_newline (); i <- i + 1)
