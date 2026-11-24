let main () =
    let a = Some (Some 1) in
    let b = Some None in
    (print_bool (a = Some (Some 1)); print_newline ();
     print_bool (b = Some None); print_newline ();
     print_bool (a = b); print_newline ())
