using System;
using System.Collections.Generic;
namespace AdaptiveOhmsLaw
{
    // Topology, not button order, determines whether the learner has built the circuit.
    public sealed class CircuitAssemblyModel
    {
        public static readonly string[] Terminals={"Source +","Source −","A +","A −","X","Y","V +","V −","Key +","Key −"};
        static readonly int[,] required={{0,2},{3,4},{5,8},{9,1},{6,4},{7,5}};
        readonly HashSet<string> connected=new HashSet<string>();
        public int Count=>connected.Count;
        public bool Complete=>Count==required.GetLength(0);
        public bool Closed {get;private set;}
        static string Key(int a,int b)=>Math.Min(a,b)+":"+Math.Max(a,b);
        public bool Connect(int a,int b,out string feedback){
            if(a<0||b<0||a>=Terminals.Length||b>=Terminals.Length||a==b){feedback="Choose two different terminals.";return false;}
            if(connected.Contains(Key(a,b))){feedback="These terminals are already connected.";return false;}
            for(int i=0;i<required.GetLength(0);i++)if(Key(a,b)==Key(required[i,0],required[i,1])){connected.Add(Key(a,b));feedback=Complete?"All six connections are ready. Close the key.":"Connection added. Keep the ammeter in the main loop and the voltmeter across X and Y.";return true;}
            feedback=(a==6||a==7||b==6||b==7)?"The voltmeter belongs across the wire: V + to X, and V − to Y.":"Follow the main loop: source → ammeter → wire XY → key → source. Keep the key open while connecting.";return false;
        }
        public void NextHint(out int a,out int b){for(int i=0;i<required.GetLength(0);i++)if(!connected.Contains(Key(required[i,0],required[i,1]))){a=required[i,0];b=required[i,1];return;}a=b=-1;}
        public bool Close(){if(!Complete)return false;Closed=true;return true;}
    }
    [Serializable] public class CircuitAssemblyRecord
    {
        public string startedUtc,completedUtc;
        public int connections,mistakes,hints;
        public float responseSeconds;
        public bool completed;
    }
}
