// jacobip.H   2000/7/21   Jason R. Cooke
//
// d^J_{M',M}(\theta) = jacobip(J,MP,M,theta)
//
/////////////////////////////////////////////////////////////////////

#if !defined(__STD_JACOBIP_H)  // This prevents loading twice!
#define __STD_JACOBI_H

#include <iostream.h>
#include <math.h>

double jacobip(int j, int mp, int m, double theta);
double testjacobip(int j, int mp, int m, double theta);
  
#endif
