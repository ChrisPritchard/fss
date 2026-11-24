let main () =
    let a = Some (Some 5) in
    let b = Some None in
    (print_int (match a with | None -> 0 | Some (Some x) -> x | Some None -> 0); print_newline ();
     print_bool (a = Some (Some 5)); print_newline ();
     print_bool (b = Some None); print_newline ())
