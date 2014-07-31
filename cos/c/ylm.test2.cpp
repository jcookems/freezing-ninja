// ylm.test2.C    2001/2/25  Jason R. Cooke
// gcc -L. ylm.test2.C -lutils -lm -lstdc++ 
///////////////////////////////////////////////////////////

#include <iostream.h>
#include <iomanip.h>
#include "ylm.H"

void main()
{
  int nump = 10;
  double *ps = new double[nump];
  double x=-0.94;
  for(int m=0; m<10; m++) {
    cout << "m="<<m<<"\n";
    dplgndrmany(m,x,ps,nump);
    for(int l=m; l<m+nump; l++) {
      double oldp = dplgndr(l,m,x);
      cout << "l="     << setw( 2) << l            << ", ";
      cout << "oldleg="<< setw(13) << oldp         << ", ";
      cout << "new="   << setw(13) << ps[l-m]      << ", ";
      cout << "diff="  << setw(13) << ps[l-m]-oldp << "\n";
    }
  }

  double *ylms = new double[nump];
  double theta = 0.7674;
  for(int m=0; m<10; m++) {
    cout << "m="<<m<<"\n";
    getYlmDemany(m, theta, ylms, nump);
    for(int l=m; l<m+nump; l++) {
      double oldylm = getYlmDe(l,m,theta);
      cout << "l="     << setw( 2) << l                << ", ";
      cout << "oldylm="<< setw(13) << oldylm           << ", ";
      cout << "new="   << setw(13) << ylms[l-m]        << ", ";
      cout << "diff="  << setw(13) << ylms[l-m]-oldylm << "\n";
    }
  }
}
