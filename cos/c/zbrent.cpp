/* This is Numerical Recipies zbrent.c, modified somewhat by JRC. */
#if !defined(__STD_ZBRENT_H_CPP)  // This prevents loading twice!
#define __STD_ZBRENT_H_CPP

#include <math.h>
#include <iostream.h>
#define ITMAX 100
#define EPS 3.0e-8

double zbrent(double (*func)(double), double x1,double x2,double tol)
{
  int iter;
  double a=x1,b=x2,c=x2,d,e,min1,min2;
  double fa=(*func)(a),fb=(*func)(b),fc,p,q,r,s,tol1,xm;
  
  fc=fb;
  if (fb*fa > 0.0) {
    cout << "Root must be bracketed in ZBRENT\n";
    exit(10);
  };
  fc=fb;
  e=d=b-a; /* this is not really needed, but error checkers want it here */
  for (iter=1;iter<=ITMAX;iter++) {
    if (fb*fc > 0.0) {
      c=a;
      fc=fa;
      e=d=b-a;
    }
    if (fabs(fc) < fabs(fb)) {
      a=b;
      b=c;
      c=a;
      fa=fb;
      fb=fc;
      fc=fa;
    }
    tol1=2.0*EPS*fabs(b)+0.5*tol;
    xm=0.5*(c-b);
    if (fabs(xm) <= tol1 || fb == 0.0) return b;
    if (fabs(e) >= tol1 && fabs(fa) > fabs(fb)) {
      s=fb/fa;
      if (a == c) {
	p=2.0*xm*s;
	q=1.0-s;
      } else {
	q=fa/fc;
	r=fb/fc;
	p=s*(2.0*xm*q*(q-r)-(b-a)*(r-1.0));
	q=(q-1.0)*(r-1.0)*(s-1.0);
      }
      if (p > 0.0)  q = -q;
      p=fabs(p);
      min1=3.0*xm*q-fabs(tol1*q);
      min2=fabs(e*q);
      if (2.0*p < (min1 < min2 ? min1 : min2)) {
	e=d;
	d=p/q;
      } else {
	d=xm;
	e=d;
      }
    } else {
      d=xm;
      e=d;
    }
    a=b;
    fa=fb;
    if (fabs(d) > tol1)
      b += d;
    else
      b += (xm > 0.0 ? fabs(tol1) : -fabs(tol1));
    fb=(*func)(b);
  }
  cout << "Maximum number of iterations exceeded in ZBRENT\n";
  exit(10);
  return 0.0;
} /* zbrent */

double zbrentmod(double (*func)(double),
		 double x1, double f1inp,
		 double x2, double f2inp, double tol)
{
  /* The reason this is modified is that we sometimes have 
     already calculated the funcation at points x1 and x2  */
  int iter;
  double a=x1,b=x2,c=x2,d,e,min1,min2;
  double fa=f1inp,fb=f2inp,fc=f2inp,p,q,r,s,tol1,xm;
  
  if (fb*fa > 0.0) {
    cout << "Root must be bracketed in ZBRENT\n";
    exit(10);
  };
  fc=fb;
  e=d=b-a; /* this is not really needed, but error checkers want it here */
  for (iter=1;iter<=ITMAX;iter++) {
    if (fb*fc > 0.0) {
      c=a;
      fc=fa;	
      e=d=b-a;
    }
    if (fabs(fc) < fabs(fb)) {
      a=b;
      b=c;
      c=a;
      fa=fb;
      fb=fc;
      fc=fa;
    }
    tol1=2.0*EPS*fabs(b)+0.5*tol;
    xm=0.5*(c-b);
    if (fabs(xm) <= tol1 || fb == 0.0) return b;
    if (fabs(e) >= tol1 && fabs(fa) > fabs(fb)) {
      s=fb/fa;
      if (a == c) {
	p=2.0*xm*s;
	q=1.0-s;
      } else {
	q=fa/fc;
	r=fb/fc;
	p=s*(2.0*xm*q*(q-r)-(b-a)*(r-1.0));
	q=(q-1.0)*(r-1.0)*(s-1.0);
      }
      if (p > 0.0)  q = -q;
      p=fabs(p);
      min1=3.0*xm*q-fabs(tol1*q);
      min2=fabs(e*q);
      if (2.0*p < (min1 < min2 ? min1 : min2)) {
	e=d;
	d=p/q;
      } else {
	d=xm;
	e=d;
      }
    } else {
      d=xm;
      e=d;
    }
    a=b;
    fa=fb;
    if (fabs(d) > tol1)
      b += d;
    else
      b += (xm > 0.0 ? fabs(tol1) : -fabs(tol1));
    fb=(*func)(b);
  };
  cout << "Maximum number of iterations exceeded in ZBRENT\n";
  exit(10);
  return 0.0;
} /* zbrentmod */

#undef ITMAX
#undef EPS
#endif
