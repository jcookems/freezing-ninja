/* ylm.test.C    6/22/2000  Jason R. Cooke
 * gcc ylm.test.C -lm -lstdc++
 */

#include <iostream.h>
#include <iomanip.h>
#include "ylm.H"

void main()
{
  double p,pm1;

  cout << setprecision(10) << setiosflags(ios::fixed);

  int max = 5;

  cout << "MatrixForm[a1={";
  for(int l=-max; l<=max; l++) {
    if(l!=-max) cout <<",";
    cout << "{";
    for(int m=-max; m<=max; m++) {
      if(m!=-max) cout <<",";
      cout << asslegpoly(l,m,0.1);
    }
    cout << "}\n";
  }
  cout << "}]\n";

  cout << "MatrixForm[a2={";
  for(int l=-max; l<=max; l++) {
    if(l!=-max) cout <<",";
    cout << "{";
    for(int m=-max; m<=max; m++) {
      if(m!=-max) cout <<",";
      cout << asslegpoly(l,m,1.1);
    }
    cout << "}\n";
  }
  cout << "}]\n";

  cout << "MatrixForm[a3={";
  for(int l=-max; l<=max; l++) {
    if(l!=-max) cout <<",";
    cout << "{";
    for(int m=-max; m<=max; m++) {
      if(m!=-max) cout <<",";
      cout << asslegpoly(l,m,-1.1);
    }
    cout << "}\n";
  }
  cout << "}]\n";

  cout << "MatrixForm[b1=Table[N[LegendreP[l,m,1/10]],";
  cout << "{l,"<<-max<<","<<max<<"},";
  cout << "{m,"<<-max<<","<<max<<"}]]\n";

  cout << "MatrixForm[b2=Table[N[LegendreP[l,m,3,11/10]],";
  cout << "{l,"<<-max<<","<<max<<"},";
  cout << "{m,"<<-max<<","<<max<<"}]]\n";
  
  cout << "MatrixForm[b3=Table[N[LegendreP[l,m,3,-11/10]],";
  cout << "{l,"<<-max<<","<<max<<"},";
  cout << "{m,"<<-max<<","<<max<<"}]]\n";

  exit(1);

  for(int l=0; l<=5; l++)
    for(int m=-l; m<=l; m++){
      cout << "Chop[SphericalHarmonicY["<<l<<","<<m<<",0.9,0]";
      cout << " - (" << getYlmDe(l, m, 0.9) << ")]\n";
    }


  for(int l=0; l<=8; l++)
    cout << "Chop[LegendreP["<<l<<",0.5]-("<< dlegsimp(l,0.5)<<")]\n";

  for(int l=0; l<=5; l++)
    for(int m=-l; m<=l; m++){
      cout << "Chop[SphericalHarmonicY["<<l<<","<<m<<",0.9,0]";
      cout << " - (" << getYlmDe(l, m, 0.9) << ")]\n";
    }

}
