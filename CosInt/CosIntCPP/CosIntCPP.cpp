// test.cos.C   2000/6/30  Jason R. Cooke
//
/////////////////////////////////////////////////////////////////////

#include "stdafx.h"

int _tmain(int argc, _TCHAR* argv[])
{
	double a11 = 2.;     double aM  = 1.9;    double a21 = 5.;
	double a12 = 2.;	 double bM1 = 0.1;    double a22 = 3.2;
	double b1  = 0.2;	 double bM2 = 0.79;   double b2  = -3.199;

	/* cout << "i[n11_,n12_,m1_,n21_,n22_,m2_] := 1/(2*Pi)^2*NIntegrate[\n";
	cout << "Cos[m1*phi1]*Cos[m2*phi2]/(\n";
	cout << "("<<a11<<"+"<<b1 <<"*Cos[phi1])^n11\n";
	cout << "("<<a12<<"+"<<b1 <<"*Cos[phi1])^n12\n";
	cout << "("<<aM <<"+"<<bM1<<"*Cos[phi1]+"<<bM2<<"*Cos[phi2])\n";
	cout << "("<<a21<<"+"<<b2 <<"*Cos[phi2])^n21\n";
	cout << "("<<a22<<"+"<<b2 <<"*Cos[phi2])^n22\n";
	cout << "),{phi1,0,2*Pi},{phi2,0,2*Pi}];\n"; */

	cout << setprecision(10) << setiosflags(ios::fixed);

	for (int x = 0 ; x<10; x++) {
		for(int n11 = 0; n11<2; n11++)
			for(int n21 = 0; n21<2; n21++)
				for(int n12 = 0; n12<3; n12+=2)
					for(int n22 = 0; n22<3; n22+=2)
						for(int m1 = 0; m1<3; m1++)
							for(int m2 = 0; m2<3; m2++)
							{
								double c1 = cosnn1nnint(a11,a12,b1,n11,n12,m1,aM,bM1,bM2,
									a21,a22,b2,n21,n22,m2);
								double c2 = cosnn1nnint(a21,a22,b2,n21,n22,m2,aM,bM2,bM1,
									a11,a12,b1,n11,n12,m1);
								/* cout<<"i["<<n11<<","<<n12<<","<<m1
								<< ","<<n21<<","<<n22<<","<<m2<<"] = \t"
								<<setw(13)<<c1<<"\t"
								<<setw(13)<<c2<<"\t";
								cout << setprecision(10) << resetiosflags(ios::fixed);
								cout << setw(17)<<(c1-c2)/(c1+c2)<<"\n"; */
							}
	}
}
