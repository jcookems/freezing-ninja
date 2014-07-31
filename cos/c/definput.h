/* definputpp.h  2000/2/19   Jason Cooke */
#if !defined(__STD_DEFINPUT_H_CPP)  // This prevents loading twice!
#define __STD_DEFINPUT_H_CPP

#include <ncurses.h>

/* in_onoff   : Listen to the user's input?
   in_str     : Question to ask
   in_type    : input format for the answer
   in_typeout : output format for the answer
   in_name    : variable name for answer.  Must be set to default value first
   Examples:
   DEFINPUT(1,"Mass of the mesons?     ","%lf","%f",*massscalar);
   DEFINPUT(1,"Use SB graph?","%d","%d",(*whichgraphs)[1]);
*/
#define DEFINPUT(in_onoff,in_str,in_type,in_typeout,in_name,buffsize) \
    printf(in_str);                                   \
    printf(" (");                                     \
    printf(in_typeout,in_name);                       \
    printf("): ");                                    \
    DEFINPUT_BUFF = new char[buffsize];               \
    if(in_onoff) {                                    \
        my_scanf(DEFINPUT_BUFF,buffsize);             \
        if(DEFINPUT_BUFF[0] == 0) {                   \
            printf(in_typeout,in_name);               \
        } else {                                      \
            sscanf(DEFINPUT_BUFF,in_type,&(in_name)); \
        };                                            \
    } else {                                          \
        printf(in_typeout,in_name);                   \
    };                                                \
    printf("\n");                                     \
    if(DEFINPUT_BUFF != NULL) delete[] DEFINPUT_BUFF; \
    if(in_onoff) {                                    \
        endwin();                                     \
    }

void my_scanf(char *buff, int len) { 
    /* This is my own version of scanf.  Kind of just for fun, but
       partly practical.  For some terminals, the backspace and delete
       keys are mapped funny, but this is coded so both just delete back
       one character.
    */
    int tempin,loc,del; 

    fflush(stdout); 
    initscr(); 
    cbreak(); 
    noecho(); 
    loc = 0; 
    tempin = 0;
    while(tempin != 10) { 
       	tempin = getch(); 
	del = 0;  
	if( (tempin >= 32) && (tempin < 127) ) 
	    if(loc<(len-1)) { 
		buff[loc] = tempin; 
		printf("%c",tempin); 
		loc ++; 
		/*	    printf("Added char!\n"); */ 
	    }; 
	if( (tempin == 127 /* xt's bksp */ ) || (tempin == 8 /* xt's del */) ) 
	    del = 1;
	if( tempin == 27 ){
	    tempin = getch();
	    if(tempin == 91){
		tempin = getch();
		if(tempin == 51){
		    tempin = getch();
		    if(tempin == 126) del = 1; /* rxvt's delete */
		} else if(tempin == 68) {
		    del = 1; /* left arrow */
		} else {
		    getch();
		};
	    };
	};
	if(del) {
	    if(loc !=0) {
		printf("\b \b");
		loc--;
		/*		printf("Deleted a char.\n"); */
	    } else {
		/*		printf("No chars to delete!\n"); */
	    };
	};
	buff[loc] = 0;
	fflush(stdout);
    };
/*    printf("\n"); */
    /*    endwin(); */
} /* my_scanf */

void definput(int onoff, char *str, double *varname) {
    char *DEFINPUT_BUFF;

    DEFINPUT(onoff,str,"%lf","%f",*varname,80);
}

void definput(int onoff, char *str, int *varname) {
    char *DEFINPUT_BUFF;

    DEFINPUT(onoff,str,"%d","%d",*varname,80);
}

#endif
