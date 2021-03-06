using SCLS;
using SCLS;
using System.Collections;
using System.Collections.Generic;
using System.Text;


using System;



public class Parser {
	const int _EOF = 0;
	const int _Id = 1;
	const int _Number = 2;
	const int _VART = 3;
	const int _VARS = 4;
	const int _VARE = 5;
	const int _RULESDIC = 6;
	const int _TERMDIC = 7;
	const int _PATTERNDIC = 8;
	const int _EXCLUDEDIC = 9;
	const int _CODEDELIMITER = 10;
	const int _LOOP = 11;
	const int maxT = 23;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

public static Model model;



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void SCLS() {
		List<Rule> rulesList = new List<Rule>();
        Compartment initialTerm = new Compartment(Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.None);
		SymbolTable symbols = new SymbolTable();
		List<Pattern> patternsList = new List<Pattern>();
		List<int> vars = new List<int>();	
		StringBuilder TEMP = new StringBuilder(); 
		List<string> exclude_list = new List<string>();			
		
		Expect(6);
		rules(rulesList, symbols, vars);
		Expect(7);
		terms(initialTerm, symbols, vars);
		if (la.kind == 8) {
			Get();
			if (la.kind == 1 || la.kind == 2) {
				patterns(patternsList, symbols, vars  );
			}
		}
		if (la.kind == 9) {
			Get();
			if (la.kind == 1 || la.kind == 2 || la.kind == 12) {
				if (la.kind == 1 || la.kind == 2) {
					Identifier(TEMP);
					exclude_list.Add(TEMP.ToString()); TEMP = new StringBuilder(); 
					while (la.kind == 1 || la.kind == 2) {
						Identifier(TEMP);
						exclude_list.Add(TEMP.ToString()); TEMP = new StringBuilder(); 
					}
				} else {
					Get();
					foreach (KeyValuePair<string,int> keyVal in symbols.inverse_table) {	exclude_list.Add(keyVal.Key); } 
					
					if (la.kind == 13) {
						Get();
						Identifier(TEMP);
						exclude_list.Remove(TEMP.ToString()); TEMP = new StringBuilder(); 
						while (la.kind == 1 || la.kind == 2) {
							Identifier(TEMP);
							exclude_list.Remove(TEMP.ToString()); TEMP = new StringBuilder(); 
						}
					}
				}
			}
		}
		foreach (Rule r in rulesList) { r.init(symbols);    }
        model = new Model(rulesList.ToArray(), patternsList.ToArray(), initialTerm, symbols,Microsoft.FSharp.Collections.SetModule.OfSeq<int>(vars), Microsoft.FSharp.Collections.SetModule.OfSeq<string>(exclude_list));	
		
	}

	void rules(List<Rule> rulesList, SymbolTable symbols, List<int> vars  ) {
		
		rule(rulesList, symbols,vars );
		
		if (la.kind == 1 || la.kind == 2) {
			rules(rulesList, symbols,vars );
		}
	}

	void terms(Compartment upperTerm, SymbolTable symbols,List<int> vars  ) {
        Compartment currentTerm = new Compartment(Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.None); 
		term(currentTerm, symbols,vars);
		if (la.kind == 18) {
			Get();
			Expect(2);
		}
		long temp;
		long.TryParse(t.val, out temp);
		if(temp == 0) temp++;
		upperTerm.AddChild(new Tuple<Node,long,bool>(currentTerm, temp, true));
		
		if (la.kind == 19) {
			Get();
			terms(upperTerm, symbols,vars );
		}
	}

	void patterns(List<Pattern> patternsList, SymbolTable symbols, List<int> vars  ) {
		
		pattern(patternsList, symbols,vars );
		
		if (la.kind == 1 || la.kind == 2) {
			patterns(patternsList, symbols,vars );
		}
	}

	void Identifier(StringBuilder TEMP) {
		if (la.kind == 2) {
			Get();
			TEMP.Append(t.val); 
		}
		Expect(1);
		TEMP.Append(t.val); 
	}

	void rule(List<Rule> rulesList, SymbolTable symbols,List<int> vars  ) {
        Compartment left = new Compartment(Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.None);
        Compartment right = new Compartment(Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.None);	
		StringBuilder TEMP = new StringBuilder();							
		Identifier(TEMP);
		string name = TEMP.ToString();	
		Expect(14);
		Expect(15);
		terms(left, symbols,vars );
		Expect(16);
		
		if (StartOf(1)) {
			terms(right , symbols,vars );
		}
		Expect(16);
		TEMP = new StringBuilder();	
		rateFormula(TEMP);
		Expect(17);
		rulesList.Add(new Rule(name,left, right, TEMP.ToString(), symbols));
	}

	void rateFormula(StringBuilder TEMP) {
		if (la.kind == 10) {
			Get();
			Get();
			TEMP.Append("#" + t.val); 
			while (StartOf(2)) {
				Get();
				TEMP.Append(t.val); 
			}
			Expect(10);
		} else if (StartOf(2)) {
			Get();
			TEMP.Append(t.val); 
		} else SynErr(24);
	}

	void term(Compartment term, SymbolTable symbols,List<int> vars  ) {
		if (StartOf(3)) {
            Sequence seq = new Sequence(Microsoft.FSharp.Core.FSharpOption<List<Element>>.None, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.Some(Microsoft.FSharp.Core.FSharpOption<Node>.Some(term)));
			sequences(seq, symbols,vars );
			term.AddChild( new Tuple<Node,long,bool>(seq, 1,true) );			
		} else if (la.kind == 3) {
			StringBuilder TEMP = new StringBuilder(); 
			Get();
			Identifier(TEMP);
			int el = symbols.add(TEMP.ToString());
            term.AddChild(new Tuple<Node, long, bool>(new TermVariable(el, Microsoft.FSharp.Core.FSharpOption<Node>.Some(term)), 1, true));
			vars.Add(el);
			
		} else if (la.kind == 11 || la.kind == 15) {
            LoopingSequence seq = new LoopingSequence(Microsoft.FSharp.Core.FSharpOption<List<Element>>.None, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.Some(Microsoft.FSharp.Core.FSharpOption<Node>.None));
            Compartment content = new Compartment(Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.Some(Microsoft.FSharp.Core.FSharpOption<Node>.None));
			
			
			if (la.kind == 11) {
				Get();
			}
			Expect(15);
			if (StartOf(3)) {
				sequences(seq, symbols,vars);
			}
			Expect(17);
			if (la.kind == 20) {
				enclosedTerm(content, symbols,vars);
			}
            Loop l = new Loop(seq, content, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.Some(Microsoft.FSharp.Core.FSharpOption<Node>.Some(term)));
			term.AddChild(new Tuple<Node,long,bool>(l, 1, true)); 
		} else SynErr(25);
	}

	void sequences(Sequence seq, SymbolTable symbols,List<int> vars  ) {
		int elem; int type; /*int link;*/ 
		sequence(out elem, out type/*, out link*/, symbols,vars );
		switch (type)
		    {
		        case 0: seq.AddChild(new ConstantElement(elem/*, link*/));
		  		break;
		  case 1: seq.AddChild(new ElementVariable(elem/*, link*/));
			vars.Add(elem);
		          break;
		  case 2: seq.AddChild(new SequenceVariable(elem));
			vars.Add(elem);
		          break;
		
		        }
					
		if (la.kind == 22) {
			Get();
			sequences(seq, symbols,vars);
		}
	}

	void enclosedTerm(Compartment term, SymbolTable symbols,List<int> vars 	) {
		Expect(20);
		terms(term, symbols,vars);
		Expect(21);
		
	}

	void sequence(out int elem, out int type/*, out int link*/, SymbolTable symbols,List<int> vars  ) {
		elem = 0; type = 0;/* link = -1*/ ; StringBuilder TEMP = new StringBuilder(); 
		if (la.kind == 1 || la.kind == 2) {
			
			Identifier(TEMP);
			elem = symbols.add(TEMP.ToString()); 
		} else if (la.kind == 5) {
			Get();
			Identifier(TEMP);
			elem = symbols.add(TEMP.ToString()); type = 1;
		} else if (la.kind == 4) {
			Get();
			Identifier(TEMP);
			elem = symbols.add(TEMP.ToString()); type = 2;
		} else SynErr(26);
	}

	void pattern(List<Pattern> patternsList, SymbolTable symbols,List<int> vars  ) {
        Compartment p = new Compartment(Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<Node>>.None);
		StringBuilder TEMP = new StringBuilder();							
																			
		Identifier(TEMP);
		string name = TEMP.ToString();	
		Expect(14);
		Expect(15);
		terms(p, symbols,vars );
		TEMP = new StringBuilder();	
		if (la.kind == 16) {
			Get();
			rateFormula(TEMP);
		}
		Expect(17);
		patternsList.Add(new Pattern(p,name, TEMP.ToString() )); 
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		SCLS();

    Expect(0);
	}
	
	bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,x,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x},
		{x,T,T,T, T,T,T,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, x},
		{x,T,T,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
	public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
  public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text
  
	public void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "Id expected"; break;
			case 2: s = "Number expected"; break;
			case 3: s = "VART expected"; break;
			case 4: s = "VARS expected"; break;
			case 5: s = "VARE expected"; break;
			case 6: s = "RULESDIC expected"; break;
			case 7: s = "TERMDIC expected"; break;
			case 8: s = "PATTERNDIC expected"; break;
			case 9: s = "EXCLUDEDIC expected"; break;
			case 10: s = "CODEDELIMITER expected"; break;
			case 11: s = "LOOP expected"; break;
			case 12: s = "\"ALL\" expected"; break;
			case 13: s = "\"-\" expected"; break;
			case 14: s = "\":\" expected"; break;
			case 15: s = "\"(\" expected"; break;
			case 16: s = "\",\" expected"; break;
			case 17: s = "\")\" expected"; break;
			case 18: s = "\"*\" expected"; break;
			case 19: s = "\"|\" expected"; break;
			case 20: s = "\"[\" expected"; break;
			case 21: s = "\"]\" expected"; break;
			case 22: s = "\".\" expected"; break;
			case 23: s = "??? expected"; break;
			case 24: s = "invalid rateFormula"; break;
			case 25: s = "invalid term"; break;
			case 26: s = "invalid sequence"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}

