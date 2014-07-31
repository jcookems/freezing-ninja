#include <stdio.h>
#include <stdlib.h>
#include <signal.h>

#define u_char unsigned char
#define u_int unsigned int
#define DATASTRLEN 16
#define SCREENHEIGHT 20
#define DATAWIDTH 16

typedef struct {
	u_char numentries,data[DATASTRLEN];
	void *previous,*next;
} memchunck;


memchunck *readindata(FILE *fp, u_int *maxdata)
{
  int ch;
  memchunck *start,*current,*temp;

  start = current = malloc(sizeof(memchunck));
  (*current).previous = NULL;
  (*current).numentries = 0;
  *maxdata = 0;

  while ( (ch=fgetc(fp)) != EOF) {
    ++(*maxdata);
    (*current).data[((*current).numentries)++] = ch;
    if ((*current).numentries == DATASTRLEN) {
      temp = malloc(sizeof(memchunck));
      (*current).next = temp;
      (*temp).previous = current;
      current = temp;
      (*current).numentries = 0;
    }
  }
  fclose(fp);

  if(!((*current).numentries)) {
    if( current == start ) {
      free(current);
      return(NULL);
    }
    current = (*current).previous;
    free((*current).next);
  }
  (*current).next = NULL;

  return(start);
}

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

int findhexdata(memchunck *start, int currentaddress, int inputchar)
{
  static int previousaddress;
  static memchunck *current;
  static u_char curpos;
  int numadvance;

/*  printf("%d %d\n",previousaddress, currentaddress); */
  if (currentaddress < 0) {
    previousaddress = 0;
    current = start;
    curpos = 0;
    return(0);
  }
  numadvance = currentaddress-previousaddress;
/*  printf("%d\n",numadvance); */
  previousaddress = currentaddress;
  if (numadvance < 0) {
    numadvance = -numadvance;
    while (numadvance<curpos) {
      numadvance -= curpos+1;
      if((*current).previous == NULL) {
        printf("Oh no!  Error in advancedata!  Tried to backtrack past the\n");
        printf("beginning of the list!  Quitting...\n");
        printf("numadvance = %d\n",numadvance);
        exit(1);
      }
      current = (*current).previous;
      curpos  = (*current).numentries-1;
    }
    curpos -= numadvance;
  }
  else if(numadvance>0) {
    while (numadvance>=((*current).numentries-curpos)) {
      numadvance -= (*current).numentries-curpos;
      if((*current).next == NULL) {
        curpos = (*current).numentries-1;
        previousaddress -= numadvance; 
/*        printf("Tried to pass the end of the list!\n");
        printf("numadvance = %d\n",numadvance);
        printf("currentaddress = %d\n",currentaddress); */
        return((int)(-1));
      }
      current = (*current).next;
      curpos  = 0;
    }
    curpos += numadvance;
  }

  if(inputchar<0)
    return((*current).data[curpos]);
  else {
    (*current).data[curpos] = inputchar;
    return(0);
  }
} /* findhexdata */

void displaydata(memchunck *start, u_int startposition, u_int highlight)
{
  u_int i,j,currentaddress,lastdata,lastchar;
  int ch;
  char **dataoutput,**charoutput;

  findhexdata(start,(int)(-1),0);
  currentaddress = startposition;

  dataoutput = calloc(SCREENHEIGHT,sizeof(char*));
  charoutput = calloc(SCREENHEIGHT,sizeof(char*));
  for(i=0; i<SCREENHEIGHT ; i++) {
    dataoutput[i] = calloc(DATAWIDTH*5,sizeof(char));
    sprintf(dataoutput[i]," %08X [",currentaddress);
    lastdata = 11;
    charoutput[i] = calloc(DATAWIDTH*5,sizeof(char));
    charoutput[i][0] = '[';
    lastchar = 1;
    for(j=0; j<DATAWIDTH ; j++, currentaddress++) {
      if(j)
        dataoutput[i][lastdata++] = ' ';
      ch = findhexdata(start,currentaddress,(int)(-1));
      if(ch<0) {
        sprintf(&dataoutput[i][lastdata],"  ");
        lastdata += 2;
        sprintf(&charoutput[i][lastchar]," ");
        lastchar += 1;
      }
      else if(currentaddress==highlight) {
        sprintf(&dataoutput[i][lastdata],"%c[7m%02X%c[m",27,ch,27);
        lastdata += 9;
        sprintf(&charoutput[i][lastchar],"%c[7m%c%c[m",27,pablechar(ch),27);
        lastchar += 8;
      }
      else {
        sprintf(&dataoutput[i][lastdata],"%02X",ch);
        lastdata += 2;
        sprintf(&charoutput[i][lastchar],"%c",pablechar(ch));
        lastchar += 1;
      }
    }
    sprintf(&dataoutput[i][lastdata],"]  ");
    lastdata += 3;
    sprintf(&charoutput[i][lastchar],"]");
    lastchar += 1;
  }

  printf("%c[2H",27);
  for(i=0; i<SCREENHEIGHT ; i++)
    printf("%s%s\n",dataoutput[i],charoutput[i]);
}

void changechar(memchunck *liststart, u_int highlight)
{
  int i;

  findhexdata(liststart,(int)(-1),0);
  printf("%c[%dH%c[J(%02X) What number to change to, in hex? ",27,
	SCREENHEIGHT+2,27,findhexdata(liststart,highlight,(int)(-1)));
  scanf("%02X",&i);
  fflush(stdin);
  findhexdata(liststart,highlight,i);
}

int getcommand()
{
  char input[40];

  printf("%c[%dH%c[J",27,SCREENHEIGHT+2,27);
  while (1) {
    printf("%c[%dHNow what? J=back, K=forward, I=up, J=down, Q=quit: ",
	27,SCREENHEIGHT+2);
    gets(input);
    printf("\n");
    switch (input[0]) {
      case 'q' :
      case 'Q' :	
        return(0);
      case 'j' :
      case 'J' :
        return(1);
      case 'k' :
      case 'K' :
        return(2);
      case 'i' :
      case 'I' :
        return(3);
      case 'm' :
      case 'M' :
        return(4);
      case 'u' :
      case 'U' :
        return(5);
      case 'n' :
      case 'N' :
        return(6);
      case 'c' :
      case 'C' :
        return(10);
      default :
        printf("\n Nope, not a good choice!%c",7);
    }
  }
}

void main(int argc, char**argv)
{
  int code,trytomove;
  u_int highlight,maxdata,startposition;
  FILE *fp;
  memchunck *liststart;

  if(argc!=2) {
    printf("HexEdit v0.0\t\tBy Jason Cooke\t\t8/26/96\n");
    printf("Usage: hexedit filename\n");
    exit(1);
  }
  fp = fopen(argv[1], "r");
  if (fp == NULL) {
    printf("cannot open %s\n",argv[1]);
    exit(1);
  }

  printf("%c[H%c[JFile: %s\n",27,27,argv[1]);
  liststart = readindata(fp,&maxdata);
  highlight = 0;
  startposition = 0;
  code = 1;

  while(code) {
    displaydata(liststart,startposition,highlight);
    code = getcommand();
    switch (code) {
      case 0 : 
        break;
      case 1 :
        trytomove = highlight - 1;
        break;
      case 2 : 
        trytomove = highlight + 1;
        break;
      case 3 :
        trytomove = highlight - DATASTRLEN;
        break;
      case 4 :
        trytomove = highlight + DATASTRLEN;
        break;
      case 5 :
        trytomove = highlight - DATASTRLEN*(SCREENHEIGHT-3);
        break;
      case 6 :
        trytomove = highlight + DATASTRLEN*(SCREENHEIGHT-3);
        break;
      case 10 :
        changechar(liststart,highlight);
        break;
      default : 
        printf("Error!  Unknown code %d in main!\n",code);
    }
  if(trytomove<0)
    highlight = 0;
  else if(trytomove>(maxdata-1))
    highlight = maxdata-1;
  else
    highlight = trytomove;
  if(highlight>=(startposition+SCREENHEIGHT*DATAWIDTH))
    startposition = (highlight/DATAWIDTH-SCREENHEIGHT+1)*DATAWIDTH;
  if(highlight<startposition)
    startposition = (highlight/DATAWIDTH)*DATAWIDTH;
  }


  exit(0);
}
