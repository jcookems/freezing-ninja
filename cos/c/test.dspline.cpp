// test.dspline.C   2000/6/30  Jason R. Cooke
// 
// To compile, use
// gcc -I/home/jrcooke/c/numerical.recipies test.dspline.C -lm -lstdc++
// 
/////////////////////////////////////////////////////////////////////
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <iostream.h>
#include <iomanip.h>

#include "dspline.C"

int main(){
  int n = 10;
  double *xtab = new double[n];
  double *ytab = new double[n];

  for(int i=0; i<n; i++) {
    xtab[i] = double(i)*2*M_PI/double(n-1);
    ytab[i] = cos(xtab[i]);
  }

  double *y2tab = new double[n];
  dspline(xtab, ytab, n, 0, 0, y2tab);

  int ntest = 500;
  for(int i=0; i<ntest; i++) {
    double x = double(i)*2*M_PI/double(ntest);
    double y = dsplint(xtab,ytab,y2tab,n,x);
    cout << "x="<<setw(15)<<x<<"  ";
    cout << "y="<<setw(15)<<cos(x)<<"  ";
    cout << "csy="<<setw(15)<<y<<"  ";
    cout << "Dy="<<setw(15)<<y-cos(x)<<"\n";
  }

  exit(0);
}

