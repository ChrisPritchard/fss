type P = { X: int; Y: int }
let main () =
    print_bool ({ X = 1; Y = 2 } = { X = 1; Y = 2 });
    print_newline ();
    print_bool ({ X = 1; Y = 2 } <> { X = 1; Y = 3 });
    print_newline ()
