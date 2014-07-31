// test.sphbes.C   2000/6/30  Jason R. Cooke
// 
// To compile, use
// gcc -I/home/jrcooke/c/numerical.recipies test.sphbes.C numerical.recipies/nrutil.c -lm -lstdc++
// 
// It passes the test!
/////////////////////////////////////////////////////////////////////
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <iostream.h>
#include <iomanip.h>

#include "zchebev.c"
#include "zbeschb.c"
#include "zbessjy.c"
#include "zsphbes.c"
#include "nrutil.c"

int main(){
  double x = 0.43;
  for(int n=0; n<6; n++) {
    double sj,sy,sjp,syp;
    zsphbes(n,x,&sj,&sy,&sjp,&syp);
    cout << "j_"<<n<<"("<<x<<")="<<sj<<"\n";
  }
  exit(0);
}

