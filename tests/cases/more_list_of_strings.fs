let rec join xs = match xs with [] -> "" | h :: [] -> h | h :: t -> h + "," + join t
let main () =
    print_string (join ["a"; "b"; "c"]);
    print_newline ()
