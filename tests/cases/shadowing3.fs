let x = 100

let main () =
    let x = 1 in
    let y = x + 1 in
    (print_int y; print_newline ();
     let x = 50 in
     (print_int x; print_newline ());
     print_int x; print_newline ())
