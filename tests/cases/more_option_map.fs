let opt_map f o = match o with Some v -> Some (f v) | None -> None
let main () =
    print_int (match opt_map (fun x -> x * 2) (Some 21) with Some n -> n | None -> 0);
    print_newline ()
