// jacobip.test.C    2000/7/21    Jason R. Cooke
// gcc -I/home/jrcooke/c -L. jacobip.test.C -lutils -lm -lstdc++ -Wall
////////////////////////////////////////////////////

#include <iostream.h>
#include <iomanip.h>
#include <jacobip.H>

int main()
{
  cout << setprecision(10) << setiosflags(ios::fixed);

  double theta = 0.2;

  cout << "{\n";
  for(int j=1; j<=1; j++)
    for(int mp=-j; mp<=j; mp++)
      for(int m=-j; m<=j; m++) {
	cout << "Chop[Wignerd["<<j<<","<<mp<<","<<m<<","<<theta<<"]-(";
	cout << jacobip(j,mp,m,theta)<<")],\n";
      }
  cout << "0}\n";
  exit(0);
}
