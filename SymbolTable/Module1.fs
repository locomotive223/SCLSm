﻿// Learn more about F# at http://fsharp.net

namespace SCLS

open System.Collections.Generic;
open System.Text;
open Microsoft.FSharp.Math

type SymbolTable  =  
    class
     // FIELDS
        val mutable table :  Dictionary< int, string> 
        val mutable inverse_table : Dictionary< string, int >
    // CONSTRUCTORS
        new() = { table = new Dictionary<int, string>(); inverse_table =  new Dictionary<string, int>(); }
    // METHODS
        /// get the number of different values
        member inline st.Count() : int = st.table.Count
        /// get a value by an address
        member st.get(i: int) : string Option =
            let ok, res = st.table.TryGetValue(i)
            if ok then Some(res)
                  else None
        /// get an address by an identifier
        member st.inverse_get(i: string) : int Option =
            let ok, res = st.inverse_table.TryGetValue(i)
            if ok then Some(res - 1)
                  else None
        /// add a value and get it's address
        member st.add(elem : string) : int =
            let ok,res = st.inverse_table.TryGetValue(elem)
            if ok 
                then (  res - 1 )
                else (
                    st.table.Add(st.Count(), elem )
                    st.inverse_table.Add(elem, st.Count())
                    st.Count()-1
                )
        /// remove a value
        member st.remove(elem : string) : unit =
            let ok,res = st.inverse_table.TryGetValue(elem)
            if (ok) 
                then (
                    st.inverse_table.Remove(elem) |> ignore
                    st.table.Remove(res) |> ignore
                )
                else ()
        /// get string representation of the symbol table
        override st.ToString() : string =
            let mutable output = new StringBuilder("[ ")
            for keyVal in st.table do 
                output.Append("( " + keyVal.Key.ToString() + " , " + box(keyVal.Value).ToString() + " )") |> ignore 
            output.Append(" ]") |> ignore
            output.ToString()
    end 
    
type TOOLS =
        class
            /// translate the call by identifiers in a call by symbols
        static member preprocess_parameter(what:string, s:SymbolTable) :  string =
            
                let what_array =    what.Split([|' '|])  
                                    |> Array.map (fun (ide:string) -> match s.inverse_get(ide) with Some(addr) -> addr.ToString() | None -> "-1")
                                    |> Set.ofSeq
             
                if what_array.Count > 0 
                    then 
                        let sb = new StringBuilder()
                        for w in what_array do
                            sb.Append(w.ToString() + ", ") |> ignore
                        done
                        sb.Remove(sb.Length-2,2) |> ignore
                        sb.ToString()
                else ""  
        end     
               

