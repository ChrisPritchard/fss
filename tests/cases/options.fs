let main () =
    let a = Some 5 in
    let b = None in
    ((match a with | None -> print_int 0 | Some x -> print_int x); print_newline ();
     (match b with | None -> print_int 0 | Some x -> print_int x); print_newline ();
     print_bool (a = Some 5); print_newline ();
     print_bool (b = None); print_newline ())
