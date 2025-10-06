grammar Alfa;

namespaceToEOF: namespace EOF;
attributeDeclarationToEOF: attributeDeclaration EOF;
obligationNameDeclarationToEOF: obligationNameDeclaration EOF;
adviceNameDeclarationToEOF : adviceNameDeclaration EOF;
attributeCategoryNameDeclarationToEOF: attributeCategoryNameDeclaration EOF;
importNamespaceToEOF: importNamespace EOF;
attributeDesignatorToEOF: attributeDesignator EOF;
permitDenyRuleToEOF: permitDenyRule EOF;
policyToEOF: policy EOF;
policysetToEOF: policyset EOF;
booleanExpressionToEOF: booleanExpression EOF;
expressionToEOF: expression EOF;
functionToEOF: function EOF;
functionDeclarationToEOF: functionDeclaration EOF; 

// Rules

body    : namespace* EOF;
 
namespace   : NAMESPACE IDENTIFIER BRACE_OPEN statement* BRACE_CLOSE
            | err=NAMESPACE BRACE_OPEN statement* BRACE_CLOSE { NotifyErrorListeners(_localctx.err, "Missing namespace identifier", null); };


importNamespace : IMPORT IDENTIFIER WILDCARD?
                | err=IMPORT { NotifyErrorListeners(_localctx.err, "import must be followed by a namespace path", null);}
                ;
                    
attributeCategoryAssignment : CATEGORY ASSIGNMENT_OPERATOR IDENTIFIER;
attributeIdAssignment : ID ASSIGNMENT_OPERATOR LITERAL_STRING;
attributeTypeAssignment: TYPE ASSIGNMENT_OPERATOR typeName;

typeName: STRING | BOOLEAN | INTEGER | DOUBLE | TIME | DATETIME | DATE | DURATION;
multipleParamsOfTypeName: functionSignatureArgument MULTIPLY;
bagOfTypeName: BAG SQUAREBRACE_OPEN typeName SQUAREBRACE_CLOSE ;

functionDeclaration: FUNCTION IDENTIFIER ASSIGNMENT_OPERATOR LITERAL_STRING ':' functionSignatureDeclarations ;
functionSignatureDeclarations: (functionSignatureDeclaration OVERLOAD_OPERATOR)* functionSignatureDeclaration;
functionSignatureDeclaration: functionSignatureInputs '->' functionSignatureOutputs ;
functionSignatureInputs: functionSignatureArgument* multipleParamsOfTypeName? 
                       | functionSignatureArgument* err=multipleParamsOfTypeName* functionSignatureArgument* 
                        { NotifyErrorListeners(_localctx.err.Start, "Variable parameter must be last parameter", null); }
                       ;
                       
functionSignatureOutputs: functionSignatureArgument; 
functionSignatureArgument: typeName | bagOfTypeName;

attributeDeclaration : err=ATTRIBUTE IDENTIFIER BRACE_OPEN attributeBody BRACE_CLOSE
                     | err=ATTRIBUTE { NotifyErrorListeners(_localctx.err, "Incomplete attribute declaration", null); }
                     | err=ATTRIBUTE IDENTIFIER { NotifyErrorListeners(_localctx.err, "Incomplete attribute declaration", null); }
                    ;
attributeBody : ( attributeCategoryAssignment | attributeIdAssignment | attributeTypeAssignment )*;

permitDenyRule : RULE IDENTIFIER? BRACE_OPEN (ruleEffect | target | condition | onPermit | onDeny  )* BRACE_CLOSE;
ruleEffect : ( PERMIT | DENY ) ;

onPermit : ON_PERMIT BRACE_OPEN (obligation | advice)+ BRACE_CLOSE
         | err=ON_PERMIT (BRACE_OPEN BRACE_CLOSE)* 
            { NotifyErrorListeners(_localctx.err, "on permit must contain at least one piece of advice or obligation", null); }
         ;
         
onDeny  : ON_DENY BRACE_OPEN (obligation|advice)+ BRACE_CLOSE
        | err=ON_DENY (BRACE_OPEN BRACE_CLOSE)* 
            { NotifyErrorListeners(_localctx.err, "on deny must contain at least one piece of advice or obligation", null); }
        ;

obligation : OBLIGATION IDENTIFIER attributeAssignments?;
advice : ADVICE IDENTIFIER attributeAssignments?;

attributeAssignments : BRACE_OPEN attributeAssignment* BRACE_CLOSE;
attributeAssignment : IDENTIFIER ASSIGNMENT_OPERATOR ( expression | booleanExpression);

attributeDesignator : IDENTIFIER
                    | IDENTIFIER SQUAREBRACE_OPEN MUSTBEPRESENT SQUAREBRACE_CLOSE;
         
policyset : POLICYSET IDENTIFIER? BRACE_OPEN policysetBody BRACE_CLOSE;
policysetBody : (target | policyCombinator | policysetPolicy)* 
              ;
policysetPolicy : (policy | POLICY IDENTIFIER | policyset | onPermit | onDeny )
                | IDENTIFIER { NotifyErrorListeners("Named policy references should be preceded by the 'policy' keyword"); };

target : TARGET clause*;

policyCombinator    : APPLY  ( COMBINE_DENY_OVERRIDES | COMBINE_PERMIT_OVERRIDES | COMBINE_FIRST_APPLICABLE | COMBINE_ONLY_ONE_APPLICABLE | COMBINE_DENY_UNLESS_PERMIT | COMBINE_PERMIT_UNLESS_DENY );
policy  : POLICY IDENTIFIER? BRACE_OPEN policyBody BRACE_CLOSE;
policyBody : ( target | policyCombinator | permitDenyRule | onPermit | onDeny )* 
           ;
           
condition : CONDITION booleanExpression
          | err=CONDITION { NotifyErrorListeners(_localctx.err, "Condition must have a boolean expression", null);}
          ;
 
clause : CLAUSE booleanExpression
       | err=CLAUSE { NotifyErrorListeners(_localctx.err, "Missing boolean expression for clause", null);}
       ;

all : ALL '(' expression ')'
    | ALL { NotifyErrorListeners(_localctx.Start, "all(<expression>) must contain an attribute expression", null); }
    | ALL '(' expression? { NotifyErrorListeners(_localctx.Start, "all missing closing )", null); }
    | ALL '(' ')' { NotifyErrorListeners(_localctx.Start, "all(<expression>) must contain an attribute expression", null); }
    ;
    
functionParameters : expression (',' expression)*;
function : IDENTIFIER PAREN_OPEN functionParameters? PAREN_CLOSE;

valueCoercion : LITERAL_STRING ':' DATETIME # StringToDateTime
              | LITERAL_STRING ':' DATE     # StringToDate 
              | LITERAL_STRING ':' TIME     # StringToTime
              | LITERAL_STRING ':' DURATION # StringToDuration
              | LITERAL_STRING ':'  
              { NotifyErrorListeners(_localctx.Start, "Value coercion is only supported for date, time, dateTime and duration", null);} #StringToError
              ;
expressionLiterals : LITERAL_STRING    # LiteralString
                   | LITERAL_INTEGER    # LiteralInteger
                   | LITERAL_DOUBLE     # LiteralDouble
                   | booleanLiterals    # LiteralBoolean
                   ;

expression : expressionLiterals
           | attributeDesignator
           | valueCoercion
           | all
           | function
           | PAREN_OPEN expression PAREN_CLOSE
           | expression op=DIVIDE expression
           | expression op=MULTIPLY expression
           | expression op=MINUS expression
           | expression op=PLUS expression    
          ;
                         
booleanLiterals : LITERAL_TRUE      # LiteralBooleanTrue
                | LITERAL_FALSE     # LiteralBooleanFalse
                ;
                
inverseBooleanExpression :  NOT PAREN_OPEN booleanExpression PAREN_CLOSE    
                         | NOT booleanFunction
                         | NOT booleanLiterals
                         | NOT booleanAll
                         | NOT booleanAttributeDesignator
                         ;

booleanAttributeDesignator : attributeDesignator;
booleanFunction : function;
booleanAll : all;
      
booleanExpression : inverseBooleanExpression    
                  | booleanLiterals
                  | booleanAttributeDesignator
                  | booleanExpression op=AND booleanExpression     
                  | booleanExpression op=OR booleanExpression
                  | booleanExpression op=ANDCLAUSE booleanExpression   
                  | booleanExpression op=ORCLAUSE booleanExpression
                  | expression op=EQUAL expression                 
                  | expression op=NOTEQUAL expression              
                  | expression op=GREATERTHAN expression           
                  | expression op=LESSTHAN expression              
                  | expression op=GREATERTHANANDEQUAL expression   
                  | expression op=LESSTHANANDEQUAL expression                                          
                  | booleanFunction 
                  | booleanAll                                                    
                  | PAREN_OPEN booleanExpression PAREN_CLOSE    
                  ;  
                  
obligationNameDeclaration : OBLIGATION IDENTIFIER ASSIGNMENT_OPERATOR LITERAL_STRING;
adviceNameDeclaration : ADVICE IDENTIFIER ASSIGNMENT_OPERATOR LITERAL_STRING;
attributeCategoryNameDeclaration : CATEGORY IDENTIFIER ASSIGNMENT_OPERATOR LITERAL_STRING;


statement : namespace
          | importNamespace
          | attributeDeclaration
          | obligationNameDeclaration
          | adviceNameDeclaration
          | attributeCategoryNameDeclaration
          | policyset
          | policy
          | functionDeclaration;
         
// Tokens              
MUSTBEPRESENT : 'mustbepresent';
          
ON_PERMIT   :'on permit';
ON_DENY     :'on deny';

OBLIGATION  : 'obligation';
ADVICE      : 'advice';
         
APPLY       : 'apply';
POLICY      : 'policy';
POLICYSET   : 'policyset';
RULE        : 'rule';
PERMIT      : 'permit';
DENY        : 'deny';
TARGET      : 'target';
CLAUSE      : 'clause';
CONDITION   : 'condition';
AND         : '&&';
OR          : '||';
FUNCTION    : 'function';
ASSIGNMENT_OPERATOR : '=';
OVERLOAD_OPERATOR: '|';

ANDCLAUSE   : 'and';
ORCLAUSE    : 'or';

IMPORT      : 'import';
ATTRIBUTE   : 'attribute';
CATEGORY    : 'category';
ID          : 'id';
TYPE        : 'type';
STRING      : 'string';
BOOLEAN     : 'boolean';
INTEGER     : 'integer';
DOUBLE      : 'double';
DATETIME    : 'dateTime';
DATE        : 'date';
TIME        : 'time';
DURATION    : 'duration';
NAMESPACE   : 'namespace';
BAG         : 'bag';
ALL         : 'all';

COMBINE_DENY_OVERRIDES : 'denyOverrides';
COMBINE_PERMIT_OVERRIDES : 'permitOverrides';
COMBINE_FIRST_APPLICABLE : 'firstApplicable';
COMBINE_ONLY_ONE_APPLICABLE : 'onlyOne';
COMBINE_DENY_UNLESS_PERMIT : 'denyUnlessPermit';
COMBINE_PERMIT_UNLESS_DENY : 'permitUnlessDeny';

LITERAL_STRING : QUOTE ~( '\n' | '\r' | '"' )*  QUOTE
               | SINGLE_QUOTE ~( '\n' | '\r' | '\'' )* SINGLE_QUOTE;


LITERAL_INTEGER : [0-9]+;
LITERAL_DOUBLE : [0-9]*'.'[0-9]+;
LITERAL_TRUE    : 'true';
LITERAL_FALSE   : 'false';


LITERAL_TYPE_DESIGNTOR: ':';
EQUAL : '==';
NOTEQUAL : '!=';
GREATERTHAN : '>';
LESSTHAN : '<';
GREATERTHANANDEQUAL : '>=';
LESSTHANANDEQUAL : '<=';
NOT : 'not';
PLUS : '+';
MINUS : '-';
MULTIPLY : '*';
DIVIDE : '/';

BRACE_OPEN : '{';
BRACE_CLOSE : '}';
PAREN_OPEN : '(';
PAREN_CLOSE : ')';
SQUAREBRACE_OPEN : '[';
SQUAREBRACE_CLOSE : ']';
                        
INLINECOMMENT : '//' LITERAL_STRING_BODY-> channel(HIDDEN);
COMMENTBLOCK : '/*' .*? '*/' -> channel(HIDDEN);

IDENTIFIER		           : [a-zA-Z_][a-zA-Z0-9_]+('.'[a-zA-Z_][a-zA-Z0-9_]+)*;
WILDCARD    : ('.*');

WHITESPACE  : (' ' | '\t')+ -> skip;
NEWLINE     : ('\r'? '\n' | '\r')+ -> skip;

fragment LITERAL_STRING_BODY : ~( '\n' | '\r'  )*;
fragment QUOTE : '"' ;
fragment SINGLE_QUOTE : '\'';

// Tokenise syntax errors so that we can report from the parser with better context
// This needs to be the last token in this file
BAD_INPUT: . ;