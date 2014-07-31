// see cos.int.C for comments.
////////////////////////////////////////////////////////////////////

#if !defined(__STD_COS_INT_V2_H)  // This prevents loading twice!
#define __STD_COS_INT_V2_H

#include <iostream> // for cout
using namespace std;

#include <iomanip> // for cout
#define _USE_MATH_DEFINES
#include <math.h>     // for sqrt
#include "gauss.int.H" // for loop integral in cos1n11nintwork

/*
  void cosintminiwork(double a, double b, int absm, double *c, double *cint);
  double facforderivs(int n, double aIn, double cIn, int absm);
  void cosintwork(double a, double b, int m,
  double *int1, double *c);
  double cosintwork(double a1, double a2, double b,
  int maxLoop, double sizeDelta, double prec,
  int n1, int n2, int absm);
  double cos11nintwork(double a1, double a2, double a3, double b,
  int maxLoop,
  double sizeDelta13,
  double sizeDelta12,
  double prec, int sw,
  int n3, int absm);
  double cos1n11nintwork(double a11, double a12, double b1, int n1, int absm1,
  double aM,  double bM1, double bM2,
  double a21, double a22, double b2, int n2, int absm2,
  int num1);
  double cosintwork(double a1, double a2, double b, int n1, int n2, int absm);
  double cos11nintwork(double a1, double a2, double a3,
  double b, int n3, int absm);
  double cos1n11nintwork(double a11, double a12, double b1, int n1, int absm1,
  double aM,  double bM1, double bM2,
  double a21, double a22, double b2, int n2, int absm2);
*/

double cosint(double a, double b, int n, int m);
double cosint(double a, double b, int m);
void cosint(double a,double b,int m, double *int1, double *int2, double *int3);
double cos12int(double a1, double a2, double b, int m);
double cos14int(double a1, double a2, double b, int m);
double cosint(double a1, double a2, double b, int n1, int n2, int m);
double cos1nnint(double a1, double a2, double a3, double b, 
		 int n2, int n3, int m);
double cosnn1nnint(double a11,double a12, double b1, int n11, int n12, int m1,
		   double aM, double bM1, double bM2,
		   double a21,double a22, double b2, int n21, int n22, int m2);

#endif

