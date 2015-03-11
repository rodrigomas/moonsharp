﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonSharp.Interpreter.Tree
{
	class Lexer
	{
		Token m_Current = null;

		string m_Code;
		int m_Cursor = 0;
		int m_Line = 0;
		int m_Col = 0;

		public Lexer(string scriptContent)
		{
			m_Code = scriptContent; // have a sentinel
		}


		public Token PeekToken()
		{
			if (m_Current == null)
				m_Current = ReadToken();

			return m_Current;
		}

		public Token Next()
		{
			if (m_Current != null)
			{
				Token t = m_Current;
				m_Current = null;
				return t;
			}
			else
				return ReadToken();
		}

		private void CursorNext()
		{
			if (CursorNotEof())
			{
				if (CursorChar() == '\n')
				{
					m_Col = 0;
					m_Line += 1;
				}
				else
				{
					m_Col += 1;
				}

				m_Cursor += 1;
			}
		}

		private char CursorChar()
		{
			return m_Code[m_Cursor];
		}

		private char CursorCharNext()
		{
			m_Cursor += 1;

			if (m_Cursor < m_Code.Length - 1)
				return m_Code[m_Cursor];
			else
				return '\0'; // fictitious sentinel
		}

		private bool CursorMatches(string pattern)
		{
			for (int i = 0; i < pattern.Length; i++)
			{
				int j = m_Cursor + i;

				if (j >= m_Code.Length)
					return false;
				if (m_Code[j] != pattern[i])
					return false;
			}
			return true;
		}

		private bool CursorNotEof()
		{
			return m_Cursor < m_Code.Length;
		}

		private bool IsWhiteSpace(char c)
		{
			return char.IsWhiteSpace(c) || (c == ';');
		}

		private void SkipWhiteSpace()
		{
			for (; CursorNotEof() && IsWhiteSpace(CursorChar()); CursorNext())
			{
			}
		}


		private Token ReadToken()
		{
			SkipWhiteSpace();

			int fromLine = m_Line;
			int fromCol = m_Col;

			if (!CursorNotEof())
				return new Token(TokenType.Eof);

			char c = CursorChar();

			switch (c)
			{
				case '=':
					return PotentiallyDoubleCharOperator('=', TokenType.Op_Assignment, TokenType.Op_Equal, fromLine, fromCol);
				case '<':
					return PotentiallyDoubleCharOperator('=', TokenType.Op_LessThan, TokenType.Op_LessThanEqual, fromLine, fromCol);
				case '>':
					return PotentiallyDoubleCharOperator('=', TokenType.Op_GreaterThan, TokenType.Op_GreaterThanEqual, fromLine, fromCol);
				case '~':
				case '!':
					if (CursorCharNext() != '=')
						throw new SyntaxErrorException("Expected '=', {0} was found", CursorChar());
					CursorCharNext();
					return CreateToken(TokenType.Op_NotEqual, fromLine, fromCol, "~=");
				case '.':
					if (CursorCharNext() == '.')
						return PotentiallyDoubleCharOperator('.', TokenType.Op_Concat, TokenType.VarArgs, fromLine, fromCol);
					else
						return CreateToken(TokenType.Dot, fromLine, fromCol, ".");
				case '+':
					return CreateSingleCharToken(TokenType.Op_Add, fromLine, fromCol);
				case '-':
					{
						char next = CursorCharNext();
						if (next == '-')
						{
							return ReadComment(fromLine, fromCol);
						}
						else
						{
							return CreateToken(TokenType.Op_MinusOrSub, fromLine, fromCol, "-");
						}
					}
				case '*':
					return CreateSingleCharToken(TokenType.Op_Mul, fromLine, fromCol);
				case '/':
					return CreateSingleCharToken(TokenType.Op_Div, fromLine, fromCol);
				case '%':
					return CreateSingleCharToken(TokenType.Op_Mod, fromLine, fromCol);
				case '^':
					return CreateSingleCharToken(TokenType.Op_Pwr, fromLine, fromCol);
				case '#':
					return CreateSingleCharToken(TokenType.Op_Len, fromLine, fromCol);
				case '[':
					{
						char next = CursorCharNext();
						if (next == '=' || next == '[')
						{
							string str = ReadLongString();
							return CreateToken(TokenType.LongString, fromLine, fromCol, str);
						}
						return CreateToken(TokenType.Brk_Open_Square, fromLine, fromCol, "[");
					}
				case ']':
					return CreateSingleCharToken(TokenType.Brk_Close_Square, fromLine, fromCol);
				case '(':
					return CreateSingleCharToken(TokenType.Brk_Open_Round, fromLine, fromCol);
				case ')':
					return CreateSingleCharToken(TokenType.Brk_Close_Round, fromLine, fromCol);
				case '{':
					return CreateSingleCharToken(TokenType.Brk_Open_Curly, fromLine, fromCol);
				case '}':
					return CreateSingleCharToken(TokenType.Brk_Close_Curly, fromLine, fromCol);
				case ',':
					return CreateSingleCharToken(TokenType.Comma, fromLine, fromCol);
				case ':':
					return PotentiallyDoubleCharOperator(':', TokenType.Colon, TokenType.DoubleColon, fromLine, fromCol);
				case '"':
				case '\'':
					return ReadSimpleStringToken(fromLine, fromCol);
				default:
					{
						if (char.IsLetter(c) || c == '_')
						{
							string name = ReadNameToken();
							return CreateNameToken(name, fromLine, fromCol);
						}
						else if (char.IsDigit(c))
						{
							string number = ReadNumberToken();
							return CreateToken(TokenType.Number, fromLine, fromCol, number);
						}
					}
					throw new SyntaxErrorException("Fallback to default ?!", CursorChar());
			}



		}

		private string ReadLongString()
		{
			// here we are at the first '=' or second '['
			StringBuilder text = new StringBuilder(1024);
			string end_pattern = "]";
	
			for (char c = CursorChar(); ; c = CursorCharNext())
			{
				if (c == '\0' || !CursorNotEof())
				{
					throw new SyntaxErrorException("Unterminated long string or comment"); 
				}
				else if (c == '=')
				{
					end_pattern += "=";
				}
				else if (c == '[')
				{
					end_pattern += "]";
					break;
				}
				else
				{
					throw new SyntaxErrorException("Unexpected token in long string prefix: {0}", c);
				}
			}


			for (char c = CursorChar(); ; c = CursorCharNext())
			{
				if (c == '\0' || !CursorNotEof())
				{
					throw new SyntaxErrorException("Unterminated long string or comment");
				}
				else if (c == ']' && CursorMatches(end_pattern))
				{
					for (int i = 0; i < end_pattern.Length; i++)
						CursorCharNext();

					return text.ToString();
				}
				else
				{
					text.Append(c);
				}
			}
		}

		private string ReadNumberToken()
		{
			StringBuilder text = new StringBuilder(32);

			//INT : Digit+
			//HEX : '0' [xX] HexDigit+
			//FLOAT : Digit+ '.' Digit* ExponentPart?
			//		| '.' Digit+ ExponentPart?
			//		| Digit+ ExponentPart
			//HEX_FLOAT : '0' [xX] HexDigit+ '.' HexDigit* HexExponentPart?
			//			| '0' [xX] '.' HexDigit+ HexExponentPart?
			//			| '0' [xX] HexDigit+ HexExponentPart
			//
			// ExponentPart : [eE] [+-]? Digit+
			// HexExponentPart : [pP] [+-]? Digit+

			bool isHex = false;
			bool dotAdded = false;
			bool exponentPart = false;

			text.Append(CursorChar());

			char secondChar = CursorCharNext();

			if (secondChar == 'x' || secondChar == 'X')
			{
				isHex = true;
				text.Append(CursorChar());
				CursorCharNext();
			}

			for (char c = CursorChar(); CursorNotEof(); c = CursorCharNext())
			{
				if (char.IsDigit(c))
				{
					text.Append(c);
				}
				else if (c == '.' && !dotAdded)
				{
					dotAdded = true;
					text.Append(c);
				}
				else if (Char_IsHexDigit(c) && isHex && !exponentPart)
				{
					text.Append(c);
				}
				else if (c == 'e' || c == 'E' || (isHex && (c == 'p' || c == 'P')))
				{
					text.Append(c);
					exponentPart = true;
					dotAdded = true;
				}
				else
				{
					return text.ToString();
				}
			}

			return text.ToString();
		}

		private bool Char_IsHexDigit(char c)
		{
			return char.IsDigit(c) ||
				c == 'a' || c == 'b' || c == 'c' || c == 'd' || c == 'e' || c == 'f' ||
				c == 'A' || c == 'B' || c == 'C' || c == 'D' || c == 'E' || c == 'F';
		}

		private Token CreateSingleCharToken(TokenType tokenType, int fromLine, int fromCol)
		{
			char c = CursorChar();
			CursorCharNext();
			return CreateToken(tokenType, fromLine, fromCol, c.ToString());
		}

		private Token ReadComment(int fromLine, int fromCol)
		{
			StringBuilder text = new StringBuilder(32);

			char next1 = CursorCharNext();

			// +++ Long comments

			for (char c = CursorChar(); CursorNotEof(); c = CursorCharNext())
			{
				if (c == '\n')
				{
					CursorCharNext();
					return CreateToken(TokenType.Comment, fromLine, fromCol, text.ToString());
				}
				else if (c != '\r')
				{
					text.Append(c);
				}
			}

			return CreateToken(TokenType.Comment, fromLine, fromCol, text.ToString());
		}

		private Token ReadSimpleStringToken(int fromLine, int fromCol)
		{
			StringBuilder text = new StringBuilder(32);
			char separator = CursorChar();

			for (char c = CursorCharNext(); CursorNotEof(); c = CursorCharNext())
			{
				if (c == '\\')
				{
					text.Append(c);
					text.Append(CursorCharNext());
				}
				else if (c == separator)
				{
					CursorCharNext();
					return CreateToken(TokenType.SimpleString, fromLine, fromCol, text.ToString());
				}
				else
				{
					text.Append(c);
				}
			}

			throw new SyntaxErrorException("Unterminated string");
		}

		private Token PotentiallyDoubleCharOperator(char expectedSecondChar, TokenType singleCharToken, TokenType doubleCharToken, int fromLine, int fromCol)
		{
			string op = CursorChar().ToString() + CursorCharNext().ToString();

			return CreateToken(CursorChar() == expectedSecondChar ? doubleCharToken : singleCharToken,
				fromLine, fromCol, op);
		}



		private Token CreateNameToken(string name, int fromLine, int fromCol)
		{
			TokenType? reservedType = Token.GetReservedTokenType(name);

			if (reservedType.HasValue)
			{
				return CreateToken(reservedType.Value, fromLine, fromCol, name);
			}
			else
			{
				return CreateToken(TokenType.Name, fromLine, fromCol, name);
			}
		}


		private Token CreateToken(TokenType tokenType, int fromLine, int fromCol, string text = null)
		{
			return new Token(tokenType, fromLine, fromCol, m_Line, m_Col)
			{
				Text = text
			};
		}

		private string ReadNameToken()
		{
			StringBuilder name = new StringBuilder(32);

			for (char c = CursorChar(); CursorNotEof(); c = CursorCharNext())
			{
				if (char.IsLetterOrDigit(c) || c == '_')
					name.Append(c);
				else
					break;
			}

			return name.ToString();
		}




	}
}
