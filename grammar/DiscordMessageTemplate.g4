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
LT: '<';
GT: '>';

LBRACE: '(';
RBRACE: ')';
LIDX: '[';
RIDX: ']';
LACC: '{';
RACC: '}';

TEXT: 'text';
ATTACHMENT: 'attachment';

CONST: 'const';
VAR: 'var';

IF: 'if';
FOR: 'for';
WHILE: 'while';
DO: 'do';
RETURN: 'return';
FUNCTION: 'function';

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
FIELDS: 'fields';
FIELD: 'field';

TRUE: 'true' | 'yes' | 'on' | 'enable''d'? | 'towards' | 'based' | 'indisputable' | 'y''e'?('a'('h'|'y')?)? | 'hooray' | 'lesgo';
FALSE: 'false' | 'no' | 'off' | 'disable''d'? | 'against' | 'biased' | 'reconsiderable' | 'n'[ao]+[yh]? | 'boo' | 'shut';

STRLIT: QUOTE (ESCAPE_QUOTE | (~[\r\n"]))* QUOTE;
NUM: [0-9]+;
ID: [a-zA-Z0-9_$]+;
HEXNUM: ('0x' | '#') [0-9a-fA-F]+;

WHITESPACE: [ \t\r\n] -> channel(HIDDEN);

unaryOpNumericalNegate: MINUS;
unaryOpLogicalNegate: EXCLAMATION;
unaryOpBitwiseNegate: TILDE;
unaryOp
    : unaryOpNumericalNegate
    | unaryOpLogicalNegate
    | unaryOpBitwiseNegate
;
binaryOpPlus: PLUS;
binaryOpMinus: MINUS;
binaryOpMultiply: MULTIPLY;
binaryOpDivide: DIVIDE;
binaryOpModulus: MODULUS;
binaryOpPow: ROOF;
binaryOpBitwiseAnd: AMPERSAND;
binaryOpLogicalAnd: AMPERSAND AMPERSAND;
binaryOpBitwiseOr: BAR;
binaryOpLogicalOr: BAR BAR;
binaryOpLessThan: LT;
binaryOpGreaterThan: GT;
binaryOp
    : binaryOpPlus
    | binaryOpMinus
    | binaryOpMultiply
    | binaryOpDivide
    | binaryOpModulus
    | binaryOpPow
    | binaryOpBitwiseAnd
    | binaryOpLogicalAnd
    | binaryOpBitwiseOr
    | binaryOpLogicalOr
    | binaryOpLessThan
    | binaryOpGreaterThan
;

expression
    : left=expression binaryOp right=expression                             #exprBinaryOp
    | unaryOp expression                                                    #exprUnaryOp
    | funcname=ID LBRACE (expression (COMMA expression)*)? RBRACE           #exprCallFunc
    | ID                                                                    #exprVar
    | STRLIT                                                                #exprString
    | NUM (DOT NUM)?                                                        #exprNumber
    | HEXNUM                                                                #exprHexColor
    | (TRUE | FALSE)                                                        #exprBool
;

embedAuthorComponentField
    : NAME ASSIGN expression SEMICOLON    #embedAuthorComponentName
    | ICON ASSIGN expression SEMICOLON    #embedAuthorComponentIcon
    | URL ASSIGN expression SEMICOLON     #embedAuthorComponentUrl
;
embedAuthorComponent
    : name=expression (COMMA url=expression (COMMA icon=expression)?)? SEMICOLON    #embedAuthorFlow
    | AUTHOR LACC embedAuthorComponentField+ RACC                                   #embedAuthorObj
;
embedFooterComponentField
    : TEXT ASSIGN expression SEMICOLON    #embedFooterComponentText
    | ICON ASSIGN expression SEMICOLON    #embedFooterComponentIcon
;
embedFooterComponent
    : text=expression (COMMA icon=expression)? SEMICOLON        #embedFooterFlow
    | FOOTER LACC embedFooterComponentField+ RACC               #embedFooterObj
;
embedFieldComponentField
    : TITLE ASSIGN expression SEMICOLON     #embedFieldComponentTitle
    | TEXT ASSIGN expression SEMICOLON      #embedFieldComponentText
    | INLINE (TRUE | FALSE)? SEMICOLON      #embedFieldComponentInline
;
embedFieldComponentPart
    : title=expression COMMA text=expression (COMMA INLINE)? SEMICOLON      #embedFieldFlow
    | FIELD LACC embedFieldComponentField+ RACC                             #embedFieldObj
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
    | FIELDS mutate embedFieldsComponent            #embedFields
;

messageComponent
    : TEXT ASSIGN expression SEMICOLON                  #componentText
    | ATTACHMENT mutate expression SEMICOLON            #componentAttachment
    | EMBED mutate LACC embedComponent+ RACC            #componentEmbed
    | EMBED DOT embedComponent                          #componentEmbedToplevelMember
    | EMBED DOT AUTHOR DOT embedAuthorComponentField    #componentEmbedAuthorField
    | EMBED DOT FOOTER DOT embedFooterComponentField    #componentEmbedFooterField
;

union: statement | expression;

statement
    : messageComponent                                                                                      #stmtComponent
    | VAR? varname=ID ASSIGN expression                                                                     #stmtAssign
    | IF LBRACE expression RBRACE statementBlock                                                            #stmtIf
    | FOR LBRACE init=union? SEMICOLON check=expression? SEMICOLON accumulate=union? RBRACE statementBlock  #stmtForI
    | FOR LBRACE VAR? varname=ID COLON iterable=expression RBRACE statementBlock                            #stmtForEach
    | WHILE LBRACE check=expression RBRACE statementBlock                                                   #stmtWhile
    | DO statementBlock WHILE LBRACE check=expression RBRACE                                                #stmtDoWhile
    | RETURN expression SEMICOLON                                                                           #stmtReturn
    | FUNCTION name=ID LBRACE (ID (COMMA ID)+)? RBRACE statementBlock                                       #stmtDeclFunc
;
statementBlock
    : SEMICOLON             #stmtBlockEmpty
    | LACC statement* RACC  #stmtBlock
    | statement SEMICOLON   #stmtSingular
;

template
    : CONST name=ID ASSIGN expression SEMICOLON     #templateConst
    | statement+                                    #templateStatement
    | STRLIT                                        #templateText
;

COMMENT: '//' ~[\r\n]* -> channel(HIDDEN);
UNMATCHED: .;
