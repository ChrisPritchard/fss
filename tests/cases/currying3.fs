let add3 x y z = x + y + z

let main () =
    let add1 = add3 1 in
    let add12 = add1 2 in
    (print_int (add12 3); print_newline ();
     print_int (add3 1 2 3); print_newline ())
