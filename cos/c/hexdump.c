#include <stdio.h>
#include <stdlib.h>
#define MAXPATH 64
#define MAXINTx2 16

char pablechar(int x)
{
  if((x<32) || ((126<x)&&(x<161)) ||
	(x==164) || (x==166) ||
	((171<x)&&(x<176)) || (x==180) ||
	(x==184) || (x==190) || (x==208) ||
	(x==222) || (x==240) ||
	(253<x) )
    return('.');
  return(x);
}

char num2hex(int n)
{
  static char *hextab = { "0123456789ABCDEF" };

  return(hextab[n % 16]);
}

char *byte2hex(int n, char *string)
{
  char *ch;

  ch = string;
  *ch = num2hex(n/16);
  ++ch;
  *ch = num2hex(n);
  return(string);
}

char *int2hex(int n, char *string)
{
  int i,temp;
  char *ch;

  temp = n;
  ch   = string + 2*sizeof(int);
  *ch  = '\0';
  ch   = ch-2;
  for (i=0; i<sizeof(int); i++, ch=ch-2) {
    byte2hex(temp,ch);
    temp = temp/256;
  }
  return(string);
}

void main(int argc, char**argv)
{
  int ch,i,j;
  FILE *fp1;
  char *pathname;
  char hexnum[]  = { "        " };
  char hexdata[]  = { "[ 0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F]" };
  char chardata[] = { "[0123456789ABCDEF]" };

  if(MAXINTx2 < 2*sizeof(int)) {
    printf("MAXINTx2 is not large enough!");
    exit(1);
  }

  if(argc>2) {
    printf("Too many parameters!\n");
    exit(1);
  }
  else if(argc==2)
    pathname = argv[1];
  else {
    pathname = calloc(MAXPATH, sizeof(char));
    printf("Input filename: ");
    gets(pathname);
  }
  printf("File: %s\n",pathname);
  if( *pathname == '\0' ) {
    printf("You did not give a file name!\n");
    exit(1);
  }
  fp1 = fopen(pathname, "r");
  if (fp1 == NULL) {
    printf("cannot open %s\n", pathname);
    exit(1);
  }

  printf(" %s  %s %s\n\n",hexnum,hexdata,chardata);
  for (i=0; ( (ch=fgetc(fp1)) != EOF) ; i++) {    
    byte2hex(ch,&hexdata[3*(i%16)+1]);
    chardata[i%16+1] = pablechar(ch);
    if ( (i%16)==15 )
      printf(" %s  %s %s\n",int2hex((i/16)*16,hexnum),hexdata,chardata);
  }
  if( ((i-1)%16)!=15 ) {
    for(j=(i%16);j<16;j++) {
      chardata[ j+1] = ' ';
      hexdata[3*j+1] = ' ';
      hexdata[3*j+2] = ' ';
    }
    printf(" %s  %s %s\n",int2hex((i/16)*16,hexnum),hexdata,chardata);
  }

  fclose(fp1);
  exit(0);

}
