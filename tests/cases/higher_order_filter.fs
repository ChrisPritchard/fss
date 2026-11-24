let rec filter p xs = match xs with | [] -> [] | h :: t -> if p h then h :: filter p t else filter p t

let main () =
    print_bool (filter (fun x -> x > 1) [1; 2; 3] = [2; 3]); print_newline ()
