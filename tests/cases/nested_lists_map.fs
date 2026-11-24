let rec map f xs = match xs with | [] -> [] | h :: t -> f h :: map f t

let main () =
    print_bool (map (fun inner -> map (fun x -> x + 1) inner) [[1; 2]; [3]] = [[2; 3]; [4]]); print_newline ()
