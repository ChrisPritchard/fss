let rec map f xs = match xs with | [] -> [] | h :: t -> f h :: map f t
