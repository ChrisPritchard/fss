let x = Some (Some 5)
let main () =
    print_int (match x with Some (Some n) -> n | _ -> 0);
    print_newline ()
