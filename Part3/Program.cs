using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using static Part2.Program.Parser;
using static Part2.Program.Tokenizer;

namespace Part2;

public class Program
{
    static void Main(string[] args)
    {
        /*
         * Expression = Primary, ( Call )*
         * Primary = Identifier | ( "(" Expression ")" )
         * Call = ("(" ArgsList ")" , Return )?
         * ArgsList = Expression, ( "," Expression )*
         * Return = ReturnType Identifier
         */

        var parser = new Parser();
        //string input = @"
        //    declare int x;
        //    declare string y;
        //    declare bool z;
        //    f(g(x), y)(z);
        //    ";

        string input = @"
            f(string) -> A;
            g(B) -> string;
            U(T, T) -> T;
            U(f, g) -> T;
            ";

        var parts = input
            .Split('\n')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        string res = null;

        foreach (var part in parts)
        {
            res = parser.Parse(part);
        }
        Console.WriteLine(res);

        Console.ReadLine();
    }


    public class Parser
    {
        public abstract class Node { }

        public class CallNode : Node
        {
            public Node Callee { get; set; } // would be the callee
            public List<Node> Arguments { get; set; } // is the argument List
            public List<Node> Returns { get; set; }

            public override string ToString()
            {
                var allTypes = Arguments.Concat(Returns).Select(a => a.ToString());
                string result = string.Join(" -> ", allTypes);
                return result;
            }
        }

        public class IdentifierNode : Node
        {
            public string Name { get; set; }
            public override string ToString()
            {
                return $"{Name}";
            }
        }

        private int index = 0;
        public List<Token> tokens;
        public Token? nextToken => index + 1 < tokens.Count ? tokens[index + 1] : null;
        public Dictionary<string, string> variableTypeMap = new Dictionary<string, string>();

        public string Parse(string exp)
        {
            tokens = Tokenizer.GetTokens(exp);
            index = 0;

            if (!Tokenizer.ValidateTokens(tokens))
            {
                Console.WriteLine("Invalid syntax");
                Console.ReadLine();
            }

            if (tokens[index].Type == TokenType.Identifier)
            {
                var id = tokens[index].Value;
                var node = ParseIdentifier();
                UpdateTypes(id, node);
                return variableTypeMap[id];
            }

            return null;
        }

        class ExpressionType 
        {

        }

        class FunctionType : ExpressionType
        {
            public List<string> Parameters { get; set; }
            public string ReturnType { get; set; }
        }

        class VariableType : ExpressionType
        {
            public string Name { get; set; }
        }

        FunctionType ParseFunctionType(string typeStr)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            int depth = 0;

            for (int i = 0; i < typeStr.Length; i++)
            {
                char c = typeStr[i];

                if (c == '(')
                {
                    depth++;
                    current.Append(c);
                }
                else if (c == ')')
                {
                    depth--;
                    current.Append(c);
                }
                else if (c == '-' && i + 1 < typeStr.Length && typeStr[i + 1] == '>' && depth == 0)
                {
                    parts.Add(current.ToString().Trim());
                    current.Clear();
                    i++;
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                parts.Add(current.ToString().Trim());

            if (parts.Count < 2)
                throw new Exception("Invalid function type");

            var parameters = parts.Take(parts.Count - 1).ToList();
            var returnType = parts.Last();

            return new FunctionType { Parameters = parameters, ReturnType = returnType };
        }

        public void UpdateTypes(string id, Node node)
        {
            if (!variableTypeMap.ContainsKey(id))
            {
                variableTypeMap[id] = node.ToString();
                return;
            }

            var existingFunc = ParseFunctionType(variableTypeMap[id]);
            var newFunc = ParseFunctionType(node.ToString());

            if (existingFunc.Parameters.Count != newFunc.Parameters.Count)
                throw new Exception($"Type mismatch: different number of parameters");

            for (int i = 0; i < existingFunc.Parameters.Count; i++)
            {
                UnifyTypes(existingFunc.Parameters[i], newFunc.Parameters[i]);
            }

            UnifyTypes(existingFunc.ReturnType, newFunc.ReturnType);
        }

        public string SubstituteGenerics(string signature)
        {
            var parts = signature.Split("->").Select(p => p.Trim()).ToArray();

            for (int i = 0; i < parts.Length; i++)
            {
                if (variableTypeMap.ContainsKey(parts[i]))
                {
                    parts[i] = variableTypeMap[parts[i]];
                }
            }

            return string.Join(" -> ", parts);
        }

        public Node ParseExpression()
        {
            if (!variableTypeMap.ContainsKey(tokens[index].Value))
            {
                throw new Exception($"Variable {tokens[index].Value} not declared");
            }

            var id = new IdentifierNode
            {
                Name = "(" + variableTypeMap[tokens[index].Value].ToString() + ")"
            };

            CallNode call = null;

            if (tokens[index].Type == TokenType.OpenBracket)
            {
                index++;
                var (args, returns) = ParseCall();

                call = new CallNode
                {
                    Callee = id,
                    Arguments = args,
                    Returns = returns,
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
            Node node = null;

            if (!variableTypeMap.ContainsKey(tokens[index].Value))
            {
                node = new IdentifierNode
                {
                    Name = tokens[index].Value
                };
            }
            else
            {
                node = new IdentifierNode
                {
                    Name = "(" + variableTypeMap[tokens[index].Value].ToString() + ")"
                };
            }

            index++;

            while (index < tokens.Count && tokens[index].Type == TokenType.OpenBracket)
            {
                index++;
                var (args, returns) = ParseCall();
                node = new CallNode
                {
                    Callee = node,
                    Arguments = args,
                    Returns = returns,
                };
            }

            return node;
        }

        public (List<Node>, List<Node>) ParseCall()
        {
            var argsList = new List<Node>();
            var returnList = new List<Node>();

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

            if (tokens[index].Type == TokenType.ReturnType)
            {
                index++;
                returnList.Add(ParseIdentifier());
            }

            return (argsList, returnList);
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
                    if (input[index] == ' ')
                    {
                        if (id == "declare")
                        {
                            tokens.Add(new Token { Type = TokenType.Declare, Value = id.Trim() });
                            id = string.Empty;
                        }

                        if (id == "int")
                        {
                            tokens.Add(new Token { Type = TokenType.VariableType, Value = id.Trim(), VariableType = VariableTypes.String });
                            id = string.Empty;
                        }

                        if (id == "string")
                        {
                            tokens.Add(new Token { Type = TokenType.VariableType, Value = id.Trim(), VariableType = VariableTypes.String });
                            id = string.Empty;
                        }

                        if (id == "->")
                        {
                            tokens.Add(new Token { Type = TokenType.ReturnType, Value = id.Trim() });
                            id = string.Empty;
                        }
                    }
                    else
                    {
                        id += input[index];
                    }

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
                    if (tokens[i - 1].Type == TokenType.Comma)
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

            if (tokens[i - 1].Type != TokenType.SemiColon)
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
            public VariableTypes VariableType { get; set; }
        }

        public enum TokenType
        {
            OpenBracket = 1,
            ClosedBracket = 2,
            Comma = 3,
            Identifier = 4,
            SemiColon = 5,
            Declare = 6,
            VariableType = 7,
            ReturnType = 8,
        }

        public enum VariableTypes
        {
            String = 1,
            Int = 2
        }
    }
}