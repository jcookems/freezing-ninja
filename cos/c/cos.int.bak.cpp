// 2001/3/21 Power -> pow, removed dependence on jutils.H
//   Trying out try, throw, catch.  Have to compile with -fhandle-exceptions
// cos.int.H    2000/2/29, Leap day on a century year!
// cos.int.h    2000/1/30, Super Bowl Sunday!   Jason Cooke
//
// The mathematics behind this are located cos.int* located throught the
// hard drive.
//
// This is based on lan.v2 ,and make.h1.c.
////////////////////////////////////////////////////////////////////

#include "cos.int.H"

#define MAXNF 11

// --These routines are the real workhorses ---------------------------------

void cosintminiwork(double  a, double b,    int    absm,
		    double *c, double *cint) {
  // c    =       Sign(a)*sqrt(a^2-b^2)
  // cint = ( (-a+sign(a)*sqrt(a^2-b^2))/b )^m/c
  
  double aSqMbSq = a*a-b*b;
  if(aSqMbSq < 0. || absm < 0) {
    cout << "\aPanic in cos12intminiwork!\n";
    if(aSqMbSq < 0.) cout <<"aSqMbSq = "<<aSqMbSq<<", a= "<<a<<", b="<<b<<"\n";
    if(absm    < 0 ) cout <<"absm = "<<absm<<" < 0!\n";
    exit(10);
  }
  *c = ( a>0. ? 1 : -1 )*sqrt(aSqMbSq); // Sign(a)*Sqrt(a^2-b^2)
  
  double f2m = 1.;
  if(absm>0) {
    double angStuff;
    if(b*b>a*a*1.e-3)  // if b/a is not small then use the full expansion
      // this point was set at random!
      angStuff = (-a+(*c))/b;
    else {/* expand the square root! 
	     c = a *(1 -2*(b/(2*a))^2 -2*(b/(2*a))^4 -4*(b/(2*a))^6 + HOT ) 
	     so
	     angStuff = -b/(2*a)+ HOT
	     The error on this is quite small
	  */
      double bo2a  = b/(2.*a);
      double bo2a2 = bo2a*bo2a;
      angStuff = -bo2a*(1+bo2a2*(1+bo2a2*(2+bo2a2*(5+bo2a2*(14)))));
    }
    for(int i=0;i<absm;i++) // This is my cheap version of exponential
      f2m *= angStuff;
  }
  *cint = f2m/(*c);
} // cosintminiwork

void facforderivs(int n, double a, double b, double c, int absm, double *f) {
  // this is f_n from the LaTeX
  //
  // f_1 = 1
  // f_2 = ( a + |m| sgn(a)sqrt(a^2-b^2) )/(a^2-b^2)
  // f_n = ( f_2 * f_{n-1} - \frac{d}{da} f_{n-1} )/(n-1)
  //
  // We need lots of these for the power series expansion to approximate
  // the cos12 and cos14 terms.

  int m=absm; // since really need abs

  f[1] = 1.;
  if(n==1)
    return;
  else {
    double a2 = a*a;
    double b2 = b*b;
    double a2b21 = a2-b2;
    f[2] = (a + c*m)/a2b21;
    if(n==2)
      return;
    else {
      double c2 = c*c;
      int    m2 = m*m;
      double a2b22 = a2b21*a2b21;
      f[3] = (2*a2 + b2 + 3*a*c*m + c2*m2)/(2.*a2b22);
      if(n==3)
	return;
      else {
	double a3 = a2*a;
	double b3 = b2*b;
	double c3 = c2*c;
	int    m3 = m2*m;
	double a2b23 = a2b22*a2b21;
	f[4] = (6*a3 + 9*a*b2 + 11*a2*c*m + 4*b2*c*m + 6*a*c2*m2 + c3*m3)/
	  (6.* a2b23);
	if(n==4)
	  return;
	else {
	  double a4 = a3*a;
	  double b4 = b3*b;
	  double c4 = c3*c;
	  int    m4 = m3*m;
	  double a2b24 = a2b23*a2b21;
	  f[5] = (24*a4 + 9*b4 + 50*a3*c*m + 10*b2*c2*m2 + 
		  c4*m4 + a2*(72*b2 + 35*c2*m2) + 
		  5*a*(11*b2*c*m + 2*c3*m3))/(24.*a2b24);
	  if(n==5)
	    return;
	  else {
	    double a5 = a4*a;
	    double b5 = b4*b;
	    double c5 = c4*c;
	    int    m5 = m4*m;
	    double a2b25 = a2b24*a2b21;
	    f[6] = (120*a5 + 274*a4*c*m + 64*b4*c*m + 20*b2*c3*m3 + c5*m5 + 
		    75*a3*(8*b2 + 3*c2*m2) + a2*(607*b2*c*m + 85*c3*m3) + 
		    15*a*(15*b4 + 13*b2*c2*m2 + c4*m4))/(120.*a2b25);
	    if(n==6)
	      return;
	    else {
	      double a6 = a5*a;
	      double b6 = b5*b;
	      double c6 = c5*c;
	      int    m6 = m5*m;
	      double a2b26 = a2b25*a2b21;
	      f[7] = (720*a6 + 225*b6 + 1764*a5*c*m + 259*b4*c2*m2 + 
		      35*b2*c4*m4 + c6*m6 + 8*a4*(675*b2 + 203*c2*m2) + 
		      21*a3*(312*b2*c*m + 35*c3*m3) + 
		      a2*(4050*b4 + 2842*b2*c2*m2 + 175*c4*m4) + 
		      21*a*(99*b4*c*m + 25*b2*c3*m3 + c5*m5))/(720.*a2b26);
	      if(n==7)
		return;
	      else {
		double a7 = a6*a;
		double b7 = b6*b;
		double c7 = c6*c;
		int    m7 = m6*m;
		double a2b27 = a2b26*a2b21;
		f[8] = (5040*a7 + 13068*a6*c*m + 2304*b6*c*m + 784*b4*c3*m3 +
			56*b2*c5*m5 + c7*m7 + 196*a5*(270*b2 + 67*c2*m2) + 
			a4*(73188*b2*c*m + 6769*c3*m3) + 
			14*a3*(4725*b4 + 2759*b2*c2*m2 + 140*c4*m4) + 
			a2*(46575*b4*c*m + 9772*b2*c3*m3 + 322*c5*m5) + 
			7*a*(1575*b6 + 1516*b4*c2*m2 + 170*b2*c4*m4 +
			     4*c6*m6))/(5040.*a2b27);
		if(n==8)
		  return;
		else {
		  double a8 = a7*a;
		  double b8 = b7*b;
		  double c8 = c7*c;
		  int    m8 = m7*m;
		  double a2b28 = a2b27*a2b21;
		  f[9] = (40320*a8 + 11025*b8 + 109584*a7*c*m +
			  12916*b6*c2*m2 + 1974*b4*c4*m4 + 84*b2*c6*m6 + 
			  c8*m8 + 4*a6*(141120*b2 + 29531*c2*m2) + 
			  36*a5*(23978*b2*c*m + 1869*c3*m3) + 
			  3*a4*(352800*b4 + 175036*b2*c2*m2 + 7483*c4*m4) + 
			  18*a3*(50989*b4*c*m + 9079*b2*c3*m3 + 252*c5*m5) + 
			  3*a2*(117600*b6 + 96599*b4*c2*m2 + 9184*b2*c4*m4 + 
				182*c6*m6) + 
			  9*a*(15159*b6*c*m + 4396*b4*c3*m3 + 266*b2*c5*m5 + 
			       4*c7*m7))/(40320.*a2b28);
		  if(n==9)
		    return;
		  else {
		    double a9 = a8*a;
		    // double b9 = b8*b;
		    double c9 = c8*c;
		    int    m9 = m8*m;
		    double a2b29 = a2b28*a2b21;
		    f[10] = (362880*a9 + 1026576*a8*c*m + 147456*b8*c*m +
			     52480*b6*c3*m3 + 4368*b4*c5*m5 + 120*b2*c7*m7 +
			     c9*m9 + 180*a7*(36288*b2 +6515*c2*m2) + 
			     8*a6*(1353357*b2*c*m + 90460*c3*m3) + 
			     45*a5*(381024*b4 + 163292*b2*c2*m2 +
				    5985*c4*m4) + 
			     3*a4*(5768622*b4*c*m + 886390*b2*c3*m3 + 
				   21091*c5*m5) + 
			     45*a3*(211680*b6 + 150791*b4*c2*m2 + 
				    12362*b2*c4*m4 + 210*c6*m6) + 
			     3*a2*(1717557*b6*c*m + 431465*b4*c3*m3 + 
				   22498*b2*c5*m5 + 290*c7*m7) + 
			     45*a*(19845*b8 + 20217*b6*c2*m2 + 
				   2674*b4*c4*m4 + 98*b2*c6*m6 + c8*m8))/
		      (362880.*a2b29);
		    if(n==10)
		      return;
		    else {
		      cout << "\aError!  In facforderivs, n=" << n << "\n";
		      exit(20);
		      // Need to change MAXNF if you add more!
		    }
		  }
		}
	      }
	    }
	  }
	}
      }
    }
  }
} // facforderivs

double facforderivs(int n, double a, double b, double c, int absm) {
  static double *f = new double[MAXNF];
  facforderivs(n,a,b,c,absm,f);
  return f[n];
} // facforderivs


// -- Now the next level up. ------------------------------------------------
// -- Not much is needed for cosint -----------------------------------------
// -- More work is needed for cos12int and cos14int -------------------------

void cosintwork(double a, double b, int m,
		double *int1, double *c) {
  // \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi) =
  // Sign(a)/sqrt(a^2-b^2) ( (-a+sign(a)sqrt(a^2-b^2))/b )^|m|
  
  // This is slightly different from the next two, since we sometimes 
  // want to look at some different integrals.

  int absm (m<0 ? -m : m); // = abs(m);
  cosintminiwork(a,b,absm,c,int1);
} // cosintwork

double cos12intwork(double a1, double a2, double b, int absm) {
  /* the integral 
     \int d\phi/2\pi exp(i m \phi)/(a1+b\cos\phi)(a2+b\cos\phi)^2 =
     1/( (a1-a2)^2 * b^m )*     (
     Sign(a1)/sqrt(a1^2-b^2) ( (-a1+sign(a1)sqrt(a1^2-b^2)) )^m   +
     Sign(a2)/sqrt(a2^2-b^2) ( (-a2+sign(a2)sqrt(a2^2-b^2)) )^m   * 
     ( (a1-a2)*(a2+m*sign(a2)sqrt(a2^2-b^2))/( a2*a2 - b*b ) - 1)
  */
  double ret = 0.;

  if( (a1-a2)*(a1-a2)<0.000025*(a1+a2)*(a1+a2) ) { // a1-a2< 1% a1
    double int1,c;
    cosintwork(a2,b,absm,&int1,&c);
    double pertans = facforderivs(3,a2,b,c,absm);

    for(int i=1;i<=5;i++) {
      double temp = pow(a2-a1,i)*facforderivs(3+i,a2,b,c,absm);
      pertans += temp;
    }
    ret = pertans*int1;
  } else { // different enough to use the usual expression
    double c1, cint1;   cosintminiwork(a1,b,absm,&c1,&cint1);
    double c2, cint2;   cosintminiwork(a2,b,absm,&c2,&cint2);

    double f2 = facforderivs(2,a2,b,c2,absm);

    double f11 = 1./(a2-a1);
    double f12 = f11*f11;
    
    double g12 = f11*f2 + f12;
    
    ret = cint1*f12 - cint2*g12;
  }
  return ret;
} // cos12intwork


double cosintwork(double a1, double a2, double b, int n1, int n2, int absm) {
  /* the integral 
     \int d\phi/2\pi exp(i m \phi)/(a1+b\cos\phi)^n1(a2+b\cos\phi)^n2
  */
  static double *f = new double[MAXNF];

  double ret = 0.;

  double delta = a2-a1;
  if( fabs(delta)<0.005*fabs(a1+a2) ) { // delta < 1% a1
    double c2, cint2; cosintminiwork(a2,b,absm,&c2,&cint2);
    int maxLoop = 5;
    facforderivs(n1+n2+maxloop,a2,b,c2,absm,f);
    double deltaTimesBinom = 1.;
    double pertg2 = f[n1+n2];
    for(int j=1;j<=maxLoop;j++) { // 5 seems like a good number
      deltaTimesBinom *= delta*(n1-1.+j)/j;
      pertg2 += deltaTimesBinom*f[n1+n2+j];
    }
    ret = pertg2*cint2;
  } else { // different enough to use the usual expression
    double c1, cint1; cosintminiwork(a1,b,absm,&c1,&cint1);
    double c2, cint2; cosintminiwork(a2,b,absm,&c2,&cint2);

    double fn, f1, binom;

    binom = 1.;
    f1 = fn = 1./(a1-a2); 
    for(int j=1;j<n2;j++) fn*=f1;
    facforderivs(n1+1,a1,b,c1,absm,f);
    double g1 = 0.;
    for(int j=0;j<n1;j++) {
      g1    += binom*f[n1-j]*fn; 
      fn    *= f1;
      binom *= (j+n2)/(j+1.);
    }
    g1 *= (n2&1?-1:1);
    
    binom = 1.;
    f1 = fn = 1./(a2-a1);
    for(int j=1;j<n1;j++) fn*=f1;
    facforderivs(n2+1,a2,b,c2,absm,f);
    double g2 = 0.;
    for(int j=0;j<n2;j++) {
      g2    += binom*f[n2-j]*fn;
      fn    *= f1;
      binom *= (j+n1)/(j+1.);
    }
    g2 *= (n1&1?-1:1);
    
    ret = g1*cint1 + g2*cint2;
  }

  return ret;
} // cosintwork


double cosint11nwork(double a1, double a2, double a3, double b,
		     int n3, int absm) {
  /* the integral 
     \int d\phi/2\pi exp(i m \phi)
        1/(a1+b\cos\phi)(a2+b\cos\phi)(a3+b\cos\phi)^n3
  */
  static double *f = new double[MAXNF];

  double ret = 0.;

  double delta = a2-a1;
  if( fabs(delta)<0.005*fabs(a1+a2) ) { // delta < 1% a1
    double int1,c;
    cosintwork(a2,b,absm,&int1,&c);
    double pertans = facforderivs(3,a2,b,c,absm);

    for(int i=1;i<=5;i++) {
      double temp = pow(a2-a1,i)*facforderivs(3+i,a2,b,c,absm);
      pertans += temp;
    }
    ret = pertans*int1;
  } else { // different enough to use the usual expression
    double c1, cint1; cosintminiwork(a1,b,absm,&c1,&cint1);
    double c2, cint2; cosintminiwork(a2,b,absm,&c2,&cint2);

    double fn, f1, binom;

    binom = 1.;
    f1 = fn = 1./(a1-a2); 
    for(int j=1;j<n2;j++) fn*=f1;
    facforderivs(n1+1,a1,b,c1,absm,f);
    double g1 = 0.;
    for(int j=0;j<n1;j++) {
      g1    += binom*f[n1-j]*fn; 
      fn    *= f1;
      binom *= (j+n2)/(j+1.);
    }
    g1 *= (n2&1?-1:1);
    
    binom = 1.;
    f1 = fn = 1./(a2-a1);
    for(int j=1;j<n1;j++) fn*=f1;
    facforderivs(n2+1,a2,b,c2,absm,f);
    double g2 = 0.;
    for(int j=0;j<n2;j++) {
      g2    += binom*f[n2-j]*fn;
      fn    *= f1;
      binom *= (j+n1)/(j+1.);
    }
    g2 *= (n1&1?-1:1);
    
    ret = g1*cint1 + g2*cint2;
  }

  return ret;
} // cosintwork


double cos14intwork(double a1, double a2, double b, int absm) {
  double ret = 0.;

  if( (a1-a2)*(a1-a2)<0.000025*(a1+a2)*(a1+a2) ) { // a1-a2< 1% a1
    double int1,c;
    cosintwork(a2,b,absm,&int1,&c);   
    double pertans = facforderivs(5,a2,b,c,absm);
    for(int i=1;i<=5;i++) {
      double temp = pow(a2-a1,i)*facforderivs(5+i,a2,b,c,absm);
      pertans += temp;
    }
    ret = pertans*int1;
  } else { // different enough to use the usual expression
    double c1,c2,cint1, cint2;
    cosintminiwork(a1,b,absm,&c1,&cint1);
    cosintminiwork(a2,b,absm,&c2,&cint2);
   
    double f2 = facforderivs(2,a2,b,c2,absm);
    double f3 = facforderivs(3,a2,b,c2,absm);
    double f4 = facforderivs(4,a2,b,c2,absm);
    
    double f11 = 1./(a2-a1);
    double f12 = f11*f11;
    double f13 = f11*f12;
    double f14 = f11*f13;
    
    double g14 = f11*f4 +f12*f3 +f13*f2 +f14;    
    ret = cint1*f14 - cint2*g14;
  }

  return ret;
} // cos14intwork


// -- The thrid level of abstraction.  We call these. -----------------------
// -- The second cosint is different from the others. -----------------------

double cosint(double a, double b, int m) {
  double int1 , c;
  
  cosintwork(a,b,m,&int1,&c);
  return int1;
} // cosint

void cosint(double a, double b, int m,
	    double *int1, double *int2, double *int3) {
  // This calculates the integrals needed for the box diagrams in 
  // Wallace and Mandelzweig's approximation of the Green's function.
  // See my second paper for exactly what is going on.

  double c;

  int absm (m<0 ? -m : m); // = abs(m);
  //  int absm = abs(m);
  cosintwork(a,b,absm,int1,&c);
  // int2 = \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^2 = int1 * f_2
  *int2 = *int1 * facforderivs(2,a,b,c,absm);
  // int2 = \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^3 = int1 * f_3
  *int3 = *int1 * facforderivs(3,a,b,c,absm);
} // cosint

double cos12int(double a1, double a2, double b, int m) {
  int absm (m<0 ? -m : m); // = abs(m);
  double ret;
  ret = cos12intwork(a1,a2,b,absm);
  return ret;
} // cos12int

double cosint(double a1, double a2, double b, int n1, int n2, int m) {
  int absm (m<0 ? -m : m); // = abs(m);
  double ret;
  ret = cosintwork(a1,a2,b,n1,n2,absm);
  return ret;
} // cosint

double cos14int(double a1, double a2, double b, int m) {
  int absm (m<0 ? -m : m); // = abs(m);
  double ret;
  ret = cos14intwork(a1,a2,b,absm);
  return ret;
} // cos14int

double cos114int(double a1, double aM, double a2, double b, int m) {
  int absm (m<0 ? -m : m); // = abs(m);
  double ret;
  ret = cos114intwork(a1,aM,a2,b,absm);
  return ret;
} // cos114int

#undef MAXNF

