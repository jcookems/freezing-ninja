// jacobip.C   2000/7/21   Jason R. Cooke
//
// d^J_{M',M}(\theta) = jacobip(J,MP,M,theta)
//
/////////////////////////////////////////////////////////////////////

#include "jacobip.h"

// These are not available to the outside world
double factrl_jacobip(int n) {  // returns n!, from Numerical recipies
  static int ntop=4;
  static double a[33]={1.0,1.0,2.0,6.0,24.0};

  if (n < 0) {
    cout << "\aNegative factorial in routine FACTRL\n";
    return log((double)(n)); // This produces a trapable error
  }
  if (n > 32) return gamma(n+1.0); // returns n!
  while (ntop<n) { int j = ntop++; a[ntop]=a[j]*ntop; }
  return a[n];
} // factrl

// These are the outside face
double jacobip(int j, int mp, int m, double theta) {
  if(j<0) {
    cout << "\aError!  jacobip only works when j>=0.  You gave j="<<j<<"!\n";
    exit(1);
  }

  if(j*j<mp*mp || j*j<m*m ) return 0.;
  // this is the only sensable thing to do.  Don't generate an error.
  int kmin = m - ( m< mp ? m :  mp ); // Max(0   ,m-mp) -> m - Min(m, mp)
  int kmax = j + ( m<-mp ? m : -mp ); // Min(j+m ,j-mp) -> j + Min(m,-mp)

  double costhetao2   =     cos(theta*0.5);
  double sinthetao2   =     sin(theta*0.5);
  double costhetao2m2 = 1./(costhetao2*costhetao2);
  double sinthetao22  =     sinthetao2*sinthetao2;

  double ret = 0.;
  double curfac = ((kmin-m+mp)%2 ? -1. : 1.)*
    pow(costhetao2,2*j-2*kmin+m-mp) * pow(sinthetao2,2*kmin-m+mp);
  for(int k=kmin; k<=kmax; k++) {
    ret += curfac/(factrl_jacobip(j+m-k)  * factrl_jacobip(k)* 
		   factrl_jacobip(j-k-mp) * factrl_jacobip(k-m+mp));
    curfac *= -1.*costhetao2m2*sinthetao22;
  }

  ret *= sqrt( factrl_jacobip(j-m)  * factrl_jacobip(j+m) *
	       factrl_jacobip(j-mp) * factrl_jacobip(j+mp));

  return(ret);
} // jacobip

double testjacobip(int j, int mp, int m, double theta) {
  if(j<0) {
    cout << "\aError!  jacobip only works when j>=0.  You gave j="<<j<<"!\n";
    exit(1);
  }
  
  if(j*j<mp*mp || j*j<m*m) return 0.;
  // this is the only sensable thing to do.  Don't generate an error.
  int kmin = m - ( m< mp ? m :  mp ); // Max(0   ,m-mp) -> m - Min(m, mp)
  int kmax = j + ( m<-mp ? m : -mp ); // Min(j+m ,j-mp) -> j + Min(m,-mp)
  
  double costhetao2   =     cos(theta*0.5);
  double sinthetao2   =     sin(theta*0.5);
  double costhetao2m2 = 1./(costhetao2*costhetao2);
  double sinthetao22  =     sinthetao2*sinthetao2;
  
  double ret = 0.;
  double curfac = ((kmin-m+mp)%2 ? -1. : 1.)*
    pow(costhetao2,2*j-2*kmin+m-mp) * pow(sinthetao2,2*kmin-m+mp);
  for(int k=kmin; k<=kmax; k++) {
    ret += curfac/(factrl_jacobip(j+m-k)  * factrl_jacobip(k)*
		   factrl_jacobip(j-k-mp) * factrl_jacobip(k-m+mp));
    curfac *= -1.*costhetao2m2*sinthetao22;
  }
  
  ret *= sqrt( factrl_jacobip(j-m)  * factrl_jacobip(j+m) * 
	       factrl_jacobip(j-mp) * factrl_jacobip(j+mp));
  
  return(ret);
} // jacobip
