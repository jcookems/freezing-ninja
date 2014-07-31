#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <iostream.h>
#include <iomanip.h>

#include "file.io.H"

int main(){
  double *vec = new double[100];
  for(int i=0;i<10;i++)
    vec[i] = cos((i*(i+1))*0.5);

  for(int i=0;i<10;i++)
    cout <<i<<":"<<vec[i]<<"\n";
  writeitout("/home/jrcooke/c","testio.bin",10,vec,1);
  writeitout("/home/jrcooke/c","testio.out",10,vec,0);

  cout << setprecision(10);

  delete[] vec;
  cout << "Now read in from testio.out\n";
  readitin("/home/jrcooke/c","testio.out",10,&vec,0);
  for(int i=0;i<10;i++)
    cout <<i<<":"<<vec[i]<<"\n";

  delete[] vec;
  cout << "Now read in from testio.bin\n";
  readitin("/home/jrcooke/c","testio.bin",10,&vec,1);
  for(int i=0;i<10;i++)
    cout <<i<<":"<<vec[i]<<"\n";
  
  exit(0);
}

