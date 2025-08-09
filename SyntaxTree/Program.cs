using System.Diagnostics.Contracts;
using static SyntaxTree.Program.Parser;
using static SyntaxTree.Program.Tokenizer;

namespace SyntaxTree;

public class Program
{
    static void Main(string[] args)
    {
        /*
         * grammar:
         * F -> Call
         * ArgList -> E(,E)*
         * E -> Literal
         * 
         * new:
         * Expression = Primary, ( Call )*
         * Primary = Identifier | ( "(" Expression ")" )
         * Call = ("(" ArgsList ")" )?
         * ArgsList = Expression, ( "," Expression )*
         * 
         */
        var parser = new Parser();
        parser.Parse("f()(g(), h(), );");
    }


    public class Parser
    {
        public abstract class Node { }

        public class CallNode : Node
        {
            public Node Callee { get; set; } // would be the callee
            public List<Node> Arguments { get; set; } // is the argument List

            public override string ToString()
            {
                string argsStr = string.Join(", ", Arguments.Select(a => a.ToString()));
                return $"Call({Callee}, [{argsStr}])";
            }
        }

        public class IdentifierNode : Node
        {
            public string Name { get; set; }
            public override string ToString()
            {
                return $"'{Name}'";
            }
        }

        private int index = 0;
        public List<Token> tokens;
        public Token? nextToken => index+1 < tokens.Count ? tokens[index + 1] : null;

        public void Parse(string exp)
        {
            tokens = Tokenizer.GetTokens(exp);

            if (!Tokenizer.ValidateTokens(tokens))
            {
                Console.WriteLine("Invalid syntax");
                Console.ReadLine();
            }

            if (tokens[index].Type == TokenType.Identifier)
            {
                var node = ParseIdentifier();
                Console.WriteLine(node.ToString());
            }

            Console.ReadLine();

        }

        public Node ParseExpression()
        {
            var id = new IdentifierNode
            {
                Name = tokens[index].Value
            };
            CallNode call = null;

            if (tokens[index].Type == TokenType.OpenBracket)
            {
                index++;
                var args = ParseCall();

                call = new CallNode
                {
                    Callee = id,
                    Arguments = args
                };
            }

            if (call != null)
            {
                return call;
            }

            return id;

        }

        public Node ParseIdentifier()
        {
            Node node = new IdentifierNode
            {
                Name = tokens[index].Value
            };
            index++;

            while (index < tokens.Count && tokens[index].Type == TokenType.OpenBracket)
            {
                index++;
                var args = ParseCall();
                node = new CallNode
                {
                    Callee = node,
                    Arguments = args
                };
            }

            return node;
        }

        public List<Node> ParseCall()
        {
            var argsList = new List<Node>();

            while (tokens[index].Type != TokenType.ClosedBracket)
            {
                if (tokens[index].Type == TokenType.Identifier)
                {
                    argsList.Add(ParseIdentifier());
                }
                else
                {
                    index++;
                }

            }
            index++;
            return argsList;
        }
    }

    public static class Tokenizer
    {
        public static List<Token> GetTokens(string input)
        {
            var tokens = new List<Token>();
            int index = 0;
            var id = string.Empty;

            while (index < input.Length)
            {
                if (input[index] == '(')
                {
                    if (id != string.Empty)
                    {
                        tokens.Add(new Token { Type = TokenType.Identifier, Value = id.Trim() });
                        id = string.Empty;
                    }

                    tokens.Add(new Token { Type = TokenType.OpenBracket, Value = "(" });
                }
                else if (input[index] == ')')
                {
                    if (id != string.Empty)
                    {
                        tokens.Add(new Token { Type = TokenType.Identifier, Value = id.Trim() });
                        id = string.Empty;
                    }

                    tokens.Add(new Token { Type = TokenType.ClosedBracket, Value = ")" });
                }
                else if (input[index] == ';')
                {
                    if (id != string.Empty)
                    {
                        tokens.Add(new Token { Type = TokenType.Identifier, Value = id.Trim() });
                        id = string.Empty;
                    }

                    tokens.Add(new Token { Type = TokenType.SemiColon, Value = ";" });
                }
                else if (input[index] == ',')
                {
                    if (id != string.Empty)
                    {
                        tokens.Add(new Token { Type = TokenType.Identifier, Value = id.Trim() });
                        id = string.Empty;
                    }

                    tokens.Add(new Token { Type = TokenType.Comma, Value = "," });
                }
                else
                {
                    if (input[index] == ' ' && id != string.Empty)
                    {
                        throw new Exception("SYNTAXT ERROR");
                    }

                    id += input[index];
                }

                index++;
            }

            return tokens;
        }

        public static bool ValidateTokens(List<Token> tokens)
        {
            /*
             * validate tokens:
             * - if there is a comma then closed bracket, throw
             * - if there are not the same amount of open and closed brackets
             * - if it does not end with a semi colon
             */

            int brackets = 0; // add 1 for open, minus 1 for closed, should be 0 at the end
            Token prevToken = tokens[0];
            int i = 1;
            for (i = 1; i < tokens.Count; i++)
            {
                if (tokens[i].Type == TokenType.OpenBracket)
                {
                    brackets++;
                }
                else if (tokens[i].Type == TokenType.ClosedBracket)
                {
                    if (tokens[i-1].Type == TokenType.Comma)
                    {
                        Console.WriteLine("Syntax Error: Cannot be ,)");
                        return false;
                    }

                    brackets--;
                }

                if (tokens[i].Type == TokenType.Identifier)
                {
                    if (tokens[i].Value.Any(x => Char.IsWhiteSpace(x)))
                    {
                        Console.WriteLine("Syntax Error: Whitespace in identifier");
                        return false;
                    }

                    if (string.IsNullOrEmpty(tokens[i].Value))
                    {
                        Console.WriteLine("Syntax Error: Identifier it null");
                        return false;
                    }    

                }
            }

            if (tokens[i-1].Type != TokenType.SemiColon)
            {
                Console.WriteLine("Syntax error: Must end with semi colon");
                return false;
            }

            if (brackets != 0)
            {
                Console.WriteLine("Syntax error: open and closing brackets dont match");
                return false;
            }


            return true;
        }
        public class Token
        {
            public TokenType Type { get; set; }
            public string Value { get; set; }
        }

        public enum TokenType
        {
            OpenBracket = 1,
            ClosedBracket = 2,
            Comma = 3,
            Identifier = 4,
            SemiColon = 5,
        }
    }
}