// Arithmetic and local bindings.
let answer = 40 + 2

// Entry point: prints results and runs a small loop.
let main () =
    print_int answer; print_newline ();
    let mutable i = 0 in
    while i < 3 do (print_int i; print_newline (); i <- i + 1);
    let bigger = answer > 40 in
    print_bool bigger; print_newline ()
