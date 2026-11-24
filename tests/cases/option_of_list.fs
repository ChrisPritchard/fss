let head xs = match xs with | [] -> None | h :: _ -> Some h

let main () =
    (print_int (match head [7; 8] with | None -> 0 | Some x -> x); print_newline ();
     print_bool (head [] = None); print_newline ())
