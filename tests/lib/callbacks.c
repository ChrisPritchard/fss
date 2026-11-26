/* C helpers used by tests/cases/c_callbacks.fs to exercise callbacks:
   fss passes a top-level fss function where C expects a function pointer. */
#include "callbacks.h"

int apply(int (*f)(int), int x) { return f(x); }

int apply2(int (*f)(int, int), int a, int b) { return f(a, b); }

void each(void (*f)(int), int n) {
    for (int i = 0; i < n; i++) f(i);
}
