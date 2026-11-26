#include <stdio.h>
#include "mathlib.h"

int main(void) {
    printf("add(2,3)=%d\n", add(2, 3));
    printf("fib(20)=%d\n", fib(20));
    printf("sumTo(100)=%d\n", sumTo(100));
    printf("greet=%s\n", greet("world"));
    printf("hyp=%.0f\n", hyp(3.0, 4.0));
    return 0;
}
