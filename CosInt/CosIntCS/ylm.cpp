// ylm.C    1/26/2000  Jason R. Cooke
/////////////////////////////////////////////////////////////////////

#include "ylm.h"

// Start with some ordinary Legendre Polynomial functions ----------------

void dleg(double x, double *pl, int nl) {
  // a modification of the routine fleg.c from Numerical Recipies
  // This gives a list of { P_{l=0), P_{l=1), ... ,P_{l=nl) }
  if(nl<1) return;
  pl[0]=1.0;  if(nl<2) return;
  pl[1]=x;    if(nl<3) return;
  double twox = 2.0*x;
  double f2   = x;
  double d    = 1.0;
  for (int j=2; j<nl; j++) {
    double f1  = d++;
    f2        += twox;
    pl[j]=(f2*pl[j-1]-f1*pl[j-2])/d;
  }
} // dleg

void legp(double *pj, double *pjm1, double x, int j) {
  // COMPUTES THE LEGENDRE POLYNOMS, P_j(x) and P_{j-1}(x), from Machleidt
  *pjm1 = 1.0; if(j<=0) { *pj = 1.0; return; }  // compute for J=0
  *pj = x;     if(j==1) return; // for J=1
  for(int i=2; i<=j; i++) {   // for J>1
    double a = x*(*pj);
    double b = a-(*pjm1);
    *pjm1 = *pj;
    *pj   = -b/i + b + a;
  }
} // legp

double dlegsimp(int j, double x) {
  // COMPUTES THE LEGENDRE POLYNOM, P_j(x), modification of legp above
  if(j<=0) return 1.0;  // J=0
  if(j==1) return x;    // J=1
  double pjm1 = 1.0;
  double pj   = x;
  for(int i=2; i<=j; i++) { // J>1
    double a = x*pj;
    double b = a-pjm1;
    pjm1 = pj;
    pj   = -b/i + b + a;
  }
  return pj;
} // dlegsimp

// Now we have Associated Legendre Polynomials ---------------------------

double dplgndr(int l, int m, double x) {
  // a modification of the routine plgndr.c from Numerical Recipies
  // Computes associated Legendre polynomial P^m_l(x).  Here 
  // m and l satisfy 0 <= m <= l,  -1 <= x <= 1
  if (m < 0 || m > l || fabs(x) > 1.0) {
    cout << "Bad arguments in routine dplgndr!\n"; exit(1);
  }
  double pmm=1.0; // Compute P^m_m
  if (m > 0) {
    double somx2=sqrt((1.0-x)*(1.0+x));
    double fact=1.0;
    for (int i=1;i<=m;i++) { pmm *= -fact*somx2; fact += 2.0; }
  }
  
  if (l == m) return pmm; // we're done
  else { // Compute P^m_{m+1}
    double pmmp1=x*(2*m+1)*pmm;
    if (l == (m+1)) return pmmp1;
    else { // Compute P^m_l, l>m+1
      double pll = pmmp1; // to keep the error checker happy
      for (int ll=(m+2);ll<=l;ll++) {
	pll   = (x*(2*ll-1)*pmmp1-(ll+m-1)*pmm)/(ll-m);
	pmm   = pmmp1;
	pmmp1 = pll;
      }
      return pll;
    }
  }
} // dplgndr

void dplgndrmany(int m, double x, double *p, int nump) {
  // a modification of the routine plgndr.c from Numerical Recipies
  // Computes associated Legendre polynomials
  // { P^m_m(x), P^m_{m+1}(x), ... , P^m_{m+nump-1} }.
  // Here m and l satisfy 0 <= m <= l,  -1 <= x <= 1
  if (m < 0 || fabs(x) > 1.0 || nump < 1) {
    cout << "Bad arguments in routine dplgndrmany!\n";
    cout << "m="<<m<<" and x="<<x<<" and nump="<<nump<<"\n"; exit(1);
  }
  p[0]=1.0; // Compute P^m_m
  if (m > 0) {
    double somx2=sqrt((1.0-x)*(1.0+x));
    double fact=1.0;
    for (int i=1;i<=m;i++) { p[0] *= -fact*somx2; fact += 2.0; }
  }
  
  if (nump!=0) {
    p[1]=x*(2*m+1)*p[0]; // Compute P^m_{m+1}
    for (int ll=(m+2);ll<nump+m;ll++) // and recursively get the rest
      p[ll-m] = (x*(2*ll-1)*p[ll-m-1]-(ll+m-1)*p[ll-m-2])/(ll-m);
  }
} // dplgndr

double asslegpoly(int l,int m, double x) {
  // 2000/11/29
  // severely modified from NR.
  // Computes associated Legendre polynomial P^m_l(x).
  // Here m and l are arbitrary.  We must have -1 < x, but there is no
  // upper limit on x.
  // For |x|<1, we use the usual definition of P.
  // For |x|>1, we multiply by a phase to "keep it real".
  // Note that for x<-1 and x close to -1, there is a pole nearby.
  // This code is probably not too accurate there.
  double absx = fabs(x);
  if(l<0) return asslegpoly(-l-1,m,x); // Use symmetry
  if(m>l) return 0.; // This is def.
  if(m>=-l && m<0) {
    double factrat = 1.; // factrat = (l+|m|)!/(l-|m|)!
    for(int i=l-m;i>l+m;i--) factrat *= double(i);
    return ((m%2)&&(absx<1.0)?-1.:1.)*asslegpoly(l,-m,x)/factrat;
  }
  // Now the real calculation
  double pllm2,pllm1,pll;
  int initl;
  
  // Setup the initial conditions
  if(m>=0) { // Use the NR approach
    initl = m;
    pllm1 = 0.0; // P^m_{m-1} = 0
    pll   = 1.0; // Compute pll = P^m_m
    if (m > 0) {
      double somx2=sqrt((absx>1.0?-1.:1.)*(1.0-x)*(1.0+x));
      double fact=1.0;
      for (int i=1;i<=m;i++) {
	pll *= (x>1.0?1.:-1.)*fact*somx2;
	fact += 2.0;
      }
    }
  } else { // have m<-l.
    initl = 0;
    double somx2=sqrt((absx>1.0?-1.:1.)*(1.0-x)/(1.0+x));
    pll   = somx2; // Compute pll = P^m_0
    for (int i=2;i<=-m;i++) pll *= somx2/double(i); // Compute P^m_0
    pllm1 = pll; // P^m_-1 = P^m_0
  }
  
  // Now use recursion to compute P^m_l
  for (int ll=initl+1;ll<=l;ll++) {
    pllm2 = pllm1;
    pllm1 = pll;
    pll   = ( x*(2*ll-1)*pllm1 - (ll+m-1)*pllm2 )/(ll-m);
  }
  return pll;
} // asslegpoly

// Now the Spherical Harmonics ----------------------------------------

double getYlmDe(int l, int m, double theta) {
  // YlmDe = Y(l,m,\theta)
  // The complex exponential has been factored out (hence De)
  // so that the Ylm is real.
  
  int localm = m;
  double retsgn = 1.;
  if(m<0) { localm = -m; if(m!=2*(m/2)) retsgn=-1.; }
  
  double factinv = 1.;
  for(int i=l-localm+1; i<=l+localm; i++) factinv *= i;
  double ret = retsgn*sqrt( (2.*l+1.)/(4*M_PI*factinv) )*
    dplgndr(l,localm,cos(theta));
  
  return ret;
} // getYlmDe

void getYlmDes(int m, int num, double *thetas, double **YlmDes) {
  // YlmDes = { {Y(l=0) for many thetas},{Y(l=1) for many thetas}, ... }
  // The complex exponential has be factored out (hence De)
  *YlmDes = new double[num*num];
  if(m==0) {
    double *manyN  = new double[num];
    for(int i=0; i<num; i++) {
      dleg(cos(thetas[i]),manyN,num);
      for(int lmm=0; lmm<num; lmm++)
	(*YlmDes)[lmm*num+i] = manyN[lmm]*sqrt( (2*lmm+1.)/(4*M_PI) );
    }
    delete[] manyN;
  } else {
    double twomfac = 1.; for(int i=2*m; i>0; i--) twomfac *= i;
    for(int i=0; i<num; i++) {
      double costhetai = cos(thetas[i]);
      double lmmfacDlpmfac = 1./twomfac;	    
      for(int lmm=0; lmm<num; lmm++) {
	if(lmm!=0) lmmfacDlpmfac *= lmm/(lmm+2.*m);
	(*YlmDes)[lmm*num+i] = dplgndr(lmm+m,m,costhetai)
	  *sqrt( (2*(lmm+m)+1.)/(4*M_PI)*lmmfacDlpmfac ); 
      }
    }
  }
} // getYlmDes

void getYlmDemany(int m, double theta, double *YlmDe, int num) {
  // This gives  YlmDe = { {Y^m_m, Y^m_{m+1}, ... Y^m_{m+num-1} }
  // The complex exponential has be factored out (hence De)
  // Recall that Y^m_l = sqrt( (2*l+1)/(4\pi) * ( (l-m)!/(l+m)! ) ) *
  //                      P^m_l(cos(theta) *
  //                      exp(i * m * phi )
  dplgndrmany(m,cos(theta),YlmDe,num);
  for(int lmm=0; lmm<num; lmm++) YlmDe[lmm] *= sqrt( (2*(lmm+m)+1.)/(4*M_PI) );
  if(m!=0) { // Have to put in the factorial stuff
    double twomfac = 1.; for(int i=2*m; i>0; i--) twomfac *= i; // (2m)!
    double lmmfacDlpmfac = 1./twomfac; // more efficient to do divison once
    // This is the starting factor, when l=m, we get 1/( (2m)! )
    for(int lmm=0; lmm<num; lmm++) {
      if(lmm!=0) lmmfacDlpmfac *= lmm/(lmm+2.*m); // diff of cur and prev
      YlmDe[lmm] *= sqrt(lmmfacDlpmfac);
    }
  }
} // getYlmDemany

// ---- The stuff below is for Clebsch Gordan coefficients --------------

int doubleint(double x){
  double dt = floor(2*x+0.5);
  if(fabs(2.*x-dt)>0.0001) {
    cout << "\aError in doubleint!  Input should be double rep of\n";
    cout << "an integer or half integer.  You gave me " << x << ".\n";
  }
  int it = 0;
  while( (it*1.-dt)*(it*1.-dt)>0.1)
    if (it*1.<dt) it++; else it--;
  return it;
} // doubleint

void printinthalfint(int t){if(t==2*(t/2)) cout<<t/2; else cout<<t<<"/2"; }

void testthejm(int j, int m, char *jname, char *mname) {
  if( (j+m) != 2*((j+m)/2)) {
    cout << "Warning! " << jname << "=";	
    printinthalfint(j);
    cout << " and " << mname << "=";
    printinthalfint(m);
    cout << "!\n";
  }
} // testthejm

double fac2(int x) {
  // We use doubles here to avoid the problem of overflow of ints
  if(x!=2*(x/2)) cout << "\aError in fac2!  Operand is not even!\n";
  double ret=1.; for(int i=x/2; i>0; i--) ret*=i;
  return ret;
} // fac2

double clebschgordanwork(int J1, int M1, int J2, int M2, int J, int M) {
  // This is from CACM
  // This gives <J/2,M/2;J1/2,J2/2|J1/2,M1/2;J2/2,M2/2>
  /* printf("\n Calculating <");
     printinthalfint(J);  printf(", "); printinthalfint(M);  printf("; ");
     printinthalfint(J1); printf(", "); printinthalfint(J2); printf(" | ");
     printinthalfint(J1); printf(", "); printinthalfint(M1); printf("; ");
     printinthalfint(J2); printf(", "); printinthalfint(M2); printf(">\n");
  */
  testthejm(J1,M1,"J1","M1");
  testthejm(J2,M2,"J2","M2");
  testthejm(J ,M ,"J ","M ");
  
  if( (M1 + M2 != M) ||
      (abs(M1) > abs(J1)) ||	( (M1+J1) != 2*((M1+J1)/2) ) ||
      (abs(M2) > abs(J2)) ||	( (M2+J2) != 2*((M2+J2)/2) ) ||
      (abs(M ) > abs(J )) ||	( (M +J ) != 2*((M +J )/2) ) ||
      (J >     J1 + J2 )  ||	(J < abs(J1 - J2)) || 
      ( (J1 + J2 + J) != 2*((J1+J2+J)/2) ) ||
      (M1+M2!=M) )
    return(0.);
  else {
    if(J1==1 || J2==1) {
      if(J==2)
	if(M1==-1)
	  if(M2==-1)	           return(1.);
	  else /* M2==+1 */        return(sqrt(0.5));
	else // M1==+1
	  if(M2==-1)	           return(sqrt(0.5));
	  else /* M2==+1 */        return(1.);
      else // J=0 -> M=0
	if(M1==-1) /* M2==+1 */    return(-sqrt(0.5));
	else /* M1==+1, M2==-1 */  return(+sqrt(0.5));
    } else {
      int zmin = 0;
      if( J-J2+M1+zmin < 0) zmin = -J+J2-M1;
      if( J-J1-M2+zmin < 0) zmin = -J+J1+M2;
      int zmax = J1+J2-J;
      if( J2+M2-zmax < 0) zmax = J2+M2;
      if( J1-M1-zmax < 0) zmax = J1-M1;
      double cc = 0.;
      for(int z=zmin; z<=zmax; z += 2)
	cc += (z==4*(z/4) ? 1. : -1.)/(fac2(z)*
				       fac2(J1 + J2 - J - z)*
				       fac2(J1 - M1 - z)*
				       fac2(J2 + M2 - z)*
				       fac2(J  - J2 + M1 + z)*
				       fac2(J  - J1 - M2 + z));
      return(cc*sqrt( ((J + 1)*
		       fac2( J1 + J2 - J)*
		       fac2( J1 - J2 + J)*
		       fac2(-J1 + J2 + J)*
		       fac2(J1 + M1)*   fac2(J1 - M1)*
		       fac2(J2 + M2)*   fac2(J2 - M2)*
		       fac2(J  + M )*   fac2(J  - M ))/
		      (1.*fac2(J1 + J2 + J + 2)) ) );
    }
  }
} // clebschgordanwork

double clebschgordan(double j1, double m1, double j2, double m2,
		     double j , double m) { 
  // This gives <j,m;j1,j2|j1,m1;j2,m2>
  return clebschgordanwork(doubleint(j1),doubleint(m1),
			   doubleint(j2),doubleint(m2),
			   doubleint(j ),doubleint(m ));
} // clebschgordan

double clebschgordanint(int j1, int m1, int j2, int m2, int j, int m) { 
  // This gives <j,m;j1,j2|j1,m1;j2,m2>
  return clebschgordanwork(2*j1,2*m1,2*j2,2*m2,2*j,2*m);
} // clebschgordanint
