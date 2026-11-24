let wrap x = Some x
let main () =
    print_int (match wrap 5 with Some n -> n | None -> 0);
    print_newline ();
    print_string (match wrap "s" with Some s -> s | None -> "?");
    print_newline ()
