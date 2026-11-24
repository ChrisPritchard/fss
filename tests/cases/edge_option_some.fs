let main () =
    print_int (match Some 42 with Some n -> n | None -> 0);
    print_newline ()
