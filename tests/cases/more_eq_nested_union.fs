type T = A of T option | B
let main () =
    print_bool (A (Some (A None)) = A (Some (A None)));
    print_newline ();
    print_bool (A (Some (B)) <> A None);
    print_newline ()
