// test.tmp.C   2000/6/30  Jason R. Cooke
// 
// To compile, use
// gcc test.tmp.C -lm -lstdc++ -I/home/jrcooke/c
// 
/////////////////////////////////////////////////////////////////////
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <iostream.h>
#include <iomanip.h>

#include "cos.int.H"

int main(){

  double a,b,c;
  
  //  cout << setprecision(60);
  cout << setiosflags(ios::scientific);

  for(int i=1; i<=10; i++) {
    a = 10.*(1.-2.*uniformdouble);
    b = a  *(1.-2.*uniformdouble);
    for(int n=1; n<=10; n++) {
      for(int m=0; m<=10; m++) {
	double aSqMbSq = a*a-b*b;
	c = ( a>0. ? 1 : -1 )*sqrt(aSqMbSq); // Sign(a)*Sqrt(a^2-b^2)
	double nf = facforderivs(n, a, b, c, m);
	double of = facforderivsold(n, a, b, c, m);
	
	//      cout << setw(35) << nf << "\n";
	// cout << setw(35) << of << "\n";
	cout << nf-of << " ";
      }
    }
  }

  exit(0);
}

