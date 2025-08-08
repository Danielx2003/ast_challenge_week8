using System.Diagnostics.Contracts;
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
        parser.Parse("f(g(x), b, c)(z);");
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
        }


        private List<Node> tree = new List<Node>();
        private int index = 0;
        public List<Token> tokens;
        public Token? nextToken => index+1 < tokens.Count ? tokens[index + 1] : null;

        public void Parse(string exp)
        {
            tokens = Tokenizer.GetTokens(exp);

            Console.WriteLine("Starting: ");

            if (tokens[index].Type == TokenType.Identifier)
            {
                var node = ParseIdentifier();
            }

            Console.WriteLine("DONE");
            Console.ReadLine();

        }

        public void ParseExpression()
        {
            if (tokens[index].Type == TokenType.OpenBracket)
            {
                index++;
                ParseCall();
            }
        }

        public Node ParseIdentifier()
        {
            var id = new IdentifierNode
            {
                Name = tokens[index].Value
            };

            while (nextToken != null && nextToken.Type == TokenType.OpenBracket)
            {
                index++;
                ParseCall();

            }

            ParseExpression();

            if (tokens[index].Type == TokenType.Identifier)
            {
                Console.WriteLine(tokens[index].Value);
            }

            index++;

            return id;
        }

        public void ParseCall()
        {
            while (tokens[index].Type != TokenType.ClosedBracket)
            {
                if (tokens[index].Type == TokenType.Identifier)
                {
                    ParseIdentifier();
                }
                else
                {
                    index++;
                }

            }
            Console.WriteLine("Close Function Call");

            index++;
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
                        tokens.Add(new Token { Type = TokenType.Identifier, Value = id });
                        id = string.Empty;
                    }

                    tokens.Add(new Token { Type = TokenType.OpenBracket, Value = "(" });
                }
                else if (input[index] == ')')
                {
                    if (id != string.Empty)
                    {
                        tokens.Add(new Token { Type = TokenType.Identifier, Value = id });
                        id = string.Empty;
                    }

                    tokens.Add(new Token { Type = TokenType.ClosedBracket, Value = ")" });
                }
                else if (input[index] == ';')
                {
                    if (id != string.Empty)
                    {
                        tokens.Add(new Token { Type = TokenType.Identifier, Value = id });
                        id = string.Empty;
                    }

                    tokens.Add(new Token { Type = TokenType.SemiColon, Value = ";" });
                }
                else if (input[index] == ',')
                {
                    if (id != string.Empty)
                    {
                        tokens.Add(new Token { Type = TokenType.Identifier, Value = id });
                        id = string.Empty;
                    }

                    tokens.Add(new Token { Type = TokenType.Comma, Value = "," });
                }
                else
                {
                    id += input[index];
                }

                index++;
            }

            return tokens;
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