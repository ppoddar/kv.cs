namespace oracle.kv.client.data {

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using oracle.kv.client.data;
    using oracle.kv.client.error;

    /// <summary>
    /// Parses JSON formatted string to canonical data.
    /// </summary>
    public class JSONParser : ISerializationSupport {
        internal NativeLexer Lexer;
        internal State state;
        internal Stack stack { get; set; }
        static Dictionary<State, Dictionary<Token, State>> PARSE_RULES;

        public enum State {
            INIT,
            OBJECT_START,   // start of a JSON object
            ARRAY_START,    // start of a JSON array
            PROPERTY_NAME,  // name of a property, a quoted string
            PROPERTY_VALUE, // value of a property, can be a string, number, literal,
                            // another JSON object or JSONArray
            NEXT_PROPERTY,  // next property
            NEXT_ELEMENT,   // next array element
            OBJECT_END,     // end of a JSON object
            ARRAY_END,      // end of an array
        }

        /// <summary>
        /// Defines parser grammar rules. 
        /// A rule defines a transition from state  A to state B while a token
        /// T has been emitted by lexer.
        /// </summary>
        /// <remarks>
        /// Unlike a general purpose grammar, these rules are hardcoded for
        /// JSON syntax.
        /// A token is mostly a definite character, one of following set of
        /// characters <code>{,},[,],.,:.</code> 
        /// The lexer looks ahead a character. The looked-ahead character 
        /// and the current state determine the next state.
        /// The action on each state carries out actual parse operation.
        /// The result of parse action is maintained in a stack.
        /// </remarks>
        static JSONParser() {
            PARSE_RULES =
                new Dictionary<State, Dictionary<Token, State>>() {
                 {State.INIT,
                    new Dictionary<Token, State>(){
                        {new Token('{'),State.OBJECT_START},
                        {new Token('['),State.ARRAY_START},
                    }},
                  {State.OBJECT_START,
                     new Dictionary<Token, State>(){
                        {new Token('}'),State.OBJECT_END},
                        {new Token('"'),State.PROPERTY_NAME},
                 }},

                  {State.NEXT_PROPERTY,
                     new Dictionary<Token, State>(){
                        {new Token('"'),State.PROPERTY_NAME},
                 }},

                 {State.PROPERTY_NAME,
                     new Dictionary<Token, State>(){
                        {new Token(':'),State.PROPERTY_VALUE},
                 }},
                  {State.PROPERTY_VALUE,
                     new Dictionary<Token, State>(){
                        {new Token(','),State.NEXT_PROPERTY},
                        {new Token('}'),State.OBJECT_END},
                 }},
                 {State.ARRAY_START,
                     new Dictionary<Token, State>(){
                        {new Token(']'),State.ARRAY_END},
                        {Token.ANY,     State.NEXT_ELEMENT},
                 }},
                 {State.NEXT_ELEMENT,
                     new Dictionary<Token, State>(){
                        {new Token(','), State.NEXT_ELEMENT},
                        {new Token(']'), State.ARRAY_END},
                 }},
            };
        }


        /// <summary>
        /// Serialize the specified object in to a string.
        /// </summary>
        /// <returns>The serialized string in JSON format.</returns>
        /// <remarks>
        /// The object can be a <see cref="DataObject"/> or 
        /// <see cref="DataObjectArray"/> 
        /// </remarks>
        /// <param name="obj">an object to be serialized to a JSON 
        /// formatted string.</param>
        /// <exception cref="ArgumentException">If the given object is
        /// not a <see cref="DataObject"/> or <see cref="DataObjectArray"/> 
        /// </exception>
        public string Serialize(object obj) {
            if (obj == null) return "null";
            if (obj is DataObject) return (obj as DataObject).ToJSONString().ToString();
            if (obj is DataObjectArray) return (obj as DataObjectArray).ToJSONString().ToString();
            throw new ArgumentException("Can not serialize " + obj);
        }


        /// <summary>
        /// Parses the specified string into a <see cref="DataObject"/>.
        /// </summary>
        /// <returns>A <see cref="DataObject"/> instance.</returns>
        /// <param name="str">a JSON formatetd string to be deserialized.</param>
        /// <exception cref="ArgumentException">If any parse error occurs.
        /// </exception>
        public IDataContainer Deserialize(string str) {
            return Parse(str) as IDataContainer;
        }

        /// <summary>
        /// Parses the number by reading the underlying character stream
        /// from the current position.
        /// </summary>
        /// <remarks>
        /// The string is read from current position until a non-numeric
        /// character is encountered. The resultant string is first attempted
        /// to be parsed as a long number and, if not successful, then 
        /// attempted t be parsed as a double.
        /// <para></para>
        /// Thus the parse results any number into either long or double.
        /// Any other type such as byte, short, int, float are widened
        /// as such information is not avaialble in underlying JSON
        /// formatetd character stream.
        /// </remarks>
        /// <returns>A long or double number.</returns>
        internal object ParseNumber() {
            string s = Lexer.ReadNumericString();
            long l = long.MinValue;
            if (long.TryParse(s, out l)) {
                return l;
            } else {
                decimal d = default(decimal);
                if (decimal.TryParse(s, out d)) {
                    return d;
                }
            }
            throw new ArgumentException("Invalid numeric string " + s);
        }

        /// <summary>
        /// Parses a literal by reading the underlying character stream
        /// from the current position.
        /// </summary>
        /// <returns>The literal.</returns>
        /// <remarks>JSON specification defines three literals:<code>
        /// true, false, null</code>. They appear without being enclosed
        /// by a double quote character.
        /// </remarks>
        internal object ParseLiteral() {
            return Literal.FromString(Lexer.ReadLiteralString());
        }

        /// <summary>
        /// The primary parse method parses the underlying stream until
        /// the specified end state.
        /// </summary>
        /// <returns>The parsed object.</returns>
        internal object _Parse() {
            char startChar = Lexer.GetCurrentToken();
            State end;
            if (startChar == '{') {
                state = State.OBJECT_START;
                end = State.OBJECT_END;
            } else if (startChar == '[') {
                state = State.ARRAY_START;
                end = State.ARRAY_END;
            } else {
                throw new ArgumentException("start character " + startChar + " is neither { nor [");
            }
            stack = new Stack();
            while (Lexer.HasMoreTokens && state != end) {
                Lexer.SkipWhitespace();
                state.Enter(this);
                Lexer.SkipWhitespace();
                char token = Lexer.GetNextToken();
                state = NextState(token);
            }
            return stack.Pop();
        }

        internal object _ParseRecursive() {
            JSONParser clone = new JSONParser();
            clone.Lexer = this.Lexer;
            return clone._Parse();
        }

        /// <summary>
        /// Parse the specified string to a DataObject.
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="s">a string to parse.</param>
        public object Parse(string s) {
            this.Lexer = new NativeLexer(s);
            return _Parse();
        }


        /// <summary>
        /// Computes the next parser state.
        /// </summary>
        /// <returns>The next state.</returns>
        /// <param name="ch">Character token observed aftter the next state.</param>
        State NextState(char ch) {
            if (!PARSE_RULES.ContainsKey(state)) {
                throw new ArgumentException("start state " + state
                    + " has not transition");
            } else {
                Dictionary<Token, State> nextStates = PARSE_RULES[state];
                Token token = new Token(ch);
                if (!nextStates.ContainsKey(token)) {
                    if (nextStates.ContainsKey(Token.ANY)) {
                        Lexer.pushBack();
                        return nextStates[Token.ANY];
                    }
                    throw new ArgumentException("state " + state
                        + " has no transition for token " + ch
                        + ". Expecting either of "
                        + string.Join(" ", GetPossibleTokens()));
                }
                State next = nextStates[token];
                return next;
            }
        }

        char[] GetPossibleTokens() {
            try {
                Dictionary<Token, State> nextStates = PARSE_RULES[state];
                return nextStates.Keys.Select((t) => t.Char).ToArray();
            } catch (KeyNotFoundException) {
                return new char[0];
            }
        }

        /// <summary>
        /// A stack to hold intermediate parse result.
        /// </summary>
        internal class Stack {
            readonly System.Collections.Generic.Stack<object> _stack =
                new System.Collections.Generic.Stack<object>();

            internal object Pop() {
                return _stack.Pop();
            }

            internal void Push(object name) {
                _stack.Push(name);
            }

            internal void SetPropertyValue(object storedValue) {
                object current = _stack.Peek();
                if (current is DataObjectArray) {
                    (current as DataObjectArray).Add(storedValue);
                } else if (current is string) {
                    string propertyName = _stack.Pop() as string;
                    (_stack.Peek() as DataObject)[propertyName] = storedValue;
                }
            }
        }

    }

    /// <summary>
    ///  Defines action for each parse state.
    /// </summary>
    public static class StateExtensions {
        // 
        /// <summary>
        ///  Enter given state. Perform an action. 
        /// </summary>
        /// <returns>The enter.</returns>
        /// <param name="state">current state.</param>
        /// <param name="parser">parser.</param>
        public static void Enter(this JSONParser.State state, JSONParser parser) {
            switch (state) {
                case JSONParser.State.OBJECT_START:
                    parser.stack.Push(new DataObject());
                    break;
                case JSONParser.State.ARRAY_START:
                    parser.stack.Push(new DataObjectArray());
                    break;
                case JSONParser.State.NEXT_PROPERTY:
                    break;
                case JSONParser.State.PROPERTY_NAME:
                    string propertyName = parser.Lexer.ReadQuotedString();
                    parser.stack.Push(propertyName);
                    break;
                case JSONParser.State.PROPERTY_VALUE:
                case JSONParser.State.NEXT_ELEMENT:
                    char lookAhead = parser.Lexer.GetNextToken();
                    object value = null;
                    switch (lookAhead) {
                        case '+':
                        case '-':
                        case '.':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            value = parser.ParseNumber();
                            parser.Lexer.pushBack();
                            break;
                        case '"':
                            value = parser.Lexer.ReadQuotedString();
                            break;
                        case 'n':
                        case 't':
                        case 'f':
                            string literal = parser.Lexer.ReadLiteralString();
                            value = Literal.FromString(literal).AsValue();
                            parser.Lexer.pushBack();
                            break;
                        case '{':
                        case '[':
                            value = parser._ParseRecursive();
                            break;
                        default:
                            throw new ArgumentException("Unexpected next token "
                                + lookAhead + " at " + state);
                    }
                    parser.stack.SetPropertyValue(value);
                    break;

            }
        }
    }



    /// <summary>
    /// </summary>
    internal class NativeLexer {
        readonly string stream;
        int Cursor { get; set; }

        readonly static char EOS = '\0';
        readonly static char[] NUMERIC_CHARS = { '+', '-', '.' };

        internal NativeLexer(string str) {
            if (string.IsNullOrEmpty(str))
                throw new ArgumentException("Can not parse null or empty string"
                + new StackTrace());
            stream = str;
            Cursor = 0;
        }

        public void SkipWhitespace() {
            for (; Char.IsWhiteSpace(GetNextToken());) { }
            pushBack();
        }

        internal char GetCurrentToken() {
            return (Cursor < stream.Length) ? stream[Cursor] : EOS;
        }

        /// <summary>
        /// Gets the next token.
        /// </summary>
        /// <remarks>
        /// The cursor advances as a side-effect.
        /// </remarks>
        /// <returns>The next character from the stream. Or empty character 
        /// <code>0</code> if stream has no character.
        /// </returns>
        internal char GetNextToken() {
            if (Cursor < stream.Length - 1) {
                Cursor = Cursor + 1;
                char ch = stream[Cursor];
                return ch;
            }
            return EOS;
        }

        internal bool HasMoreTokens {
            get {
                return Cursor < stream.Length - 1;
            }
        }

        internal void pushBack() {
            Cursor--;
        }

        internal string ReadQuotedString() {
            Predicate<char> entry = (c) => c == '"';
            Predicate<char> exit = (c) => c == '"';
            return ReadOnCondition(entry, exit).Substring(1);
        }

        internal string ReadNumericString() {
            Predicate<char> entry = (c) => char.IsDigit(c)
                                 || NUMERIC_CHARS.Contains(c);
            Predicate<char> exit = (c) => !(
                    char.IsDigit(c)
                    || NUMERIC_CHARS.Contains(c)
                    || c == 'e' || c == 'E');
            return ReadOnCondition(entry, exit);
        }

        internal string ReadLiteralString() {
            Predicate<char> entry = (c) => true;
            Predicate<char> exit = (c) => !char.IsLetter(c);
            return ReadOnCondition(entry, exit);
        }

        string ReadOnCondition(Predicate<char> entry, Predicate<char> exit) {
            char c = GetCurrentToken();
            if (!entry(c)) {
                throw new ArgumentException("entry condition not satisfied");
            }
            bool escapeMode;
            for (int i = 1; Cursor + i < stream.Length; i++) {
                c = stream[Cursor + i];
                escapeMode = c == '\\';
                if (escapeMode) continue;
                if (exit(c)) {
                    string str = stream.Substring(Cursor, i);
                    Cursor += i;
                    return str;
                }
            }
            throw new ArgumentException("exit condition not satisfied");
        }

    }

    internal class Token {
        readonly int ch;
        public static Token ANY = new Token();
        public char Char { get { return (char)ch; } }

        Token() {
        }

        public Token(char c) {
            ch = c;
        }

        /// <summary>
        /// equals to another token or a character.
        /// equals to any input if this is ANY token
        /// </summary>
        /// <param name="obj">a Token or a character</param>
        public override bool Equals(object obj) {
            if (this == ANY) return true;
            if (obj is Token) return (obj as Token).ch == this.ch;
            if (obj is char) return (char)obj == this.ch;
            return false;
        }

        public override int GetHashCode() {
            return ch;
        }
    }


    /// <summary>
    /// Denotes literal values <code>null, true and false</code> as defined in
    ///  JSON format.
    /// </summary>
    internal class Literal {

        public static Literal NULL = new Literal("null");
        public static Literal TRUE = new Literal("true");
        public static Literal FALSE = new Literal("false");

        string literal { get; set; }

        Literal(string v) {
            literal = v;
        }

        public object AsValue() {
            switch (literal) {
                case "true": return true;
                case "false": return false;
                case "null": return null;
                default:
                    throw new InternalError("Unknow literal [" + literal + "]");
            }
        }

        public static Literal FromString(string str) {
            switch (str.ToLower()) {
                case "false": return FALSE;
                case "true": return TRUE;
                case "null": return NULL;
                default:
                    throw new ArgumentException("Unknow literal value [" + str + "]");
            }
        }

        public override string ToString() {
            return literal;
        }

        public string ToJSONString() {
            return literal.ToLower();
        }

    }
}


