/* 
 * To compile this program, use this is the batch file,
 *
 * gcc -ggdb -L/usr/local/lib -L/home/jrcooke/c/CLAPACK eigsys.test.c \
 -o eigsys.test -leispack -llapack -lblas -lf2c -lm
 *
 */

#include <stdio.h>
#include <stdlib.h>
#include <math.h>

#include "/home/jrcooke/c/eigsys.h"

void main(){
    testeigroutines();
}
