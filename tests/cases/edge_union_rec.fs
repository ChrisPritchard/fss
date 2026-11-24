type Tree = Leaf of int | Node of Tree * Tree
let rec sum t = match t with Leaf n -> n | Node (l, r) -> sum l + sum r
let main () =
    print_int (sum (Node (Leaf 1, Node (Leaf 2, Leaf 3))));
    print_newline ()
