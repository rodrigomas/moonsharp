﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonSharp.Interpreter.Tree
{
	enum TokenType
	{
		Eof,
		Name,
		And,
		Break,
		Do,
		Else,
		ElseIf,
		End,
		False,
		For,
		Function,
		Goto,
		If,
		In,
		Local,
		Nil,
		Not,
		Or,
		Repeat,
		Return,
		Then,
		True,
		Until,
		While,
		Op_Equal,
		Op_Assignment,
		Op_LessThan,
		Op_LessThanEqual,
		Op_GreaterThanEqual,
		Op_GreaterThan,
		Op_NotEqual,
		Op_Concat,
		VarArgs,
		Dot,
		Colon,
		DoubleColon,
		Comma,
		Brk_Close_Curly,
		Brk_Open_Curly,
		Brk_Close_Round,
		Brk_Open_Round,
		Brk_Close_Square,
		Brk_Open_Square,
		Op_Len,
		Op_Pwr,
		Op_Mod,
		Op_Div,
		Op_Mul,
		Op_MinusOrSub,
		Op_Add,
		SimpleString,
		Comment,
		Number,
		LongString,
	}



}
