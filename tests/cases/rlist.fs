type RList = RNil | RCons of int * RList

let rec rsum xs = match xs with | RNil -> 0 | RCons (h, t) -> h + rsum t
let rec rlen xs = match xs with | RNil -> 0 | RCons (h, t) -> 1 + rlen t

let main () =
    let xs = RCons (1, RCons (2, RCons (3, RNil))) in
    (print_int (rsum xs); print_newline ();
     print_int (rlen xs); print_newline ();
     print_bool (xs = RCons (1, RCons (2, RCons (3, RNil)))); print_newline ())
