type R = { L: int list; O: int option }

let main () =
    (print_bool ({ L = [1; 2]; O = Some 3 } = { L = [1; 2]; O = Some 3 }); print_newline ();
     print_bool ({ L = [1; 2]; O = Some 3 } = { L = [1; 2]; O = Some 4 }); print_newline ();
     print_bool ([Some 1; None] = [Some 1; None]); print_newline ())
