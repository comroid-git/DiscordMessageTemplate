grammar DiscordMessageTemplate;

ASSIGN: '=';
APPEND: '+=';
mutate: ASSIGN | APPEND;
SEMICOLON: ';';
QUOTE: '"';
ESCAPE_QUOTE: '\\"';
HASH: '#';
COMMA: ',';

LBRACE: '(';
RBRACE: ')';
LIDX: '[';
RIDX: ']';
LACC: '{';
RACC: '}';

NOW: 'now';

TEXT: 'text';

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

HEX_DIGIT: [0-9a-fA-F];
STRLIT: QUOTE (ESCAPE_QUOTE | (~[\r\n"]))* QUOTE;

WHITESPACE: [ \t\r\n] -> channel(HIDDEN);

expr
    : STRLIT                                                                #exprString
    | NOW LBRACE RBRACE                                                     #exprNow
    | HASH HEX_DIGIT HEX_DIGIT HEX_DIGIT (HEX_DIGIT HEX_DIGIT HEX_DIGIT)?   #exprHexColor
;

embedAuthorComponentField
    : NAME ASSIGN expr SEMICOLON    #embedAuthorComponentName
    | ICON ASSIGN expr SEMICOLON    #embedAuthorComponentIcon
    | URL ASSIGN expr SEMICOLON     #embedAuthorComponentUrl
;
embedAuthorComponent
    : name=expr (COMMA icon=expr (COMMA url=expr)?)? SEMICOLON  #embedAuthorFlow
    | AUTHOR LACC embedAuthorComponentField+ RACC               #embedAuthorObj
;
embedFooterComponentField
    : TEXT ASSIGN expr SEMICOLON    #embedFooterComponentText
    | ICON ASSIGN expr SEMICOLON    #embedFooterComponentIcon
;
embedFooterComponent
    : text=expr (COMMA icon=expr)? SEMICOLON        #embedFooterFlow
    | FOOTER LACC embedFooterComponentField+ RACC   #embedFooterObj
;
embedFieldComponentField
    : TITLE ASSIGN expr SEMICOLON   #embedFieldComponentTitle
    | TEXT ASSIGN expr SEMICOLON    #embedFieldComponentText
    | INLINE SEMICOLON              #embedFieldComponentInline
;
embedFieldComponentPart
    : title=expr (COMMA text=expr (COMMA INLINE)?)? SEMICOLON   #embedFieldFlow
    | FIELD LACC embedFieldComponentField+ RACC                 #embedFieldObj
;
embedFieldsComponent
    : embedFieldComponentPart               #embedFieldSingular
    | LIDX embedFieldComponentPart+ RIDX    #embedFieldList
;
embedComponent
    : TITLE ASSIGN expr SEMICOLON           #embedTitle
    | URL ASSIGN expr SEMICOLON             #embedUrl
    | DESCRIPTION ASSIGN expr SEMICOLON     #embedDescription
    | AUTHOR mutate embedAuthorComponent    #embedAuthor
    | TIMESTAMP ASSIGN expr SEMICOLON       #embedTimestamp
    | COLOR ASSIGN expr SEMICOLON           #embedColor
    | FOOTER mutate embedFooterComponent    #embedFooter
    | IMAGE ASSIGN expr SEMICOLON           #embedImage
    | THUMBNAIL ASSIGN expr SEMICOLON       #embedThumbnail
    | FIELDS mutate embedFieldsComponent    #embedFields
;

messageComponent
    : TEXT ASSIGN expr SEMICOLON                #componentText
    | EMBED mutate LACC embedComponent+ RACC    #componentEmbed
;

template
    : messageComponent+     #templateComponents
    | STRLIT                #templateText
;

COMMENT: '//' ~[\r\n]* -> channel(HIDDEN);
UNMATCHED: .;
