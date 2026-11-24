type Tree = Leaf of int | Node of Tree * Tree

let rec sumTree t = match t with | Leaf n -> n | Node (l, r) -> sumTree l + sumTree r
let rec depth t = match t with | Leaf n -> 1 | Node (l, r) -> 1 + (let dl = depth l in let dr = depth r in (if dl > dr then dl else dr))

let main () =
    let t = Node (Leaf 1, Node (Leaf 2, Leaf 3)) in
    (print_int (sumTree (Leaf 5)); print_newline ();
     print_int (sumTree t); print_newline ();
     print_int (depth t); print_newline ();
     print_bool (t = Node (Leaf 1, Node (Leaf 2, Leaf 3))); print_newline ())
