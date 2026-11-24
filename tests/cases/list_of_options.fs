let rec keepSome xs = match xs with
    | [] -> []
    | h :: t -> match h with | None -> keepSome t | Some v -> v :: keepSome t

let main () =
    print_bool (keepSome [Some 1; None; Some 3] = [1; 3]); print_newline ()
