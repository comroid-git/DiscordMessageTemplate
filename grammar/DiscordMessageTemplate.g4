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

CONST: 'const';
VAR: 'var';

IF: 'if';
ELSE: 'else';
FOR: 'for';
WHILE: 'while';
DO: 'do';
RETURN: 'return';
FUNCTION: 'function';

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
binaryOpEquals: ASSIGN ASSIGN;
binaryOpNotEquals: EXCLAMATION ASSIGN;
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
    | binaryOpEquals
    | binaryOpNotEquals
    | binaryOpBitwiseAnd
    | binaryOpLogicalAnd
    | binaryOpBitwiseOr
    | binaryOpLogicalOr
    | binaryOpLessThan
    | binaryOpGreaterThan
;

keyValuePair: key=STRLIT COLON value=expression;
expression
    : source=expression LIDX index=NUM RIDX                                 #exprGetIndex
    | source=expression LIDX member=STRLIT RIDX                             #exprGetMember
    | LIDX (expression COMMA?)+ LIDX                                        #exprInitArray
    | LACC (keyValuePair COMMA?)+ RACC                                      #exprInitObject
    | left=expression binaryOp right=expression                             #exprBinaryOp
    | unaryOp expression                                                    #exprUnaryOp
    | funcname=ID LBRACE (expression (COMMA expression)*)? RBRACE           #exprCallFunc
    | ID                                                                    #exprVar
    | STRLIT                                                                #exprString
    | NUM (DOT NUM)?                                                        #exprNumber
    | HEXNUM                                                                #exprHexColor
    | (TRUE | FALSE)                                                        #exprBool
    | 'null'                                                                #exprNull
;

embedAuthorComponentField
    : 'name' ASSIGN expression SEMICOLON    #embedAuthorComponentName
    | 'icon' ASSIGN expression SEMICOLON    #embedAuthorComponentIcon
    | 'url' ASSIGN expression SEMICOLON     #embedAuthorComponentUrl
;
embedAuthorComponent
    : name=expression (COMMA url=expression (COMMA icon=expression)?)? SEMICOLON    #embedAuthorFlow
    | 'author' LACC embedAuthorComponentField+ RACC                                 #embedAuthorObj
;
embedFooterComponentField
    : 'text' ASSIGN expression SEMICOLON    #embedFooterComponentText
    | 'icon' ASSIGN expression SEMICOLON    #embedFooterComponentIcon
;
embedFooterComponent
    : text=expression (COMMA icon=expression)? SEMICOLON            #embedFooterFlow
    | 'footer' LACC embedFooterComponentField+ RACC                 #embedFooterObj
;
embedFieldComponentField
    : 'title' ASSIGN expression SEMICOLON     #embedFieldComponentTitle
    | 'text' ASSIGN expression SEMICOLON      #embedFieldComponentText
    | 'inline' (TRUE | FALSE)? SEMICOLON      #embedFieldComponentInline
;
embedFieldComponentPart
    : title=expression COMMA text=expression (COMMA 'inline')? SEMICOLON      #embedFieldFlow
    | 'field' LACC embedFieldComponentField+ RACC                             #embedFieldObj
;
embedFieldsComponent
    : embedFieldComponentPart               #embedFieldSingular
    | LIDX embedFieldComponentPart+ RIDX    #embedFieldList
;
embedComponent
    : 'title' ASSIGN expression SEMICOLON             #embedTitle
    | 'url' ASSIGN expression SEMICOLON               #embedUrl
    | 'description' ASSIGN expression SEMICOLON       #embedDescription
    | 'author' mutate embedAuthorComponent            #embedAuthor
    | 'timestamp' ASSIGN expression SEMICOLON         #embedTimestamp
    | 'color' ASSIGN expression SEMICOLON             #embedColor
    | 'footer' mutate embedFooterComponent            #embedFooter
    | 'image' ASSIGN expression SEMICOLON             #embedImage
    | 'fields' mutate embedFieldsComponent            #embedFields
;

messageComponent
    : 'text' ASSIGN expression SEMICOLON                    #componentText
    | 'attachment' mutate expression SEMICOLON              #componentAttachment
    | 'embed' mutate LACC embedComponent+ RACC              #componentEmbed
    | 'embed' DOT embedComponent                            #componentEmbedToplevelMember
    | 'embed' DOT 'author' DOT embedAuthorComponentField    #componentEmbedAuthorField
    | 'embed' DOT 'footer' DOT embedFooterComponentField    #componentEmbedFooterField
;

union: statement | expression;

statement
    : messageComponent                                                                                      #stmtComponent
    | VAR? varname=ID ASSIGN expression                                                                     #stmtAssign
    | IF LBRACE expression RBRACE if=statementBlock (ELSE else=statementBlock)?                             #stmtIf
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
    | statement             #stmtSingular
;

template
    : CONST name=ID ASSIGN expression SEMICOLON     #templateConst
    | statement+                                    #templateStatement
    | STRLIT                                        #templateText
;

COMMENT: '//' ~[\r\n]* -> channel(HIDDEN);
UNMATCHED: .;
