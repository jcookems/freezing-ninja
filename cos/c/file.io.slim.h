// file.io.slim.H   2000/9/23   Jason Cooke
// file.io.H   2000/3/14   Jason Cooke
//////////////////////////////////////////
#if !defined(__STD_FILE_IO_H)  // This prevents loading twice!
#define __STD_FILE_IO_H

#include <iomanip.h> // for setw and iostream.h
#include <fstream.h> // for ofstream
#include <stdio.h>   // for sprintf
//#include <unistd.h>
//#include <stdlib.h>
#include <time.h>
#include <sys/time.h>


// ---------------------------------------------------------------------
// The binary switch should be used in general.  The reason is twofold:
// 1. The output file is 33% smaller
// 2. The output file is an exact copy of the data.
//----------------------------------------------------
void writeitout(char *pathname, char *filename, int num, double *vec,
		int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ofstream tfile(newfilename);
  if(tfile.fail()) {
    cerr << "\aError!  Couldn't open " << newfilename << "for writing.\n";
    cerr << "Exiting...\n"; exit(1);
  }
  //  cout << "Writing to file " << newfilename << "...\n";
  delete[] newfilename;
  if(dobin)
    tfile.write((char *)vec, num*sizeof(double));
  else {
    tfile << setiosflags(ios::scientific);
    for(int i=0;i<num;i++) tfile<<setw(13)<<vec[i]<<"\n";
    tfile << resetiosflags(ios::scientific);
  }
  if(tfile.fail()) {cerr << "\aError in tfile.write to the file!\n"; exit(1);}
  tfile.close();
  if(tfile.fail()) {cerr << "\aError in tfile.close to the file!\n"; exit(1);}
} // writeitout

// ------------------------------------------------------------

void readsymmatinwork(char *pathname, char *filename, int num, double *vec,
		      char *name, int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ifstream ifile(newfilename);
  if(ifile.fail()) {
    cerr << "Couldn't open symmetric matrix "<<newfilename<<" for reading.\n";
    cerr << "Compiled without def'ing USINGRUNNER,\n";
    cerr << "so the program "<< name << " cannot be called.\n";
    cerr << "\aNo program name provided that can make the file.\n";
    cerr << "Exiting...\n";
    exit(1);
  } else {
    //    cout << "Opened OK.  But have to close and restart.\n";
    ifile.close();
    if(ifile.fail()){cerr << "\aError in tfile.close to the file!\n"; exit(1);}
  }

  // Is there a more elegant way to do this?
  
  ifstream ifile2(newfilename);
  if(ifile2.fail()) {
    cerr << "\aError!  Still Couldn't open "<<newfilename<<" for reading.\n";
    cerr << "Exiting...\n"; exit(1);
  }
  delete[] newfilename;

  if(dobin)
    for(int i=0;i<num;i++) {
      ifile2.read((char*)&(vec[i*num]),(i+1)*sizeof(double));
      if(ifile2.fail()) {
	cerr << "\aError in tfile.read to the file!\n";
	cerr << "num="<<num<<"\n" << " i ="<< i <<"\n"; exit(1);
      }
    }
  else
    for(int i=0;i<num;i++) for(int j=0;j<=i;j++) {
      ifile2 >> vec[i*num+j];
      if(ifile2.fail()) {
	cerr << "\aError in tfile.read to the file!\n";
	cerr << "num="<<num<<"\n i ="<< i <<"\n j ="<< j <<"\n"; exit(1);
      }
    }

  // Now fill in the rest of the matrix
  for(int i=0;i<num;i++) for(int j=0;j<=i;j++) 
    vec[j*num+i] = vec[i*num+j];

  ifile2.close();
  if(ifile2.fail()) {cerr << "\aError in tfile.close to the file!\n"; exit(1);}
} // readsymmatinwork

void readsymmatin(char *pathname, char *filename, int num, double **vec,
		  char *name, int dobin){
  *vec = new double[num*num];
  readsymmatinwork(pathname, filename, num, *vec, name, dobin);
} // readsymmatin

// --------------------------------------------------------------------

int getpospacked(int inip, int ini, int num, int upper) {
  // This calculates the physical location of the coords (inip,ini) in 
  // a lower packed matrix of size num*num
  int i = inip<ini ? ini  : inip;
  int j = inip<ini ? inip : ini ;
  if(upper) return         j + (i+1)*i/2;
  else      return num*j + i - (j+1)*j/2;
} // getpospacked
int getpospackedupper(int inip, int ini, int num) {
  return getpospacked(inip,ini,num,1);
} // getpospackedupper
int getpospackedlower(int inip, int ini, int num) {
  return getpospacked(inip,ini,num,0);
} // getpospackedlower

// --------------------------------------------------------------------

void myprinttime(double mtime, char *s){
  if(mtime<1000.) {
    sprintf(s,"%.3f mili-seconds",mtime);
  } else {
    mtime/=1000.;
    if(mtime<300.) {
      sprintf(s,"%.3f seconds",mtime);
    } else {
      mtime/=60.;
      if(mtime<180) {
	sprintf(s,"%.3f minutes.",mtime);
      } else {
	mtime/=60.;
	if(mtime<48) {
	  sprintf(s,"%.3f hours.",mtime);
	} else {
	  mtime/=24.;
	  sprintf(s,"%.3f days.",mtime);
	}
      } 
    }
  }
} // myprinttime

#endif
