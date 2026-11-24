let main () =
    let mutable i = 0 in
    (let a = (false && ((i <- i + 1); true)) in
     print_int i; print_newline ();
     let b = (true || ((i <- i + 1); false)) in
     print_int i; print_newline ())
