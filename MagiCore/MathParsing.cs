﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MagiCore
{
    public class MathParsing
    {
        /// <summary>
        /// Replace all instances of variables with their provided values
        /// </summary>
        /// <param name="input"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public static string ReplaceMathVariables(string input, Dictionary<string, string> variables)
        {
            string cpy = input;
            if (variables != null)
            {
                foreach (KeyValuePair<string, string> kvp in variables)
                {
                    if (cpy.Contains("[" + kvp.Key + "]"))
                    {
                        cpy = cpy.Replace("[" + kvp.Key + "]", kvp.Value);
                    }
                }
            }
            return cpy;
        }

        /// <summary>
        /// Takes a string and evaluates it as a mathematical expression, replacing any variables "ie, [X]" with the provided values
        /// </summary>
        /// <param name="input"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public static double ParseMath(string input, Dictionary<string, string> variables)
        {
            input = ReplaceMathVariables(input, variables);

            double currentVal = 0;
            string stack = "";
            string lastOp = "+";
            string[] ops = { "+", "-", "*", "/", "%", "^", "(", "e", "E" };
            string[] functions = { "min", "max", "l", "L", "abs", "sign", "if" }; //if (a < b ? stuff : other stuff)
            for (int i = 0; i < input.Length; ++i)
            {
                string ch = input[i].ToString();
                bool isOp = false, isFunction = false;
                foreach (string op in ops)
                {
                    if (op == ch)
                    {
                        isOp = true;
                        break;
                    }
                }
                if (!isOp)
                {
                    foreach (string fun in functions)
                    {
                        if (fun[0] == input[i])
                        {
                            isFunction = true;
                            break;
                        }
                    }
                }

                if (isOp)
                {
                    if (ch == "-" && (stack.Trim() == ""))
                    {
                        stack += ch;
                    }
                    else if (ch == "e" || ch == "E")
                    {
                        int index;
                        for (index = i + 2; index < input.Length; ++index)
                        {
                            string ch2 = input[index].ToString();
                            if (ops.Contains(ch2))
                                break;
                        }
                        string sub = input.Substring(i + 1, index - i - 1);
                        double exp = ParseMath(sub, variables);
                        double newVal = double.Parse(stack) * Math.Pow(10, exp);
                        currentVal = DoMath(currentVal, lastOp, newVal.ToString());
                        stack = "0";
                        lastOp = "+";
                        i = index - 1;
                    }
                    else if (ch == "(")
                    {
                        int j = FindEndParenthesis(input, i)[0];
                        string sub = input.Substring(i + 1, j - i - 1);
                        string val = ParseMath(sub, variables).ToString();
                        input = input.Substring(0, i) + val + input.Substring(j + 1);
                        --i;
                    }
                    else
                    {
                        currentVal = DoMath(currentVal, lastOp, stack);
                        lastOp = ch;
                        stack = "";
                    }
                }
                else if (isFunction)
                {
                    int subStart = input.IndexOf('(', i) + 1;
                    string function = input.Substring(i, subStart - i - 1);
                    int[] parenComma = FindEndParenthesis(input, subStart - 1);
                    int j = parenComma[0];
                    int comma = parenComma[1];
                    string sub = input.Substring(subStart, j - subStart);
                    double val = 0.0;

                    if (function == "if")
                    {
                        val = DoIfStatement(sub);
                    }
                    else if (function == "l")
                    {
                        val = ParseMath(sub, variables);
                        val = Math.Log(val);
                    }
                    else if (function == "L")
                    {
                        val = ParseMath(sub, variables);
                        val = Math.Log10(val);
                    }
                    else if (function == "max" || function == "min")
                    {
                        string[] parts = new string[2];
                        parts[0] = input.Substring(subStart, parenComma[1] - subStart);
                        parts[1] = input.Substring(parenComma[1] + 1, j - parenComma[1] - 1);
                        double sub1 = ParseMath(parts[0], variables);
                        double sub2 = ParseMath(parts[1], variables);
                        if (function == "max")
                            val = Math.Max(sub1, sub2);
                        else if (function == "min")
                            val = Math.Min(sub1, sub2);
                    }
                    else if (function == "sign")
                    {
                        val = ParseMath(sub, variables);
                        if (val >= 0)
                            val = 1;
                        else
                            val = -1;
                    }
                    else if (function == "abs")
                    {
                        val = ParseMath(sub, variables);
                        val = Math.Abs(val);
                    }

                    input = input.Substring(0, i) + val.ToString() + input.Substring(j + 1);
                    i--;
                }
                else
                {
                    stack += ch;
                }
            }
            currentVal = DoMath(currentVal, lastOp, stack);
            return currentVal;
        }

        /// <summary>
        /// Locates the terminating paranthesis for the given position. Also finds the location of any commas (for max, min, etc)
        /// </summary>
        /// <param name="str"></param>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private static int[] FindEndParenthesis(string str, int curPos)
        {
            int depth = 0;
            int j = 0, commaPos = -1;
            for (j = curPos + 1; j < str.Length; ++j)
            {
                if (str[j] == '(') depth++;
                if (str[j] == ')') depth--;

                if (str[j] == ',' && depth == 0)
                    commaPos = j;

                if (depth < 0)
                {
                    break;
                }
            }
            return new int[] { j, commaPos };
        }

        /// <summary>
        /// Takes the current value and performs the requested operation on it
        /// </summary>
        /// <param name="currentVal"></param>
        /// <param name="operation"></param>
        /// <param name="newVal"></param>
        /// <returns></returns>
        private static double DoMath(double currentVal, string operation, string newVal)
        {
            double newValue = 0;
            if (String.IsNullOrEmpty(newVal))
                return currentVal;
            if (!double.TryParse(newVal, out newValue))
            {
                UnityEngine.Debug.LogError("[MagiCore] Tried to parse a non-double value: " + newVal);
                return currentVal;
            }
            switch (operation)
            {
                case "+": currentVal += newValue; break;
                case "-": currentVal -= newValue; break;
                case "*": currentVal *= newValue; break;
                case "/": currentVal /= newValue; break;
                case "%": currentVal = currentVal % long.Parse(newVal); break;
                case "^": currentVal = Math.Pow(currentVal, newValue); break;
                default: break;
            }

            return currentVal;
        }

        /// <summary>
        /// Performs parsing of inline if statements
        /// </summary>
        /// <param name="statement"></param>
        /// <returns></returns>
        private static double DoIfStatement(string statement)
        {
            //At this point "statement" would look something like a < b ? stuff : other stuff
            //We need to grab the conditional and the two possible values, then do the conditional and evaluate the correct value
            string[] conditionals = { "<", ">", "<=", ">=", "==", "!=" }; //do we want to support && and ||, too? Ideally yes, but it's way tougher
            double val = 0.0;

           // Debug.Log("MagiCore: Statement = " + statement);

            int indexOfQMark = statement.IndexOf("?");
            string conditionalSection = statement.Substring(0, indexOfQMark);
            //string[] condSides = conditionalSection.Split(conditionals, StringSplitOptions.RemoveEmptyEntries);
            foreach (string conditional in conditionals)
                conditionalSection = conditionalSection.Replace(conditional, ";" + conditional + ";");
            string[] parts = conditionalSection.Split(';');
            //this is a kind of horrible method that I got from here, but avoided excessive regex: http://stackoverflow.com/a/2484982
            
            //lets assume there are only three elements
            if (parts.Length < 3)
                return 0.0;

            //do math on part one
            //do math on part two
            //compare them
            double val1 = ParseMath(parts[0], null);
            double val2 = ParseMath(parts[2], null);

           // Debug.Log("MagiCore: val1 = " + val1);
           // Debug.Log("MagiCore: val2 = " + val2);

            int indexOfColon = indexOfQMark;
            int depth = 0;
            for (int i = indexOfQMark + 1; i < statement.Length; i++)
            {
                char s = statement[i];
                if (s == '(') depth++;
                if (s == ')') depth--;

                if (s == ':' && depth == 0)
                    indexOfColon = i;

                if (depth < 0)
                    break;
            }

            string leftOption = statement.Substring(indexOfQMark + 1, indexOfColon - indexOfQMark - 1);//starts at "?" and ends at last ":" for this depth
            string rightOption = statement.Substring(indexOfColon + 1); //starts at last ":" and ends at end of statment

           // Debug.Log("MagiCore: left option = " + leftOption);
           // Debug.Log("MagiCore: right option = " + rightOption);

            string selectedOption = "";

            switch (parts[1])
            {
                case "<": selectedOption = val1 < val2 ? leftOption : rightOption; break;
                case ">": selectedOption = val1 > val2 ? leftOption : rightOption; break;
                case "<=": selectedOption = val1 <= val2 ? leftOption : rightOption; break;
                case ">=": selectedOption = val1 >= val2 ? leftOption : rightOption; break;
                case "==": selectedOption = val1 == val2 ? leftOption : rightOption; break;
                case "!=": selectedOption = val1 != val2 ? leftOption : rightOption; break;
                default: selectedOption = leftOption; break;
            }

            val = ParseMath(selectedOption, null);

            return val;
        }
    }
}
