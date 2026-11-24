type R = { O: int option; L: int list }

let main () =
    let r = { O = Some 3; L = [1; 2] } in
    (print_int (match r.O with | None -> 0 | Some v -> v); print_newline ();
     print_bool (r.L = [1; 2]); print_newline ();
     print_bool (r = { O = Some 3; L = [1; 2] }); print_newline ())
