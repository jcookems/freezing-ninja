// file.io.H   2000/3/14   Jason Cooke
//
// 2001/2/18 Fixed up filehandle leak in checkexist,
//
//////////////////////////////////////////

// #define NOUSETIME
#define USINGFREEBSD
#define USINGRUNNER

#define USINGCOMPLEX
#define __STD_FILE_IO_C

#include "file.io.H"
#include <iostream.h>

#include <fcntl.h>
#include <dirent.h>
#include <string.h>
#include <unistd.h> // for sleep

#ifdef USINGFREEBSD
#include <sys/dirent.h>
#include <sys/stat.h>
#endif

// #define MAXPATHLEN 100  // This doesn't appear to be used anymore.
#define myMAXDIRENT 500
#define MAXDIRBYTES 100000

#ifdef USINGFREEBSD
int checkexist(char *pathname, char *filename, int debug_flag) {
  int fd = open(pathname,O_RDONLY); // open path for read only
  char *buf = new char[MAXDIRBYTES];  // prep buffer for directory data
  long basep;   // basep is a pointer for bookkeeping, not used here
  getdirentries(fd,buf,MAXDIRBYTES,&basep); // reads dir into buf
  close(fd); // close up the file opened now that we did the reading

  int check = 0;
  char *curbufp = buf;
  if(debug_flag) cout << "\n  i   number rec_len type nam_len name\n";
  int i;
  for(i=0;i<myMAXDIRENT;i++) {
    struct dirent *pookielist = (struct dirent *)curbufp;
    if( ((*pookielist).d_fileno == 0) && // This seems to be safest
	((*pookielist).d_reclen == 0) &&
	((*pookielist).d_type   == 0) &&
	((*pookielist).d_namlen == 0)) break;
    if(debug_flag) printf("%3d %8d %5d   %3d  %4d    %s\n",
			  i,
			  (*pookielist).d_fileno,
			  (*pookielist).d_reclen,
			  (*pookielist).d_type,
			  (*pookielist).d_namlen,
			  (*pookielist).d_name);
    check = !strcmp(filename,(*pookielist).d_name);
    if(check) break;
    curbufp += (*pookielist).d_reclen;
  }

  if(i==myMAXDIRENT) {
    cout << "There appear to be more than "<<myMAXDIRENT<<" dir entries.\n";
    cout << "If there are less than that, there is something wrong\n";
    cout << "\acheckexist in fileio.h.\n";
    // sleep(10);
    exit(1);
  }

  delete[] buf; // Cleans up memory allocated
  return(check);
} // checkexist

void strongmkdir(char *pathname1, char *pathname2) {
  int debug_flag = 0;
  int isthere = checkexist(pathname1,pathname2,debug_flag);
  if(!isthere) {
    cout << "\nThe path does not exist.  Let's make it.\n";
    char *temp = new char[160];
    sprintf(temp,"%s/%s",pathname1,pathname2);
    cout << "Path we try to create: "<<temp<<"\n";
    mkdir(temp,S_IRWXU);
    delete[] temp;
    cout << "Made the dir.  Let's see if it went OK, ";
    isthere = checkexist(pathname1,pathname2,debug_flag);
  } else {
    if(debug_flag) cout << "\nThe path exists, "<<pathname1<<"/"<<pathname2
			<<"\n";
  }
  if(!isthere) {
    cout << "\n\aCould not create the path.  Error!\n";
    // sleep(10);
    exit(1);
  } else {
    if(debug_flag) cout << "The path is there now.\n";
  }
} // strongmkdir
#endif

#ifdef USINGRUNNER

#include <unistd.h>
//  #include <sys/types.h>  // I don't think this is used anymore
#include <sys/wait.h>

void runner(char *name, char **argv, char **envp) {
  int fval=fork();
  // cout << "Result of fork = " << fval << "\n";
  
  if(fval==0) {
    // cout << "In the child (fval="<<fval<<"), we run "<<name<<"\n";
    int i=execve(name,argv,envp);
    // we should  never return from here
    cout << "  \aWhoops! In runner, i="<<i<<"!\n";
    cout << "  In the child (fval="<<fval<<"), we ran "<<name<<"\n";
    cout << "  Exiting...\n";
    // sleep(10);
    exit(10);
  }
  //  cout << "In the parent (fval="<<fval<<"), we wait for completion.\n";
  int j;
  int i = wait(&j);
  // cout <<"fval:"<<fval<<" wait: status = "<<j<<", return value = "<<i<<"\n";
  if(i==-1) {
    cout << "\aError!  In runner, wait returned -1!  Exiting...\n";
    // sleep(10);
    exit(10);
  }
} // runner

void runner(char *name, char **argv) {
  char **envp = new char*;  envp[0] = NULL;
  runner(name,argv,envp);
  delete envp;
} // runner

void runner(char *name) {
  char **argv = new char*;  argv[0] = NULL;
  char **envp = new char*;  envp[0] = NULL;
  runner(name,argv,envp);
  delete argv;
  delete envp;
} // runner
#endif // USINGRUNNER

// ---------------------------------------------------------------------

// The binary switch should be used in general.  The reason is twofold:
// 1. The output file is 33% smaller
// 2. The output file is an exact copy of the data.
//----------------------------------------------------

#ifdef USINGCOMPLEX
void writeitout(char *pathname, char *filename, int num, double_complex *vec,
		int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ofstream tfile(newfilename);
  if(tfile.fail()) {
    cout << "\aError!  Couldn't open " << newfilename << "for writing.\n";
    cout << "Exiting...\n";
    // sleep(10);
    exit(1);
  }
  //  cout << "Writing to file " << newfilename << "...\n";
  delete[] newfilename;
  if(dobin)
    tfile.write((char *)vec, num*sizeof(double_complex));
  else {
    tfile << setiosflags(ios::scientific);
    for(int i=0;i<num;i++) tfile<<setw(13)<<vec[i]<<"\n";
    tfile << resetiosflags(ios::scientific);
  }
  if(tfile.fail()) {
    cout << "\aError in tfile.write to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
  tfile.close();
  if(tfile.fail()) {
    cout << "\aError in tfile.close to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
} // writeitout, double_complex
#endif // USINGCOMPLEX

void writeitout(char *pathname, char *filename, int num, double *vec,
		int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ofstream tfile(newfilename);
  if(tfile.fail()) {
    cout << "\aError!  Couldn't open " << newfilename << "for writing.\n";
    cout << "Exiting...\n";
    // sleep(10);
    exit(1);
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
  if(tfile.fail()) {
    cout << "\aError in tfile.write to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
  tfile.close();
  if(tfile.fail()) {
    cout << "\aError in tfile.close to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
} // writeitout, double

void writeitout(char *pathname, char *filename, int num, int *vec,
		int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ofstream tfile(newfilename);
  if(tfile.fail()) {
    cout << "\aError!  Couldn't open " << newfilename << "for writing.\n";
    cout << "Exiting...\n";
    // sleep(10);
    exit(1);
  }
  //  cout << "Writing to file " << newfilename << "...\n";
  delete[] newfilename;
  if(dobin)
    tfile.write((char *)vec, num*sizeof(int));
  else {
    tfile << setiosflags(ios::scientific);
    for(int i=0;i<num;i++) tfile<<vec[i]<<"\n";
    tfile << resetiosflags(ios::scientific);
  }
  if(tfile.fail()) {
    cout << "\aError in tfile.write to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
  tfile.close();
  if(tfile.fail()) {
    cout << "\aError in tfile.close to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
} // writeitout

void writeitoutcolumns(char *pathname, char *filename, int num,
		       double *vec0, double *vec1,
		       int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ofstream tfile(newfilename);
  if(tfile.fail()) {
    cout << "\aError!  Couldn't open " << newfilename << " for writing.\n";
    cout << "Exiting...\n";
    // sleep(10);
    exit(1);
  }
  //  cout << "Writing to file " << newfilename << "...\n";
  delete[] newfilename;
  if(dobin) {
    cout << "\aError in writeitoutcolumns!  No support for dobin!\n";
    cout << "filename: " << newfilename << "\n";
    exit(1);
  }

  tfile << setiosflags(ios::scientific);
  for(int i=0;i<num;i++) {
    tfile<<setw(13)<<vec0[i]<<"\t";
    tfile<<setw(13)<<vec1[i]<<"\n";
  }
  tfile << resetiosflags(ios::scientific);

  if(tfile.fail()) {
    cout << "\aError in tfile.write to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
  tfile.close();
  if(tfile.fail()) {
    cout << "\aError in tfile.close to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
} // writeitoutcolumns

void writeitoutmultcolumns(char *pathname, char *filename, 
			   int numcolumns, int num,
			   double **vecs,
			   int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ofstream tfile(newfilename);
  if(tfile.fail()) {
    cout << "\aError!  Couldn't open " << newfilename << "for writing.\n";
    cout << "Exiting...\n";
    // sleep(10);
    exit(1);
  }
  //  cout << "Writing to file " << newfilename << "...\n";
  delete[] newfilename;
  if(dobin) {
    cout << "\aError in writeitoutcolumns!  No support for dobin!\n";
    cout << "filename: " << newfilename << "\n";
    exit(1);
  }

  tfile << setiosflags(ios::scientific);
  for(int i=0;i<num;i++) {
    for(int j=0;j<numcolumns;j++) {
      if(j!=0) tfile<<"\t";
      tfile<<setw(13)<<vecs[j][i];
    }
    tfile<<"\n";
  }
  tfile << resetiosflags(ios::scientific);
  
  if(tfile.fail()) {
    cout << "\aError in tfile.write to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
  tfile.close();
  if(tfile.fail()) {
    cout << "\aError in tfile.close to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
} // writeitoutmultcolumns

void readitinwork(char *pathname, char *filename, int num, double *vec,
		  char *name, int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  //  cout << newfilename << "\n";
  ifstream ifile(newfilename);
  if(ifile.fail()) {
    cout << "\aError!  Couldn't open " << newfilename << " for reading.\n";
#ifdef USINGRUNNER   
    if(name[0]==0) {
#else
      cout << "Compiled without def'ing USINGRUNNER,\n";
      cout << "so the program "<< name << " cannot be called.\n";
#endif
      cout << "No program name provided that can make the file ";
      cout << newfilename << "!\n";
      cout << "Exiting...\n";
      // sleep(10);
      exit(1);
#ifdef USINGRUNNER   
    }
    cout << "Running " << name << " to try to make it.\n";
    runner(name);
    ifstream ifile(newfilename);
    if(ifile.fail()) {
      cout << "\aError!  Still Couldn't open "<<newfilename<<" for reading.\n";
      cout << "Exiting...\n";
      // sleep(10);
      exit(1);
    }
#endif
  }

  if(dobin)
    ifile.read((char*)vec,num*sizeof(double));
  else
    for(int i=0;i<num;i++) ifile >> vec[i];
  if(ifile.fail()) {
    cout << "\aError in tfile.read from the file "<<newfilename<<"!\n";
    cout << "Params to this function were:\n";
    cout << "pathname= " << pathname << "\n";
    cout << "filename= " << filename << "\n";
    cout << "num     = " << num      << "\n";
    cout << "*vec    = " << *vec     << "\n";
    cout << "name    = " << name     << "\n";
    cout << "dobin   = " << dobin    << "\n";

    // sleep(10);
    exit(1);
  }
  ifile.close();
  if(ifile.fail()) {
    cout << "\aError in tfile.close from the file "<<newfilename<<"!\n";
    // sleep(10);
    exit(1);
  }

  delete[] newfilename;
} // readitinwork

void readitin(char *pathname, char *filename, int num, double **vec,
	      char *name, int dobin){
  *vec = new double[num];
  readitinwork(pathname, filename, num, *vec, name, dobin);
} // readitin

#ifdef USINGCOMPLEX
void readitinwork(char *pathname, char *filename, int num, double_complex *vec,
		  char *name, int dobin){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  //  cout << newfilename << "\n";
  ifstream ifile(newfilename);
  if(ifile.fail()) {
    cout << "\aError!  Couldn't open " << newfilename << " for reading.\n";
#ifdef USINGRUNNER   
    if(name[0]==0) {
#else
      cout << "Compiled without def'ing USINGRUNNER,\n";
      cout << "so the program "<< name << " cannot be called.\n";
#endif
      cout << "No program name provided that can make the file ";
      cout << newfilename << "!\n";
      cout << "Exiting...\n";
      // sleep(10);
      exit(1);
#ifdef USINGRUNNER   
    }
    cout << "Running " << name << " to try to make it.\n";
    runner(name);
    ifstream ifile(newfilename);
    if(ifile.fail()) {
      cout << "\aError!  Still Couldn't open "<<newfilename<<" for reading.\n";
      cout << "Exiting...\n";
      // sleep(10);
      exit(1);
    }
#endif
  }

  if(dobin)
    ifile.read((char*)vec,num*sizeof(double_complex));
  else
    for(int i=0;i<num;i++) ifile >> vec[i];
  if(ifile.fail()) {
    cout << "\aError in tfile.read from the file "<<newfilename<<"!\n";
    cout << "Params to this function were:\n";
    cout << "pathname= " << pathname << "\n";
    cout << "filename= " << filename << "\n";
    cout << "num     = " << num      << "\n";
    cout << "*vec    = " << *vec     << "\n";
    cout << "name    = " << name     << "\n";
    cout << "dobin   = " << dobin    << "\n";

    // sleep(10);
    exit(1);
  }
  ifile.close();
  if(ifile.fail()) {
    cout << "\aError in tfile.close from the file "<<newfilename<<"!\n";
    // sleep(10);
    exit(1);
  }

  delete[] newfilename;
} // readitinwork

void readitin(char *pathname, char *filename, int num, double_complex **vec,
	      char *name, int dobin){
  *vec = new double_complex[num];
  readitinwork(pathname, filename, num, *vec, name, dobin);
} // readitin
#endif // USINGCOMPLEX

// ------------------------------------------------------------

void writesymmatoutwork(char *pathname, char *filename, int num, double *vec,
			int dobin, int upper){
  // It is not efficient to test the symmetry of the matirx here.
  // That is the calling program's responsibility.
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ofstream tfile(newfilename);
  if(tfile.fail()) {
    cout << "\aError!  Couldn't open " << newfilename << " for writing.\n";
    cout << "Exiting...\n";
    // sleep(10);
    exit(1);
  }
  //  cout << "Writing symmetric matrix to file " << newfilename << "...\n";
  delete[] newfilename;
  
  if(dobin)
    for(int i=0;i<num;i++) 
      if(upper)
	tfile.write((char *)&(vec[i*num  ]),(i+1)*sizeof(double));
      else
	tfile.write((char *)&(vec[i*num+i]),(num-i)*sizeof(double));
  else {
    tfile << setiosflags(ios::scientific);
    for(int i=0;i<num;i++) for(int j=(upper?0:i);(upper?j<=i:j<num);j++)
      tfile<<setw(13)<<vec[i*num+j]<<"\n";
    tfile << resetiosflags(ios::scientific);
  }
  if(tfile.fail()) {
    cout << "\aError in tfile.write to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
  
  tfile.close();
  if(tfile.fail()) {
    cout << "\aError in tfile.close to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
} // writesymmatoutwork
void writesymmatout(char *pathname, char *filename, int num, double *vec,
		    int dobin){
  writesymmatoutwork(pathname, filename, num, vec, dobin, 1);
} // writesymmatout
void writesymmatoutupper(char *pathname, char *filename, int num, double *vec,
			 int dobin){
  writesymmatoutwork(pathname, filename, num, vec, dobin, 1);
} // writesymmatoutupper
void writesymmatoutlower(char *pathname, char *filename, int num, double *vec,
			 int dobin){
  writesymmatoutwork(pathname, filename, num, vec, dobin, 0);
} // writesymmatoutlower

void readsymmatinwork(char *pathname, char *filename, int num, double *vec,
		      char *name, int dobin, int upper){
  char *newfilename = new char[160];
  sprintf(newfilename,"%s/%s",pathname,filename);
  ifstream ifile(newfilename);
  if(ifile.fail()) {
    cout << "Couldn't open symmetric matrix "<<newfilename<<" for reading.\n";
#ifdef USINGRUNNER   
    if(name[0]==0) {
#else
      cout << "Compiled without def'ing USINGRUNNER,\n";
      cout << "so the program "<< name << " cannot be called.\n";
#endif
      cout << "\aNo program name provided that can make the file.\n";
      cout << "Exiting...\n";
      // sleep(10);
      exit(1);
#ifdef USINGRUNNER   
    }
    cout << "Running " << name << " to try to make it.\n";
    runner(name);
#endif
  } else {
    //    cout << "Opened OK.  But have to close and restart.\n";
    ifile.close();
    if(ifile.fail()) {
      cout << "\aError in tfile.close to the file!\n";
      cout << "filename: " << newfilename << "\n";
      // sleep(10);
      exit(1);
    }
  }

  // Is there a more elegant way to do this?
  
  ifstream ifile2(newfilename);
  if(ifile2.fail()) {
    cout << "\aError!  Still Couldn't open "<<newfilename<<" for reading.\n";
    cout << "Exiting...\n";
    // sleep(10);
    exit(1);
  }
  delete[] newfilename;

  if(dobin)
    for(int i=0;i<num;i++) {
      if(upper)
	ifile2.read((char*)&(vec[i*num  ]),(i+1)*sizeof(double));
      else
	ifile2.read((char*)&(vec[i*num+i]),(num-i)*sizeof(double));
      if(ifile2.fail()) {
	cout << "\aError in tfile.read to the file!\n";
	cout << "num="<<num<<"\n" << " i ="<< i <<"\n";
	cout << "filename: " << newfilename << "\n";
	// sleep(10);
	exit(1);
      }
    }
  else
    for(int i=0;i<num;i++) for(int j=(upper?0:i);(upper?j<=i:j<num);j++) {
      ifile2 >> vec[i*num+j];
      if(ifile2.fail()) {
	cout << "\aError in tfile.read to the file!\n";
	cout << "num="<<num<<"\n";
	cout << " i ="<< i <<"\n";
	cout << " j ="<< j <<"\n";
	cout << "filename: " << newfilename << "\n";
	// sleep(10);
	exit(1);
      }
    }

  // Now fill in the rest of the matrix
  for(int i=0;i<num;i++) for(int j=(upper?0:i);(upper?j<=i:j<num);j++)
    vec[j*num+i] = vec[i*num+j];

  ifile2.close();
  if(ifile2.fail()) {
    cout << "\aError in tfile.close to the file!\n";
    cout << "filename: " << newfilename << "\n";
    // sleep(10);
    exit(1);
  }
} // readsymmatinwork
void readsymmatin(char *pathname, char *filename, int num, double **vec,
		  char *name, int dobin){
  *vec = new double[num*num];
  readsymmatinwork(pathname, filename, num, *vec, name, dobin, 1);
} // readsymmatin
void readsymmatinupper(char *pathname, char *filename, int num, double **vec,
		       char *name, int dobin){
  *vec = new double[num*num];
  readsymmatinwork(pathname, filename, num, *vec, name, dobin, 1);
} // readsymmatinupper
void readsymmatinlower(char *pathname, char *filename, int num, double **vec,
		       char *name, int dobin){
  *vec = new double[num*num];
  readsymmatinwork(pathname, filename, num, *vec, name, dobin, 0);
} // readsymmatinlower

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

void myprinttime(double mtime, char *s){
  if(mtime<1000.){ sprintf(s,"%.3f milliseconds",mtime); return;} mtime/=1000.;
  if(mtime<300.) { sprintf(s,"%.3f seconds"     ,mtime); return;} mtime/=60.;
  if(mtime<180)  { sprintf(s,"%.3f minutes."    ,mtime); return;} mtime/=60.;
  if(mtime<48)   { sprintf(s,"%.3f hours."      ,mtime); return;} mtime/=24.;
  {                sprintf(s,"%.3f days."       ,mtime); return;}
} // myprinttime

#undef myMAXDIRENT
#undef MAXDIRBYTES
