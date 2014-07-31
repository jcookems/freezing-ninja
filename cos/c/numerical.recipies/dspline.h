// dspline.H   2000/11/4 Jason Cooke
//
// This pulls together the NR cubic spline codes, and adds in stuff to
// scale the x variable.  In particular, with USESEMIINFTAN the range [0,inf)
// is mapped to [-1,1), and the cubic spline is applies to that range.  The
// usespline function then knows what has been done and it scales x
// appropriately before calling the basic spline routine.
//
/////////////////////////////////////////////////////////////

#if !defined(__STD_DSPLINE_H)  // This prevents loading twice!
#define __STD_DSPLINE_H

#include <iostream.h>
#define USENORMALSPLINE 0
#define USESEMIINFTAN   1

// This is dspline.C
void spline(double *x, double *y, int n, double yp1, double ypn, double *y2) {
  if(n<=1) {cout <<"Bad input to routine spline.  n="<<n<<"<=1 !\n"; exit(1); }
  double *u = new double[n+1];
  if (yp1 > 0.99e30) // Pick the boundary conditions
    y2[1]=u[1]=0.0; // "natural"
  else {
    y2[1] = -0.5;   // specified first deriv
    u[1]  = (3.0/(x[2]-x[1]))*((y[2]-y[1])/(x[2]-x[1])-yp1);
  }
  for (int i=2; i<=n-1; i++) {
    double sig = (x[i]-x[i-1])/(x[i+1]-x[i-1]);
    double p   = sig*y2[i-1]+2.0;
    y2[i] = (sig-1.0)/p;
    u[i]  = (y[i+1]-y[i])/(x[i+1]-x[i]) - (y[i]-y[i-1])/(x[i]-x[i-1]);
    u[i]  = (6.0*u[i]/(x[i+1]-x[i-1])-sig*u[i-1])/p;
  }
  double qn, un;
  if (ypn > 0.99e30) // Pick the boundary conditions
    qn=un=0.0; // "natural"
  else {
    qn = 0.5; // specified first deriv
    un = (3.0/(x[n]-x[n-1]))*(ypn-(y[n]-y[n-1])/(x[n]-x[n-1]));
  }
  y2[n]=(un-qn*u[n-1])/(qn*y2[n-1]+1.0);
  for (int k=n-1; k>=1; k--)
    y2[k]=y2[k]*y2[k+1]+u[k];
  delete[] u;
}

// This is dsplint.c
void dsplint(double *xa, double *ya, double *y2a, int n, double x, double *y) {
  if(n<=1) {cout <<"Bad input to routine dsplint. n="<<n<<"<=1 !\n"; exit(1); }
  int klo=1;
  int khi=n;
  int k;
  while (khi-klo > 1) { // find the right place in the table by bisection
    k = (khi+klo) >> 1; // This is div by 2
    if (xa[k] > x) khi=k; else klo=k;
  }
  double h = xa[khi]-xa[klo]; // must have distict xa's
  if (h == 0.0) { cout << "Bad XA input to routine SPLINT\n"; exit(1); }
  double a = (xa[khi]-x)/h;
  double b = (x-xa[klo])/h; // Now can eavaluate the cubic spline
  *y = a*ya[klo]+b*ya[khi]+((a*a*a-a)*y2a[klo]+(b*b*b-b)*y2a[khi])*(h*h)/6.0;
}

// Now we add in my code for easy interfacting

class SplineData {
public:
  int n;
  double *x, *y, *y2, yp1, ypn;
  int whichspline;
  double delta;
  SplineData(int inwhichspline = 0, double indelta = 1.) {
    whichspline = inwhichspline;
    delta       = indelta;
  }
};

// These should be methods of the class SplineData.

void makespline(double *x, double *y, int n, double yp0, double ypnm1,
		SplineData *spd) {
  (*spd).n   = n;
  (*spd).yp1 = yp0;
  (*spd).ypn = ypnm1;
  (*spd).x   = new double[(*spd).n+1];
  (*spd).y   = new double[(*spd).n+1];
  (*spd).y2  = new double[(*spd).n+1];
  for(int i=1;i<=(*spd).n;i++) {
    (*spd).x[i] = x[i-1];
    (*spd).y[i] = y[i-1];
  }
  spline((*spd).x, (*spd).y, (*spd).n, (*spd).yp1, (*spd).ypn, (*spd).y2);
} // makespline

void makespline(double *x, double *y, int n, double yp0, double ypnm1,
		int whichspline, double delta,
		double y0, double yinf,
		SplineData *spd) {
  // This adds value by allowing for different types of splines
  if(whichspline==USENORMALSPLINE)
    makespline(x, y, n, yp0, ypnm1, spd);
  else if(whichspline==USESEMIINFTAN) {
    // Use the sorta-cubic spline to interpolate.
    double *atanx = new double[n+2];
    double *newy  = new double[n+2];
    for(int i=0;i<n;i++) {
      atanx[i+1] = 4./M_PI*atan(x[i]/delta) - 1.;
      newy[i+1]  = y[i];
    }
    atanx[0]   = -1.;    newy[0]   = y0;
    atanx[n+1] = +1.;    newy[n+1] = yinf;
    makespline(atanx, newy, n+2, yp0, ypnm1, spd);
    delete[] atanx;
    delete[] newy;
  } else {
    cout << "In makespline, passed whichspline="<<whichspline<<"!\n";
    cout << "Don't know how to deal with that!  Exiting...\n";
    exit(1);
  }
  (*spd).whichspline = whichspline;
  (*spd).delta       = delta;
} // makespline

double usespline(SplineData spd, double x) {
  double y;
  if(spd.whichspline==USENORMALSPLINE)
    dsplint(spd.x, spd.y, spd.y2, spd.n, x, &y);
  else if(spd.whichspline==USESEMIINFTAN)
    dsplint(spd.x, spd.y, spd.y2, spd.n, 4./M_PI*atan(x/spd.delta) - 1., &y);
  else {
    cout << "In usespline, passed whichspline="<<spd.whichspline<<"!\n";
    cout << "Don't know how to deal with that!  Exiting...\n";
    exit(1);
  }
  return y;
} // usespline

void destroyspline(SplineData spd) {
  spd.n           = 0;
  spd.yp1         = 0.;
  spd.ypn         = 0.;
  spd.whichspline = 0;
  spd.delta       = 0.;
  delete[] spd.x;
  delete[] spd.y;
  delete[] spd.y2;
} // makespline

#endif
