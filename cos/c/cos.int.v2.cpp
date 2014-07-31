// 2001/6/24 cos.int.v2.C
//   7/18 Fixed up some of the routines to take more general n's. Added some
//        tests to make sure n>0, and if not (which is a more trivial case)
//        fall down to an integration routine with less n's.
//
// 2001/3/21 Power -> pow, removed dependence on jutils.H
//   Trying out try, throw, catch.  Have to compile with -fhandle-exceptions
// cos.int.H    2000/2/29, Leap day on a century year!
// cos.int.h    2000/1/30, Super Bowl Sunday!   Jason Cooke
//
// The mathematics behind this are located cos.int* located throught the
// hard drive.
//
// gcc -c -O4 cos.int.v2.C
//
// This is based on lan.v2 ,and make.h1.c.
////////////////////////////////////////////////////////////////////

#include "cos.int.v2.h"

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

double facforderivs(int n, double aIn, double cIn, int absm) {
  // this is f_n from the LaTeX
  //
  // f_1 = 1
  // f_2 = ( a + |m| c )/c^2
  // f_n = ( f_2 * f_{n-1} - \frac{d}{da} f_{n-1} )/(n-1)
  //
  // We need lots of these for the power series expansion to approximate
  // the cos12 and cos14 terms.
#define MAXNFNEW 31
  static double *a = new double[MAXNFNEW];
  static double *c = new double[MAXNFNEW];
  static double *m = new double[MAXNFNEW];
  static double *oocsq = new double[MAXNFNEW];
  static int   prevn = 0;
  if(prevn==0) {
    a[1] = 0.; // This marks this as the start
    c[1] = 0.;
    m[1] = 0;
  }
  
  if(a[1]!=aIn || c[1]!=cIn || m[1]!=absm ) {
    a[1] = aIn;
    c[1] = cIn;
    m[1] = double(absm);
    oocsq[1] = 1./(cIn*cIn);
    prevn = 1;
  }

  if(n>prevn) // have to do some new calcuation
    for(int j=prevn+1;j<=n;j++) {
      a[j]     =     a[j-1]    *a[1];
      c[j]     =     c[j-1]    *c[1];
      m[j]     =     m[j-1]    *m[1];
      oocsq[j] = oocsq[j-1]*oocsq[1];
    }
  prevn=n;

#include "cos.int.F.v2.cpp" // this is what actually calcualtes. Too bulky.

#undef MAXNFNEW
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

double cosintwork(double a, double b, int n, int absm) {
  // \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^n =
  // f_n \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^n =
  if(n==0) // this is trivial
    return (absm ? 0. : 1.);
  else { // do some work
    double c, cint; cosintminiwork(a,b,absm,&c,&cint);
    return facforderivs(n,a,c,absm)*cint;
  }
} // cosintwork

double cosintwork(double a1, double a2, double b,
		  int maxLoop, double sizeDelta, double prec,
		  int n1, int n2, int absm) {
  /* the integral 
     \int d\phi/2\pi exp(i m \phi)/(a1+b\cos\phi)^n1(a2+b\cos\phi)^n2
  */

  /*  cout << "cosintwork(double a1("<<a1
      <<"), double a2("<<a2
      <<"), double b("<<b
      <<"),\n"
      <<"int maxLoop("<<maxLoop
      <<"), double sizeDelta("<<sizeDelta
      <<"), double prec("<<prec<<"),\n"
      <<"int n1("<<n1
      <<"), int n2("<<n2
      <<"), int absm("<<absm<<"))\n";
      */

  //  if(n1==0) cout << "n1=0, "<<cosintwork(a2,b,n2,absm)<<"\n";
  //  if(n2==0) cout << "n2=0, "<<cosintwork(a1,b,n1,absm)<<"\n";

  if(n1==0) return cosintwork(a2,b,n2,absm);
  if(n2==0) return cosintwork(a1,b,n1,absm);

  double ret = 0.;

  double delta = a2-a1;
  if( fabs(delta)<sizeDelta*fabs(a1+a2)*0.5 ) { // delta < sizeDelta ave(a1,a2)
    if(n1>n2)
      ret = cosintwork(a2,a1,b,maxLoop,sizeDelta,prec,n2,n1,absm);
    else {
      double c2, cint2; cosintminiwork(a2,b,absm,&c2,&cint2);
      double deltaTimesBinom = 1.;
      double pertg2 = facforderivs(n1+n2,a2,c2,absm);
      for(int j=1;j<=maxLoop;j++) {
	deltaTimesBinom *= delta*(n1-1.+j)/j;
	double tmp = deltaTimesBinom*facforderivs(n1+n2+j,a2,c2,absm);
	pertg2 += tmp;
	if(fabs(tmp)<1.e-20 && fabs(pertg2)<1.e-20) break;
	if(fabs(tmp/pertg2) < prec) break;
      }
      ret = pertg2*cint2;
    }
  } else { // different enough to use the usual expression
    double c1, cint1; cosintminiwork(a1,b,absm,&c1,&cint1);
    double c2, cint2; cosintminiwork(a2,b,absm,&c2,&cint2);
    //cout << "cint1="<<cint1<<"\n";
    //cout << "cint2="<<cint2<<"\n";

    double fn, f1, binom;
    
    binom = 1.;
    f1 = fn = 1./(a1-a2); for(int j=1;j<n2;j++) fn*=f1;
    double g1 = 0.;
    for(int j=0;j<n1;j++) {
      g1    += binom*facforderivs(n1-j,a1,c1,absm)*fn; 
      fn    *= f1;
      binom *= (j+n2)/(j+1.);
    }
    g1 *= (n2&1?-1:1);
    
    binom = 1.;
    f1 = fn = 1./(a2-a1); for(int j=1;j<n1;j++) fn*=f1;
    double g2 = 0.;
    for(int j=0;j<n2;j++) {
      g2    += binom*facforderivs(n2-j,a2,c2,absm)*fn;
      fn    *= f1;
      binom *= (j+n1)/(j+1.);
    }
    g2 *= (n1&1?-1:1);
    //    cout << "g1="<<g1<<"\n";
    //cout << "g2="<<g2<<"\n";
    
    ret = g1*cint1 + g2*cint2;
    //    cout << "return value: "<< ret << "\n";

  }

  return ret;
} // cosintwork

double cos1nnintwork(double a1, double a2, double a3, double b,
		     int maxLoop,
		     double sizeDelta13,
		     double sizeDelta12,
		     double prec, int sw,
		     int n2, int n3, int absm) {
  /* the integral 
     \int d\phi/2\pi exp(i m \phi)
     1/(a1+b\cos\phi)(a2+b\cos\phi)(a3+b\cos\phi)^n3
  */

  if(n2>n3) return cos1nnintwork(a1,a3,a2,b,
				 maxLoop,sizeDelta13,sizeDelta12,
				 prec, sw, n3, n2, absm);
  
  if(n2==0) return cosintwork(a1,a3,b,maxLoop,sizeDelta13,prec,1,n3,absm);
  if(n2!=1) { cout << "Error in cos1nnintwork! n2="<<n2<<"!\n\a"; exit(1); }

  // Note that n3 cannot be zero now, since n2<=n3, that would imply
  // n2 also is zero, in which case we were already whisked away to cosintwork!

  double ret = 0.;
  
  if( ( (sw==0) && fabs(a2-a3)<sizeDelta13*fabs(a2+a3)*0.5 )
      || (sw == 1) ) { // delta < sizeDelta ave(a1,a3)
    double deltapow, delta = deltapow = a3-a2;
    ret += cosintwork(a1,a3,b,maxLoop,sizeDelta13,prec,1,n3+1,absm);
    for(int j=1;j<=maxLoop;j++) {
      double tmp = deltapow*cosintwork(a1,a3,b,maxLoop,sizeDelta13,prec,
				       1,n3+1+j,absm);
      ret += tmp;
      if(fabs(tmp)<1.e-20 && fabs(ret)<1.e-20) break;
      if(fabs(tmp/ret) < prec) break;
      deltapow *= delta;
    }
  } else if( ( (sw==0) && fabs(a1-a3)<sizeDelta13*fabs(a1+a3)*0.5 )
	     || (sw == 2) ) { // same but 1<->2
    double deltapow, delta = deltapow = a3-a1;
    ret += cosintwork(a2,a3,b,maxLoop,sizeDelta13,prec,1,n3+1,absm);
    for(int j=1;j<=maxLoop;j++) {
      double tmp = deltapow*cosintwork(a2,a3,b,maxLoop,sizeDelta13,prec,
				       1,n3+1+j,absm);
      ret += tmp;
      if(fabs(tmp)<1.e-20 && fabs(ret)<1.e-20) break;
      if(fabs(tmp/ret) < prec) break;
      deltapow *= delta;
    }
  } else if( ( (sw==0) && fabs(a1-a2)<sizeDelta12*fabs(a1+a2)*0.5 )
	     || (sw==3) ) { // if 1&2 close
    double deltapow, delta = deltapow = a2-a1;
    ret += cosintwork(a1,a3,b,maxLoop,sizeDelta13,prec,2,n3,absm);
    for(int j=1;j<=maxLoop;j++) {
      double tmp = deltapow*cosintwork(a1,a3,b,maxLoop,sizeDelta13,prec,
				       2+j,n3,absm);
      ret += tmp;
      if(fabs(tmp)<1.e-20 && fabs(ret)<1.e-20) break;
      if(fabs(tmp/ret) < prec) break;
      deltapow *= delta;
    }
  } else { // different enough to use the usual expression
    double c1, cint1; cosintminiwork(a1,b,absm,&c1,&cint1);
    double c2, cint2; cosintminiwork(a2,b,absm,&c2,&cint2);
    double c3, cint3; cosintminiwork(a3,b,absm,&c3,&cint3);
    
    double g1 = 1./(a2-a1); // simple since n1=n2=1
    double g2 = 1./(a1-a2);    
    double g3 = 0.;
    double oo1m3X2m3ip1 = 1./((a1-a3)*(a2-a3));
    for(int i=0; i<n3; i++) {
      g1 /= (a3-a1); // simple since n1=n2=1
      g2 /= (a3-a2);

      double innersum = 0.;
      double oo1m3jp1X2m3imjp1 = oo1m3X2m3ip1;
      for(int j=0; j<=i; j++) {
	innersum += oo1m3jp1X2m3imjp1;
	oo1m3jp1X2m3imjp1 *= (a2-a3)/(a1-a3);
      }
      oo1m3X2m3ip1 /= (a2-a3);
      g3 += (i&1?-1:1)*facforderivs(n3-i,a3,c3,absm)*innersum;
    }

    ret = g1*cint1 + g2*cint2 + g3*cint3;
  }

  return ret;
} // cos1nnintwork

double
cosnn1nnintwork(double a11, double a12, double b1, int n11, int n12, int absm1,
		double aM,  double bM1, double bM2,
		double a21, double a22, double b2, int n21, int n22, int absm2,
		int num1) {
  /* This is 
     int_0^2pi dphi1 e^{i m1 phi1} 
     int_0^2pi dphi2 e^{i m2 phi2} 
     1/( (a11+b1*cos(phi1))^n11 *(a21+b1*cos(phi1))^n12 )
     1/( (aM+bM1*cos(phi1)+bM2*cos(phi2)) )
     1/( (a12+b2*cos(phi2))^n21 *(a22+b2*cos(phi2))^n22 )
  */
  // We're workin' without a net here, not checking if we are near a pole.
  // But I've got to get finished up....
  static int maxM = 10;
  static double **cj = 0;
  static double **sj = 0;
  static int maxNUM1 = 200;
  static double **listxphi = 0;
  static double **listwphi = 0;
  if(cj==0) {
    cj = new double*[maxM];
    sj = new double*[maxM];
    for(int mT=0; mT<maxM; mT++) {
      cj[mT] = new double[2*mT]; 
      sj[mT] = new double[2*mT];
      for(int j=0;j<2*mT;j++) {
	cj[mT][j] = cos((M_PI*j)/mT);
	sj[mT][j] = sin((M_PI*j)/mT);
      }
    }
    listxphi = new double*[maxNUM1]; for(int i=0;i<maxNUM1;i++) listxphi[i]=0;
    listwphi = new double*[maxNUM1]; for(int i=0;i<maxNUM1;i++) listwphi[i]=0;
  }

  if(absm1>=maxM){
    cout<<"Error in cosintInt! absm1="<<absm1<<" is too large!\n"; exit(1);
  }

  int nphi1 = int(ceil(num1/(1+absm1)));
  double *xphi = listxphi[nphi1];
  double *wphi = listwphi[nphi1];
  if(xphi==0) {
    makearbweights(GINTGLEG,nphi1,
		   &(listxphi[nphi1]),
		   &(listwphi[nphi1]),0.,M_PI);
    for(int i=0;i<nphi1;i++) listwphi[nphi1][i] /= M_PI;
    xphi = listxphi[nphi1];
    wphi = listwphi[nphi1];
  }

  double ret = 0.;
  for(int iphi1=0; iphi1 < nphi1; iphi1++) {
    double phi1 = xphi[iphi1];
    double tmp = 0.;
    if(absm1==0) {
      double c = cos(phi1);
      double b2obM2 = (fabs(b2-bM2)>1.e-10*fabs(aM) ? b2/bM2 : 1.);
      double tmpIG = b2obM2*
	cos1nnint(b2obM2*(aM+bM1*c),a21,a22,b2,n21,n22,absm2);
      //      cout << "tmpIG " << tmpIG<<"\n";
      
      //      cout << b2obM2 << "\n";
      //      cout << 1./(b2obM2*(aM+bM1*c)) << "\n";
      //      cout << 1./((aM+bM1*c)) << "\n";
      

      if(n11!=0){double pow=1./(a11+b1*c);for(int k=0;k<n11;k++)tmpIG *=pow;}
      if(n12!=0){double pow=1./(a12+b1*c);for(int k=0;k<n12;k++)tmpIG *=pow;}
      tmp += tmpIG;
    } else {
      double oo2m = 1./(2*absm1);
      double fac  = cos(phi1*0.5)*oo2m;
      double c2om = cos(phi1*oo2m);
      double s2om = sin(phi1*oo2m);
      double b2obM2 = (fabs(b2-bM2)>1.e-10*fabs(aM) ? b2/bM2 : 1.);
      for(int j = 0; j< 2*absm1; j++) {
	double c = c2om*cj[absm1][j]-s2om*sj[absm1][j]; 
	// cos((Phi+2*M_PI*j)/(2*m));
	double tmpIG = b2obM2*
	  cos1nnint(b2obM2*(aM+bM1*c),a21,a22,b2,n21,n22,absm2);
	if(n11!=0){double pow=1./(a11+b1*c);for(int k=0;k<n11;k++)tmpIG *=pow;}
	if(n12!=0){double pow=1./(a12+b1*c);for(int k=0;k<n12;k++)tmpIG *=pow;}
	tmp += fac*tmpIG;
	fac *= -1.;
      }
    }
    ret += wphi[iphi1] * tmp;
  }
  return ret;
} // cosnn1nnintwork

////////////////////////////////////////////////////////////////////
// -- The 2.5 level of abstraction ------------------------------ //
////////////////////////////////////////////////////////////////////

double cosintwork(double a1, double a2, double b, int n1, int n2, int absm) {
  double aa1 = fabs(a1);
  double aa2 = fabs(a2);
  double delta = -0.02*log(fabs(b)/(aa1<aa2 ? aa1 : aa2)+1.e-20);
  if(delta > 0.01) delta = 0.01;
  // The equation for delta is strange, but it seems to do better than 
  // just picking a fixed value for delta.
  int maxLoop=15;
  double prec = 1.e-8;  
  return cosintwork(a1,a2,b,maxLoop,delta,prec,n1,n2,absm);
}

double cos1nnintwork(double a1, double a2, double a3,
		     double b, int n2, int n3, int absm) {
  double aa1 = fabs(a1);
  double aa2 = fabs(a2);
  double aa3 = fabs(a3);
  double mina = (aa1<aa2 ? aa1 : aa2); mina = (mina<aa3 ? mina : aa3);

  double delta13 = -0.02*log(fabs(b)/mina+1.e-20);
  double delta12 = 0.00001*delta13;

  if(delta13 > 0.01) delta13 = 0.01;
  // The equation for delta is strange, but it seems to do better than 
  // just picking a fixed value for delta.
  int maxLoop=15;
  double prec = 1.e-8;  
  int sw = 0;
  return cos1nnintwork(a1,a2,a3,b,maxLoop,delta13,delta12,prec,sw,n2,n3,absm);
}

double
cosnn1nnintwork(double a11,double a12, double b1, int n11, int n12, int absm1,
		double aM, double bM1, double bM2,
		double a21,double a22, double b2, int n21, int n22, int absm2){
  int num1 = 10;
  double min1 = (n11? (fabs(a11)<fabs(a12)?a11:a12) : a12);
  double min2 = (n21? (fabs(a21)<fabs(a22)?a21:a22) : a22);
  if(fabs(b1/min1)<fabs(b2/min2))
    return cosnn1nnintwork(a11,a12,b1,n11,n12,absm1,
			   aM ,bM1 ,bM2,
			   a21,a22,b2,n21,n22,absm2,
			   num1);
  else
    return cosnn1nnintwork(a21,a22,b2,n21,n22,absm2,
			   aM ,bM2 ,bM1,
			   a11,a12,b1,n11,n12,absm1,
			   num1);
} // cosnn1nnintwork

// -- The thrid level of abstraction.  We call these. -----------------------
// -- The second cosint is different from the others. -----------------------

double cosint(double a, double b, int n, int m) {
  return cosintwork(a,b,n,(m<0 ? -m : m));
} // cosint

double cosint(double a, double b, int m) {
  return cosintwork(a,b,0,(m<0 ? -m : m));
} // cosint

void cosint(double a, double b, int m,
	    double *int1, double *int2, double *int3) {
  // This calculates the integrals needed for the box diagrams in 
  // Wallace and Mandelzweig's approximation of the Green's function.
  // See my second paper for exactly what is going on.
  int absm (m<0 ? -m : m); // = abs(m);
  double c;  cosintwork(a,b,absm,int1,&c);
  // int2 = \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^2 = int1 * f_2
  *int2 = *int1 * facforderivs(2,a,c,absm);
  // int2 = \int d\phi/2\pi exp(i m \phi)/(a+b\cos\phi)^3 = int1 * f_3
  *int3 = *int1 * facforderivs(3,a,c,absm);
} // cosint

double cos12int(double a1, double a2, double b, int m) {
  return cosintwork(a1,a2,b,1,2,(m<0 ? -m : m));
} // cos12int

double cos14int(double a1, double a2, double b, int m) {
  return cosintwork(a1,a2,b,1,4,(m<0 ? -m : m));
} // cos14int

double cosint(double a1, double a2, double b, int n1, int n2, int m) {
  return cosintwork(a1,a2,b,n1,n2,(m<0 ? -m : m));
} // cosint

double cos1nnint(double a1, double a2, double a3, double b, 
		 int n2, int n3, int m) {
  return cos1nnintwork(a1,a2,a3,b,n2,n3,(m<0 ? -m : m));
} // cos11nint

class mexp {
public:
  mexp(double xin) { x=xin; };
  static void setprec(int precin) { prec=precin; };
  static int  getprec() { return prec; };
  friend ostream& operator << (ostream& o, const mexp& f);
private:
  static int prec;
  double x;
};
int mexp::prec = 6; // Because this really is just a global var.
// mexp::setprec(5); // Changes the precision of the fortran output.
ostream& operator << (ostream &o, const mexp& f) {
  double x = f.x;
  o << (x<0?"-":" ")<<"0."; x *= (x<0.?-1.:1.);
  int expo = (x==0.? 0 : int(floor(log10(x)))+1 );
  int prec = f.prec;
  o << int(x*pow(10.,prec-expo)+0.5);
  o << "*10^" << (expo<0?"-":"+"); if(expo<0) expo= -expo;
  if(expo<10) o << "0";
  o << expo;
  return o;
} // <<


double cosnn1nnint(double a11,double a12, double b1, int n11, int n12, int m1,
		   double aM, double bM1, double bM2,
		   double a21,double a22, double b2, int n21, int n22, int m2){
  double ret = cosnn1nnintwork(a11,a12,b1,n11,n12,(m1<0 ? -m1 : m1),
			       aM ,bM1 ,bM2,
			       a21,a22,b2,n21,n22,(m2<0 ? -m2 : m2));
  if(0) {
    cout << setprecision(10) << setiosflags(ios::fixed);
    // cout << setprecision(10); //  << setiosflags(ios::fixed);
    cout << "{1/(2*Pi)^2*NIntegrate[";
    if(m1) cout << "Cos["<<m1<<"*phi1]*";
    if(m2) cout << "Cos["<<m2<<"*phi2]*";
    cout << "(\n";
    for  (int i1=0;i1<(m1?2*m1:1);i1++) {
      for(int i2=0;i2<(m2?2*m2:1);i2++) {
	cout << "(";
	if((i1+i2)%2==1) cout << "-";
	cout << "1)/(\n";
	if(n11) {
	  cout        << "("<<mexp(a11)
		      << "+"<<mexp(b1 )<< "*Cos[phi1";
	  if(m1) cout << "+"<<i1<<"*Pi/"<<m1;
	  cout        <<"])^"<<n11<<"\n";
	}
	if(n12) { 
	  cout        << "("<<mexp(a12)
		      << "+"<<mexp(b1 )<< "*Cos[phi1";
	  if(m1) cout << "+"<<i1<<"*Pi/"<<m1;
	  cout        <<"])^"<<n12<<"\n";
	}
	cout          << "("<<mexp(aM )
		      << "+"<<mexp(bM1)<< "*Cos[phi1";
	if(m1) cout   << "+"<<i1<<"*Pi/"<<m1;
	cout          <<"]";
	cout          << "+"<<mexp(bM2)<< "*Cos[phi2";
	if(m2) cout   << "+"<<i2<<"*Pi/"<<m2;
	cout          <<"])\n";
	if(n21) {
	  cout        << "("<<mexp(a21)
		      << "+"<<mexp(b2 )<< "*Cos[phi2";
	  if(m2) cout << "+"<<i2<<"*Pi/"<<m2;
	  cout        <<"])^"<<n21<<"\n";
	}
	if(n22) {
	  cout        << "("<<mexp(a22)
		      << "+"<<mexp(b2 )<< "*Cos[phi2";
	  if(m2) cout << "+"<<i2<<"*Pi/"<<m2;
	  cout        <<"])^"<<n22<<"\n";
	}
	
	cout   << ")\n";
	if(i2!=2*m2-1) if(m2) cout << "+\n";
      }
      if(i1!=2*m1-1) if(m1) cout << "+\n";
    }
    cout        << "),{phi1,0";
    if(m1) cout << "+Pi/"<<2*m1;
    cout        <<",2*Pi";
    if(m1) cout <<    "/"<<2*m1;
    if(m1) cout << "+Pi/"<<2*m1;
    cout        <<"},{phi2,0";
    if(m2) cout << "+Pi/"<<2*m2;
    cout        <<",2*Pi";
    if(m2) cout <<    "/"<<2*m2;
    if(m2) cout << "+Pi/"<<2*m2;
    cout        <<"}],\n";
    cout << mexp(ret) << "}\n";
  }
  return ret;
} // cosnn1nnint

#undef MAXNF

