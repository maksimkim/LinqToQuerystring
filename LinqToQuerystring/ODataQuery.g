grammar ODataQuery;

options
{
    language=CSharp3;
    output=AST;

}

tokens {
    ALIAS;
}

@lexer::namespace { LinqToQuerystring }
@parser::namespace { LinqToQuerystring }


@lexer::header {
using LinqToQuerystring.Exceptions;
}

@lexer::members {
    const int HIDDEN = Hidden;
    
    public override void ReportError(RecognitionException e) 
    {
        if (this.input.LT(1) == '\\')
        {
            //This will be an invalid escape sequence
            throw new InvalidEscapeSequenceException("\\" + (char)e.Character);
        }

        throw e;
    }
}

public parse
    :    (param (AMP! param)*)*;

param    :    (orderby | top | skip | filter | select | inlinecount | expand);

skip    
    :    SKIP^ T_INT+;

top    
    :    TOP^ T_INT+;

filter    
    :    FILTER^ filterexpression[false];
    
select
    :    SELECT^ propertyname[false] (COMMA! propertyname[false])*;
            
expand
    :    EXPAND^ propertyname[false] (COMMA! propertyname[false])*;
    
inlinecount
    :    INLINECOUNT^ ALLPAGES
    |    INLINECOUNT NONE ->;

filterexpression[bool subquery]
    :    orexpression[subquery] ( BOP_OR^ orexpression[subquery])*;
    
orexpression[bool subquery]
    :    andexpression[subquery] ( BOP_AND^ andexpression[subquery])*;
    
andexpression[bool subquery]
    :    (BOP_NOT^ (OP! filterexpression[subquery] CP! | booleanexpression[subquery]))
    |    (OP! filterexpression[subquery] CP! | booleanexpression[subquery]);
        
booleanexpression[bool subquery]
    :    atom1=atom[subquery] (
             op=comparison  atom2=atom[subquery]     
            -> ^($op $atom1 $atom2)
        |    -> ^(BOP_EQUALS["eq"] $atom1 T_BOOL["true"])
        );

comparison 
    :    (BOP_EQUALS | BOP_NOTEQUALS | BOP_GREATERTHAN | BOP_GREATERTHANOREQUAL | BOP_LESSTHAN | BOP_LESSTHANOREQUAL)
    ;
        
atom[bool subquery]
    :    functioncall[subquery]
    |    constant
    |    accessor[subquery];
    
functioncall[bool subquery]:
        (function^ OP! atom[subquery] (COMMA! atom[subquery])* CP!)
        | (fun=M_SUBSTRINGOF OP arg=atom[subquery] COMMA str=atom[subquery] CP)
            -> ^($fun $str $arg)
        ;

accessor[bool subquery]:
        (propertyname[subquery] -> propertyname) 
        (
            SLASH (func=M_ANY | func=M_ALL | func=M_COUNT | func=M_MAX | func=M_MIN | func=M_SUM | func=M_AVERAGE) 
            OP (
                    (lambaexpression) -> ^($func $accessor lambaexpression)
                    | -> ^($func $accessor) 
                )
            CP 
        )?;

lambaexpression
    :    (id=IDENTIFIER LAMBDA  filterexpression[true]) -> ^(LAMBDA filterexpression ALIAS[$id])
    ;

function
    :    M_STARTSWITH | M_ENDSWITH | M_INDEXOF | M_TOLOWER | M_TOUPPER | M_LENGTH | M_TRIM;
        
orderby
    :    ORDERBY^ orderbylist;
    
orderbylist
    :    orderpropertyname (COMMA! orderpropertyname)*;

orderpropertyname
    :    propertyname[false] (
            -> ^(ASC["asc"] propertyname)
            | ( (op=ASC | op=DESC)) -> ^($op propertyname)
        );
    
constant:    (T_INT^ | T_BOOL^ | T_STRING^ | T_DATETIME^ | T_LONG^ | T_SINGLE^ | T_DOUBLE^ | T_GUID^ | T_BYTE^ | T_NULL^);

propertyname[bool subquery]
    :    (identifierpart[subquery] -> identifierpart) (SLASH next=subpropertyname[false] -> ^($next $propertyname))?;

subpropertyname[bool subquery]
    :    propertyname[false];
    
identifierpart[bool subquery]
    :    (id=IDENTIFIER -> {subquery}? ALIAS[$id]
                -> IDENTIFIER[$id]
        | DYNAMICIDENTIFIER -> DYNAMICIDENTIFIER);

filteroperator
    :    BOP_EQUALS | BOP_NOTEQUALS | BOP_GREATERTHAN | BOP_GREATERTHANOREQUAL | BOP_LESSTHAN | BOP_LESSTHANOREQUAL;

/* Types */
T_BOOL    
    :    ('true' | 'false');

T_BYTE    
    :    '0x' HEX_PAIR;

T_DATETIME
    :    'datetime\'' '0'..'9'+ '-' '0'..'9'+ '-' + '0'..'9'+ 'T' '0'..'9'+ ':' '0'..'9'+ (':' '0'..'9'+ ('.' '0'..'9'+)*)* '\'';

T_DOUBLE    
    :    ('-')? ('0'..'9')+ '.' ('0'..'9')+;

T_GUID    
    :    'guid\'' HEX_PAIR HEX_PAIR HEX_PAIR HEX_PAIR '-' HEX_PAIR HEX_PAIR '-' HEX_PAIR HEX_PAIR '-' HEX_PAIR HEX_PAIR '-' HEX_PAIR HEX_PAIR HEX_PAIR HEX_PAIR HEX_PAIR HEX_PAIR '\'';

T_INT    
    :    ('-')? '0'..'9'+;

T_LONG    
    :    ('-')? ('0'..'9')+ 'L';

T_NULL    
    :    'null';

T_SINGLE    
    :    ('-')? ('0'..'9')+ '.' ('0'..'9')+ 'f';

T_STRING     
    :     '\'' (ESC_SEQ| ~('\\'|'\''))* '\'';

/* Booleand Operations */
BOP_AND    
    :     'and';

BOP_EQUALS    
    :    'eq';    
    
BOP_GREATERTHAN    
    :    'gt';    
    
BOP_GREATERTHANOREQUAL
    :    'ge';    
    
BOP_LESSTHAN    
    :    'lt';    
    
BOP_LESSTHANOREQUAL
    :    'le';    

BOP_NOT        
    :    'not';

BOP_NOTEQUALS    
    :    'ne';    

BOP_OR    
    :    'or';

/* Methods */
M_ALL    
    :    'all';

M_ANY    
    :     'any';
    
M_AVERAGE    
    :    'average';
    
M_COUNT    
    :    'count';

M_ENDSWITH
    :    'endswith';

M_INDEXOF
    :    'indexof';
    
M_LENGTH
    :    'length';

M_MIN    
    :    'min';

M_MAX    
    :    'max';

M_STARTSWITH 
    :    'startswith';

M_SUBSTRINGOF
    :    'substringof';

M_SUM    
    :    'sum';
    
M_TOLOWER
    :    'tolower';

M_TOUPPER
    :    'toupper';

M_TRIM
    :    'trim';

    
ASSIGN
    :     '=';

ASC    
    :    'asc';
    
DESC    
    :    'desc';    
    
ALLPAGES
    :     'allpages';
    
NONE
    :    'none';

SKIP
    :    '$skip=';

TOP
    :    '$top=';

FILTER
    :    '$filter=';

ORDERBY
    :    '$orderby=';
    
SELECT
    :    '$select=';
    
INLINECOUNT
    :    '$inlinecount=';
    
EXPAND    
    :    '$expand=';
    
LAMBDA : ':';

AMP : '&';

OP : '(';

CP : ')';

COMMA : ',';

SLASH : '/';

WS  :   ( ' '
        | '\t'
        | '\r'
        | '\n'
        ) {$channel=HIDDEN;}
    ;

DYNAMICIDENTIFIER
    :    '[' ('a'..'z'|'A'..'Z'|'0'..'9'|'_')+ ']';    
    
fragment
HEX_PAIR
    : HEX_DIGIT HEX_DIGIT;
    
IDENTIFIER
    :    ('a'..'z'|'A'..'Z'|'0'..'9'|'_')+;

fragment
HEX_DIGIT : ('0'..'9'|'a'..'f'|'A'..'F') ;

fragment
ESC_SEQ
    : '\'\''
        | '\\' ('b'|'t'|'n'|'f'|'r'|'\"'|'\''|'\\')
        | UNICODE_ESC
        | OCTAL_ESC
        ;

fragment
OCTAL_ESC
        :   '\\' ('0'..'3') ('0'..'7') ('0'..'7')
        |   '\\' ('0'..'7') ('0'..'7')
        |   '\\' ('0'..'7')
        ;

fragment
UNICODE_ESC
        :   '\\' 'u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT
        ;
