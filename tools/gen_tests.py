import os

os.makedirs('tests/cases', exist_ok=True)
os.makedirs('tests/errors', exist_ok=True)

cases = []

def add(name, stmts, expected):
    # stmts: flat list; entries starting with 'type ', 'let ', or 'and ' become
    # top-level definitions; the rest go into main's body.
    defs = [s for s in stmts if s.startswith(('type ', 'let ', 'and '))]
    body = [s for s in stmts if s not in defs]
    top = '\n'.join(defs) + ('\n' if defs else '')
    main = ('    ' + ';\n    '.join(body) + '\n') if body else ''
    cases.append((name, top + 'let main () =\n' + main, expected))

# --- Arithmetic edge cases ---
add('edge_arith_wrap',
    ['print_int (2147483647 + 1)', 'print_newline ()', 'print_int (-2147483647 - 2)', 'print_newline ()'],
    ['-2147483648', '2147483647'])
add('edge_arith_div_neg',
    ['print_int ((-7) / 2)', 'print_newline ()', 'print_int ((-7) % 2)', 'print_newline ()', 'print_int (7 / (-2))', 'print_newline ()', 'print_int (7 % (-2))', 'print_newline ()'],
    ['-3', '-1', '-3', '1'])
add('edge_arith_zero',
    ['print_int (0 * 12345)', 'print_newline ()', 'print_int (0 - 0)', 'print_newline ()', 'print_int (7 - 7)', 'print_newline ()'],
    ['0', '0', '0'])
add('edge_arith_prec',
    ['print_int (2 + 3 * 4 - 5)', 'print_newline ()', 'print_int (20 / 4 / 2)', 'print_newline ()', 'print_int (20 / (4 / 2))', 'print_newline ()'],
    ['9', '2', '10'])
add('edge_arith_unary',
    ['print_int (-5)', 'print_newline ()', 'print_int (-(-5))', 'print_newline ()', 'print_int (0 - -5)', 'print_newline ()'],
    ['-5', '5', '5'])

# --- Comparison ---
add('edge_cmp_int',
    ['print_bool (3 < 5)', 'print_newline ()', 'print_bool (3 <= 3)', 'print_newline ()', 'print_bool (5 > 3)', 'print_newline ()', 'print_bool (5 >= 5)', 'print_newline ()', 'print_bool (3 = 3)', 'print_newline ()', 'print_bool (3 <> 4)', 'print_newline ()'],
    ['true', 'true', 'true', 'true', 'true', 'true'])
add('edge_cmp_string',
    ['print_bool ("abc" = "abc")', 'print_newline ()', 'print_bool ("abc" <> "abd")', 'print_newline ()', 'print_bool ("" = "")', 'print_newline ()'],
    ['true', 'true', 'true'])
add('edge_cmp_bool',
    ['print_bool (true = true)', 'print_newline ()', 'print_bool (false <> true)', 'print_newline ()'],
    ['true', 'true'])

# --- Logic ---
add('edge_logic_and',
    ['print_bool (true && true)', 'print_newline ()', 'print_bool (true && false)', 'print_newline ()', 'print_bool (false && true)', 'print_newline ()', 'print_bool (false && false)', 'print_newline ()'],
    ['true', 'false', 'false', 'false'])
add('edge_logic_or',
    ['print_bool (true || true)', 'print_newline ()', 'print_bool (true || false)', 'print_newline ()', 'print_bool (false || true)', 'print_newline ()', 'print_bool (false || false)', 'print_newline ()'],
    ['true', 'true', 'true', 'false'])
add('edge_logic_not',
    ['print_bool (not true)', 'print_newline ()', 'print_bool (not false)', 'print_newline ()', 'print_bool (not (1 < 2))', 'print_newline ()'],
    ['false', 'true', 'false'])
add('edge_logic_mixed',
    ['print_bool (1 < 2 && 3 < 4)', 'print_newline ()', 'print_bool (1 < 2 || 3 > 4)', 'print_newline ()', 'print_bool (not (1 = 2) && not (2 = 3))', 'print_newline ()'],
    ['true', 'true', 'true'])

# --- Control flow ---
add('edge_if_basic',
    ['print_int (if true then 1 else 2)', 'print_newline ()', 'print_int (if false then 1 else 2)', 'print_newline ()'],
    ['1', '2'])
add('edge_elif_chain',
    ['let f x = if x = 0 then "zero" elif x = 1 then "one" elif x = 2 then "two" else "many"',
     'print_string (f 0)', 'print_newline ()', 'print_string (f 1)', 'print_newline ()', 'print_string (f 2)', 'print_newline ()', 'print_string (f 3)', 'print_newline ()'],
    ['zero', 'one', 'two', 'many'])
add('edge_while',
    ['let mutable i = 0', 'let mutable acc = 0', 'while i < 5 do (acc <- acc + i; i <- i + 1)', 'print_int acc', 'print_newline ()'],
    ['10'])
add('edge_if_unit',
    ['if true then (print_int 1; print_newline ()) else (print_int 2; print_newline ())'],
    ['1'])
add('edge_nested_if',
    ['let f x = if x > 0 then (if x > 10 then "big" else "small") else "neg"',
     'print_string (f 5)', 'print_newline ()', 'print_string (f 15)', 'print_newline ()', 'print_string (f (-1))', 'print_newline ()'],
    ['small', 'big', 'neg'])

# --- Functions ---
add('edge_fib',
    ['let rec fib n = if n < 2 then n else fib (n - 1) + fib (n - 2)', 'print_int (fib 10)', 'print_newline ()'],
    ['55'])
add('edge_mutual',
    ['let rec even n = if n = 0 then true else odd (n - 1)', 'and odd n = if n = 0 then false else even (n - 1)',
     'print_bool (even 10)', 'print_newline ()', 'print_bool (odd 10)', 'print_newline ()', 'print_bool (even 7)', 'print_newline ()'],
    ['true', 'false', 'false'])
add('edge_closure',
    ['let mutable n = 0', 'let counter d = (n <- n + 1; n)', 'print_int (counter 0)', 'print_newline ()', 'print_int (counter 0)', 'print_newline ()'],
    ['1', '2'])
add('edge_apply',
    ['let apply f x = f x', 'let inc x = x + 1', 'let double x = x * 2',
     'print_int (apply (fun x -> x + 1) 41)', 'print_newline ()', 'print_int (apply (fun x -> x * 2) 21)', 'print_newline ()'],
    ['42', '42'])
add('edge_curry',
    ['let add a b = a + b', 'let add5 = add 5', 'print_int (add5 10)', 'print_newline ()', 'print_int (add 3 4)', 'print_newline ()'],
    ['15', '7'])
add('edge_higher',
    ['let twice f x = f (f x)', 'print_int (twice (fun x -> x + 1) 5)', 'print_newline ()'],
    ['7'])

# --- Lists ---
add('edge_list_literal',
    ['print_int (match [1; 2; 3] with a :: b :: c :: [] -> a + b + c | _ -> 0)', 'print_newline ()'],
    ['6'])
add('edge_list_nested',
    ['let xs = [[1; 2]; [3]; []]',
     'print_int (match xs with a :: b :: c :: [] -> (match a with x :: y :: [] -> x + y | _ -> 0) + (match b with x :: [] -> x | _ -> 0) | _ -> 0)', 'print_newline ()'],
    ['6'])
add('edge_list_eq',
    ['print_bool ([1; 2] = [1; 2])', 'print_newline ()', 'print_bool ([1; 2] <> [2; 1])', 'print_newline ()', 'print_bool ([] = [])', 'print_newline ()'],
    ['true', 'true', 'true'])
add('edge_list_rec',
    ['let rec length xs = match xs with [] -> 0 | _ :: t -> 1 + length t',
     'let rec sum xs = match xs with [] -> 0 | h :: t -> h + sum t',
     'print_int (length [1; 2; 3; 4])', 'print_newline ()', 'print_int (sum [1; 2; 3; 4])', 'print_newline ()'],
    ['4', '10'])
add('edge_list_map',
    ['let rec map f xs = match xs with [] -> [] | h :: t -> f h :: map f t',
     'print_int (match map (fun x -> x + 1) [1; 2; 3] with a :: b :: c :: [] -> a + b + c | _ -> 0)', 'print_newline ()'],
    ['9'])

# --- Tuples ---
add('edge_tuple_nary',
    ['print_int (match (1, "x", true) with (a, b, c) -> a)', 'print_newline ()',
     'print_string (match (1, "x", true) with (a, b, c) -> b)', 'print_newline ()',
     'print_bool (match (1, "x", true) with (a, b, c) -> c)', 'print_newline ()'],
    ['1', 'x', 'true'])
add('edge_tuple_eq',
    ['print_bool ((1, "a") = (1, "a"))', 'print_newline ()', 'print_bool ((1, "a") <> (2, "a"))', 'print_newline ()'],
    ['true', 'true'])
add('edge_tuple_swap',
    ['let swap p = match p with (a, b) -> (b, a)',
     'print_int (match swap (1, 2) with (a, b) -> a)', 'print_newline ()',
     'print_int (match swap (1, 2) with (a, b) -> b)', 'print_newline ()'],
    ['2', '1'])
add('edge_tuple_nested',
    ['let x = ((1, 2), (3, 4))', 'print_int (match x with ((a, b), (c, d)) -> a + b + c + d)', 'print_newline ()'],
    ['10'])

# --- Options ---
add('edge_option_some',
    ['print_int (match Some 42 with Some n -> n | None -> 0)', 'print_newline ()'],
    ['42'])
add('edge_option_none',
    ['print_int (match None with Some n -> n | None -> 7)', 'print_newline ()'],
    ['7'])
add('edge_option_eq',
    ['print_bool (Some 1 = Some 1)', 'print_newline ()', 'print_bool (None = None)', 'print_newline ()', 'print_bool (Some 1 <> None)', 'print_newline ()'],
    ['true', 'true', 'true'])
add('edge_option_nested',
    ['let x = Some (Some 5)', 'print_int (match x with Some (Some n) -> n | _ -> 0)', 'print_newline ()'],
    ['5'])

# --- Records ---
add('edge_record_basic',
    ['type P = { X: int; Y: int }', 'let p = { X = 3; Y = 4 }', 'print_int (p.X + p.Y)', 'print_newline ()'],
    ['7'])
add('edge_record_eq',
    ['type P = { X: int; Y: int }', 'print_bool ({ X = 1; Y = 2 } = { X = 1; Y = 2 })', 'print_newline ()', 'print_bool ({ X = 1; Y = 2 } <> { X = 1; Y = 3 })', 'print_newline ()'],
    ['true', 'true'])
add('edge_record_field_str',
    ['type R = { Name: string; N: int }', 'let r = { Name = "foo"; N = 5 }', 'print_string r.Name', 'print_newline ()', 'print_int r.N', 'print_newline ()'],
    ['foo', '5'])

# --- Unions ---
add('edge_union_nullary',
    ['type Color = Red | Green | Blue', 'let f c = match c with Red -> "r" | Green -> "g" | Blue -> "b"',
     'print_string (f Red)', 'print_newline ()', 'print_string (f Green)', 'print_newline ()', 'print_string (f Blue)', 'print_newline ()'],
    ['r', 'g', 'b'])
add('edge_union_payload',
    ['type Shape = Circle of int | Rect of int * int', 'let area s = match s with Circle r -> r * r | Rect (w, h) -> w * h',
     'print_int (area (Circle 3))', 'print_newline ()', 'print_int (area (Rect (3, 4)))', 'print_newline ()'],
    ['9', '12'])
add('edge_union_rec',
    ['type Tree = Leaf of int | Node of Tree * Tree', 'let rec sum t = match t with Leaf n -> n | Node (l, r) -> sum l + sum r',
     'print_int (sum (Node (Leaf 1, Node (Leaf 2, Leaf 3))))', 'print_newline ()'],
    ['6'])
add('edge_union_eq',
    ['type Color = Red | Green | Blue', 'print_bool (Red = Red)', 'print_newline ()', 'print_bool (Red <> Blue)', 'print_newline ()'],
    ['true', 'true'])

# --- Polymorphism ---
add('edge_poly_id',
    ['let id x = x', 'print_int (id 1 + id 2)', 'print_newline ()', 'print_string (id "a")', 'print_newline ()', 'print_bool (id true)', 'print_newline ()'],
    ['3', 'a', 'true'])
add('edge_poly_compose',
    ['let comp f g x = f (g x)', 'let inc x = x + 1', 'let double x = x * 2',
     'print_int (comp (fun x -> x + 1) (fun x -> x * 2) 5)', 'print_newline ()', 'print_int (comp (fun x -> x * 2) (fun x -> x + 1) 5)', 'print_newline ()'],
    ['11', '12'])
add('edge_poly_list',
    ['let singleton x = [x]', 'print_int (match singleton 5 with a :: [] -> a | _ -> 0)', 'print_newline ()', 'print_string (match singleton "x" with a :: [] -> a | _ -> "?")', 'print_newline ()'],
    ['5', 'x'])
add('edge_poly_tuple',
    ['let fst p = match p with (a, b) -> a', 'print_int (fst (1, "x"))', 'print_newline ()', 'print_string (fst ("y", 2))', 'print_newline ()'],
    ['1', 'y'])
add('edge_poly_option',
    ['let wrap x = Some x', 'print_int (match wrap 5 with Some n -> n | None -> 0)', 'print_newline ()', 'print_string (match wrap "s" with Some s -> s | None -> "?")', 'print_newline ()'],
    ['5', 's'])

# --- Strings & builtins ---
add('edge_str_concat',
    ['print_string ("foo" + "bar")', 'print_newline ()', 'print_string ("a" + "b" + "c")', 'print_newline ()'],
    ['foobar', 'abc'])
add('edge_str_escapes',
    ['print_int (str_len "a\\nb")', 'print_newline ()', 'print_int (str_len "\\t")', 'print_newline ()', 'print_int (str_len "\\"")', 'print_newline ()', 'print_int (str_len "\\\\")', 'print_newline ()'],
    ['3', '1', '1', '1'])
add('edge_str_builtins',
    ['print_int (str_len "hello")', 'print_newline ()', 'print_int (str_get "abc" 0)', 'print_newline ()', 'print_string (str_sub "hello" 1 3)', 'print_newline ()', 'print_int (int_of_string "42")', 'print_newline ()', 'print_string (int_to_string 42)', 'print_newline ()'],
    ['5', '97', 'ell', '42', '42'])

# --- Sequences / assignment ---
add('edge_seq',
    ['(print_int 1; print_newline (); print_int 2; print_newline ())'],
    ['1', '2'])
add('edge_assign_global',
    ['let mutable x = 0', 'let inc d = (x <- x + 1; x)',
     'print_int (inc 0)', 'print_newline ()', 'print_int (inc 0)', 'print_newline ()', 'print_int x', 'print_newline ()'],
    ['1', '2', '2'])
add('edge_assign_seq',
    ['let mutable n = 0', '(n <- 1; n <- n + 10; print_int n; print_newline ())'],
    ['11'])

# --- Structural equality (deep) ---
add('edge_eq_deep_list',
    ['print_bool ([[1]; [2; 3]] = [[1]; [2; 3]])', 'print_newline ()', 'print_bool ([[1]; [2]] <> [[1]; [3]])', 'print_newline ()'],
    ['true', 'true'])
add('edge_eq_deep_option',
    ['print_bool (Some [1; 2] = Some [1; 2])', 'print_newline ()', 'print_bool (Some [1] <> Some [2])', 'print_newline ()'],
    ['true', 'true'])
add('edge_eq_deep_tuple',
    ['print_bool ((1, [2; 3], Some "x") = (1, [2; 3], Some "x"))', 'print_newline ()'],
    ['true'])
add('edge_eq_deep_union',
    ['type T = A of int list | B of T option',
     'print_bool (A [1; 2] = A [1; 2])', 'print_newline ()', 'print_bool (B (Some (A [1])) = B (Some (A [1])))', 'print_newline ()', 'print_bool (A [1] <> A [2])', 'print_newline ()'],
    ['true', 'true', 'true'])

# Error cases
errors = []

def add_err(name, program):
    errors.append((name, program + '\n'))

add_err('err_arith_str_add', 'let main () = print_int (1 + "a")')
add_err('err_cmp_int_str', 'let main () = print_bool (1 = "a")')
add_err('err_unbound', 'let main () = print_int undefined')
add_err('err_if_branch_type', 'let main () = print_int (if true then 1 else "a")')
add_err('err_apply_nonfn', 'let main () = print_int (1 2)')
add_err('err_assign_immutable', 'let x = 1\nlet main () = (x <- 2; print_int x)')
add_err('err_assign_wrong', 'let mutable x = 1\nlet main () = (x <- "s"; print_int x)')
add_err('err_dup_case', 'type T = A | A\nlet main () = print_int 0')
add_err('err_bad_pat_payload', 'type T = A of int\nlet main () = print_int (match A 1 with A (x, y) -> x | _ -> 0)')
add_err('err_record_unknown', 'type R = { A: int }\nlet main () = print_int ({ A = 1; B = 2 }.A)')
add_err('err_field_unknown', 'let main () = print_int ((1).foo)')
add_err('err_option_mismatch', 'let main () = print_int (match Some 1 with Some "a" -> 1 | None -> 0)')
add_err('err_infinite_type', 'let f x = x x\nlet main () = print_int (f 1)')
add_err('err_unit_bind', 'let x = ()\nlet main () = print_int 0')
add_err('err_cmp_funcs', 'let f x = x\nlet g x = x\nlet main () = print_bool (f = g)')
add_err('err_int_overflow_type', 'let main () = print_int (2147483648)')
add_err('err_duplicate_def', 'let x = 1\nlet x = 2\nlet main () = print_int x')
add_err('err_match_inexhaustive', 'type T = A | B\nlet f t = match t with A -> 1\nlet main () = print_int (f A)')

# Write positive cases
for name, program, expected in cases:
    with open(f'tests/cases/{name}.fs', 'w', newline='\n') as f:
        f.write(program)
    with open(f'tests/cases/{name}.expected', 'w', newline='\n') as f:
        f.write('\n'.join(expected) + '\n')

# Write error cases
for name, program in errors:
    with open(f'tests/errors/{name}.fs', 'w', newline='\n') as f:
        f.write(program)

print(f'wrote {len(cases)} positive cases and {len(errors)} error cases')
