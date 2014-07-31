// file.io.H   2000/3/14   Jason Cooke
//////////////////////////////////////////
#if !defined(__STD_FILE_IO_H)  // This prevents loading twice!
#define __STD_FILE_IO_H
#include <unistd.h>
#include <stdlib.h>
#include <fstream.h>
#include <iomanip.h>
#include <stdio.h>
#include <math.h>
#if !defined(NOUSETIME)
#include <time.h>
#include <sys/time.h>
#endif

#ifdef USINGCOMPLEX
#if !defined(NOCPPCOMPLEX)
// Turn off if using f2c, since Fortran has diff complex useage
// #include <std/complex.h> // For the complex
#include "complexfix.h" // For the complex
#endif

#endif // USINGCOMPLEX

#if !defined(__STD_FILE_IO_C)
class fort {
public:
  fort(double xin) { x=xin; };
  static void setprec(int precin) { prec=precin; };
  static int  getprec() { return prec; };
  friend ostream& operator << (ostream& o, const fort& f);
private:
  static int prec;
  double x;
};
int fort::prec = 6; // Because this really is just a global var.
// fort::setprec(5); // Changes the precision of the fortran output.
ostream& operator << (ostream &o, const fort& f) {
  double x = f.x;
  o << (x<0?"-":" ")<<"0."; x *= (x<0.?-1.:1.);
  int expo = (x==0.? 0 : int(floor(log10(x)))+1 );
  int prec = f.prec;
  o << int(x*pow(10.,prec-expo)+0.5);
  o << "D" << (expo<0?"-":"+"); expo=abs(expo);
  if(expo<10) o << "0";
  o << expo;
  return o;
} // <<
#endif

int checkexist(char *pathname, char *filename);
void strongmkdir(char *pathname1, char *pathname2);
void runner(char *name, char **argv, char **envp);
void runner(char *name, char **argv);
void runner(char *name);
#ifdef USINGCOMPLEX
#if !defined(NOCPPCOMPLEX)
void writeitout(char *pathname,char *filename,int num,double_complex *vec,int dobin);
#endif
#endif USINGCOMPLEX

void writeitout(char *pathname,char *filename,int num,double         *vec,int dobin);
void writeitout(char *pathname,char *filename,int num,int            *vec,int dobin);
void writeitoutcolumns(char *pathname, char *filename, int num,
		       double *vec0, double *vec1,
		       int dobin);
void writeitoutmultcolumns(char *pathname, char *filename, 
			   int numcolumns, int num,
			   double **vecs,
			   int dobin);
void readitinwork(char *pathname, char *filename, int num, double *vec,
		  char *name, int dobin);
void readitin(char *pathname, char *filename, int num, double **vec,
	      char *name, int dobin);
#ifdef USINGCOMPLEX
#if !defined(NOCPPCOMPLEX)
void readitinwork(char *pathname, char *filename, int num, double_complex *vec,
		  char *name, int dobin);
void readitin(char *pathname, char *filename, int num, double_complex **vec,
	      char *name, int dobin);
#endif
#endif
void writesymmatoutwork(char *pathname, char *filename, int num, double *vec,
			int dobin, int upper);
void writesymmatout(char *pathname, char *filename, int num, double *vec,
		    int dobin);
void writesymmatoutupper(char *pathname, char *filename, int num, double *vec,
			 int dobin);
void writesymmatoutlower(char *pathname, char *filename, int num, double *vec,
			 int dobin);
void readsymmatinwork(char *pathname, char *filename, int num, double *vec,
		      char *name, int dobin, int upper);
void readsymmatin(char *pathname, char *filename, int num, double **vec,
		  char *name, int dobin);
void readsymmatinupper(char *pathname, char *filename, int num, double **vec,
		       char *name, int dobin);
void readsymmatinlower(char *pathname, char *filename, int num, double **vec,
		       char *name, int dobin);
int getpospacked(int inip, int ini, int num, int upper);
int getpospackedupper(int inip, int ini, int num);
int getpospackedlower(int inip, int ini, int num);
#if !defined(NOUSETIME)
void myprinttime(double mtime, char *s);
#endif

#endif
