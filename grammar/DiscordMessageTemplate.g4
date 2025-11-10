grammar DiscordMessageTemplate;

DOT: '.';
ASSIGN: '=';
APPEND: '+=';
mutate: ASSIGN | APPEND;
COLON: ':';
SEMICOLON: ';';
QUOTE: '"';
ESCAPE_QUOTE: '\\"';
HASH: '#';
COMMA: ',';
AMPERSAND: '&';
BAR: '|';
EXCLAMATION: '!';
TILDE: '~';

PLUS: '+';
MINUS: '-';
MULTIPLY: '*';
DIVIDE: '/';
MODULUS: '%';
ROOF: '^';

LBRACE: '(';
RBRACE: ')';
LIDX: '[';
RIDX: ']';
LACC: '{';
RACC: '}';

NOW: 'now';

TEXT: 'text';

VAR: 'var';

IF: 'if';
FOR: 'for';
WHILE: 'while';
DO: 'do';
FUNCTION: 'function';

TRUE: 'true' | 'yes';
FALSE: 'false' | 'no';

URL: 'url';
NAME: 'name';
ICON: 'icon';
INLINE: 'inline';

EMBED: 'embed';
TITLE: 'title';
DESCRIPTION: 'description';
AUTHOR: 'author';
TIMESTAMP: 'timestamp';
COLOR: 'color';
FOOTER: 'footer';
IMAGE: 'image';
THUMBNAIL: 'thumbnail';
FIELDS: 'fields';
FIELD: 'field';

STRLIT: QUOTE (ESCAPE_QUOTE | (~[\r\n"]))* QUOTE;
NUM: [0-9]+;
ID: [a-zA-Z0-9_$]+?;
HEXNUM: ('0x' | '#') [0-9a-fA-F]+;

WHITESPACE: [ \t\r\n] -> channel(HIDDEN);

op
    : PLUS                  #opPlus
    | MINUS                 #opMinus
    | MULTIPLY              #opMultiply
    | DIVIDE                #opDivide
    | MODULUS               #opModulus
    | ROOF                  #opPow
    | AMPERSAND             #opBitwiseAnd
    | AMPERSAND AMPERSAND   #opLogicalAnd
    | BAR                   #opBitwiseOr
    | BAR BAR               #opLogicalOr
;

expression
    : STRLIT                                                                #exprString
    | (TRUE | FALSE)                                                        #exprBool
    | NUM (DOT NUM)?                                                        #exprNumber
    | ID                                                                    #exprVar
    | NOW LBRACE RBRACE                                                     #exprNow
    | HEXNUM                                                                #exprHexColor
    | MINUS expression                                                      #exprNumericNegate
    | EXCLAMATION expression                                                #exprLogicalNegate
    | TILDE expression                                                      #exprBitwiseNegate
    | left=expression op right=expression                                   #exprOperator
;

embedAuthorComponentField
    : NAME ASSIGN expression SEMICOLON    #embedAuthorComponentName
    | ICON ASSIGN expression SEMICOLON    #embedAuthorComponentIcon
    | URL ASSIGN expression SEMICOLON     #embedAuthorComponentUrl
;
embedAuthorComponent
    : name=expression (COMMA icon=expression (COMMA url=expression)?)? SEMICOLON  #embedAuthorFlow
    | AUTHOR LACC embedAuthorComponentField+ RACC               #embedAuthorObj
;
embedFooterComponentField
    : TEXT ASSIGN expression SEMICOLON    #embedFooterComponentText
    | ICON ASSIGN expression SEMICOLON    #embedFooterComponentIcon
;
embedFooterComponent
    : text=expression (COMMA icon=expression)? SEMICOLON        #embedFooterFlow
    | FOOTER LACC embedFooterComponentField+ RACC   #embedFooterObj
;
embedFieldComponentField
    : TITLE ASSIGN expression SEMICOLON     #embedFieldComponentTitle
    | TEXT ASSIGN expression SEMICOLON      #embedFieldComponentText
    | INLINE (TRUE | FALSE)? SEMICOLON      #embedFieldComponentInline
;
embedFieldComponentPart
    : title=expression (COMMA text=expression (COMMA INLINE)?)? SEMICOLON   #embedFieldFlow
    | FIELD LACC embedFieldComponentField+ RACC                 #embedFieldObj
;
embedFieldsComponent
    : embedFieldComponentPart               #embedFieldSingular
    | LIDX embedFieldComponentPart+ RIDX    #embedFieldList
;
embedComponent
    : TITLE ASSIGN expression SEMICOLON             #embedTitle
    | URL ASSIGN expression SEMICOLON               #embedUrl
    | DESCRIPTION ASSIGN expression SEMICOLON       #embedDescription
    | AUTHOR mutate embedAuthorComponent            #embedAuthor
    | TIMESTAMP ASSIGN expression SEMICOLON         #embedTimestamp
    | COLOR ASSIGN expression SEMICOLON             #embedColor
    | FOOTER mutate embedFooterComponent            #embedFooter
    | IMAGE ASSIGN expression SEMICOLON             #embedImage
    | THUMBNAIL ASSIGN expression SEMICOLON         #embedThumbnail
    | FIELDS mutate embedFieldsComponent            #embedFields
;

messageComponent
    : TEXT ASSIGN expression SEMICOLON                #componentText
    | EMBED mutate LACC embedComponent+ RACC    #componentEmbed
;

template
    : statement+    #templateStatement
    | STRLIT        #templateText
;

union: statement | expression;

statement
    : messageComponent #stmtComponent
    | EMBED DOT embedComponent #stmtEmbedComponent
    | VAR? varname=ID ASSIGN expression #stmtAssign
    | IF LBRACE expression RBRACE statementBlock #stmtIf
    | FOR LBRACE init=union? SEMICOLON check=expression? SEMICOLON accumulate=union RBRACE statementBlock #stmtForI
    | FOR LBRACE varname=ID COLON iterable=expression RBRACE statementBlock #stmtForEach
    | WHILE LBRACE check=expression RBRACE statementBlock #stmtWhile
    | DO statementBlock WHILE LBRACE check=expression RBRACE #stmtDoWhile
;
statementBlock
    : SEMICOLON #stmtBlockEmpty
    | LACC statement* RACC #stmtBlock
    | statement #stmtSingular
;

COMMENT: '//' ~[\r\n]* -> channel(HIDDEN);
UNMATCHED: .;
