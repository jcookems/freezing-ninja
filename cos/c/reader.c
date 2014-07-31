#include <stdio.h>
#include <stdlib.h>
#define MAXPATH 64

void main()
{
  int ch,i;
  FILE *fp1,*fp2;
  char pathname[MAXPATH];

  printf("Input filename: ");
  gets(pathname);
  if( *pathname == '\0' )
    exit(0);
  fp1 = fopen(pathname, "r");
  if (fp1 == NULL) {
    printf("cannot open %s\n", pathname);
    exit(1);
  }
  printf("Input file opened OK.\n");

  printf("Output filename: ");
  gets(pathname);
  if( *pathname == '\0' )
    exit(0);
  fp2 = fopen(pathname, "w");
  if (fp2 == NULL) {
    printf("cannot open %s\n", pathname);
    exit(1);
  }
  printf("Output file opened OK.\n");

  i = 0;
  while ((ch = fgetc(fp1)) != EOF) {
    printf("  %d  is  %d (%c) \n",++i,ch,ch);
    if ( (ch == 10) || (ch >= 32) )
      fputc(ch,fp2);
  }

  fclose(fp1);
  fclose(fp2);
  exit(0);

}
