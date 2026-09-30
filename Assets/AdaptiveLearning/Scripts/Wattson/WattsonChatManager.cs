using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace AdaptiveOhmsLaw
{
    [Serializable] public class LearningContext
    {
        public string scene, activity, recentMistake, step;
        public ConceptId concept;
        public float mastery;
        public SupportLevel support;
        public bool switchClosed;
        public float sourceVoltage;
        public float circuitResistance = 10f;
        public float currentReading;
        public float resistorVoltageReading;
        public int recordedObservationsCount, plottedObservationsCount;
        public float selectedVoltage, selectedCurrent;
        public string question;
        public string[] answerChoices;
        public bool hasCircuitMeasurements,hasVoltageDemonstration,hasSelectedReading,usingPracticeData;
    }
    [Serializable] public class WattsonWebSource {public string title,url;}
    [Serializable] public class WattsonChatTurn
    {
        public string role, text;
        public string sourcesUrl;
        public bool searched;
        public WattsonChatTurn(string role,string text){this.role=role;this.text=text;}
    }
    public interface IWattsonResponseProvider { Task<string> RespondAsync(string message, LearningContext context, CancellationToken cancellation); }
    public interface IWattsonConversationProvider : IWattsonResponseProvider
    {
        Task<string> RespondAsync(string message,LearningContext context,IReadOnlyList<WattsonChatTurn> history,CancellationToken cancellation);
    }
    public class LocalWattsonResponseProvider : IWattsonResponseProvider
    {
        public Task<string> RespondAsync(string message, LearningContext context, CancellationToken cancellation)
        { cancellation.ThrowIfCancellationRequested(); return Task.FromResult(Respond(message, context)); }
        public string Respond(string message, LearningContext context)
        {
            string text = message.Trim().ToLowerInvariant();
            if((context.activity=="Scene09BuildCircuit"||context.activity=="Scene08OurLabIntroduction")&&(text.Contains("next")||text.Contains("understand")||text.Contains("help")))return context.activity=="Scene09BuildCircuit"?"Connect the main loop: Source + to A +, A − to X, Y to Key +, and Key − to Source −. Then connect V + to X and V − to Y. Tap two terminals to add each lead. Close the key only after all six connections are ready. Asking for help never lowers mastery.":"Explore the instruments with NEXT INSTRUMENT. The ammeter measures current; the voltmeter measures potential difference; wire XY is the conductor we will investigate. Then continue to build the circuit.";
            if(context.activity=="Scene12DiscoverResistance")
            {
                if(text.Contains("inside")||text.Contains("wire")||text.Contains("electron")||text.Contains("drift")||text.Contains("lattice")||text.Contains("micro"))
                    return "Inside wire XY, free electrons undergo rapid random motion with a gentle net drift. Collisions with fixed lattice ions impede directed motion without consuming charges — that opposition is electrical resistance.";
                if(text.Contains("ratio")||text.Contains("divide")||text.Contains("table")||text.Contains("constant")||text.Contains("v/i")||text.Contains("calc"))
                    return "Dividing potential difference by current gives the same ratio for every measurement: 1.0 V / 0.20 A = 5 Ω, 2.0 V / 0.40 A = 5 Ω, and so on. For this wire at constant temperature, that constant is its resistance R = 5 Ω.";
                if(text.Contains("slope")||text.Contains("graph")||text.Contains("triangle")||text.Contains("point"))
                    return "On a graph with potential difference (V) on the vertical axis and current (I) on the horizontal axis, the slope ΔV/ΔI gives resistance: for example, 3.0 V / 0.60 A = 5 Ω.";
                if(text.Contains("ohm")||text.Contains("unit")||text.Contains("symbol")||text.Contains("omega")||text.Contains("\u03a9"))
                    return "The unit of resistance is the ohm, symbol Ω. One ohm is the resistance when one volt across a conductor produces a current of one ampere: 1 Ω = 1 V / 1 A.";
                if(text.Contains("next")||text.Contains("help")||text.Contains("what")||text.Contains("understand"))
                    return "Follow the investigation steps: inspect wire XY's microscopic lattice, check the V/I ratios in the table, find the slope on the graph, and answer the adaptive resistance question.";
            }
            if(context.activity=="Scene13OhmsLaw")
            {
                if(text.Contains("law")||text.Contains("formula")||text.Contains("equation")||text.Contains("v=ir")||text.Contains("relationship"))
                    return "Ohm's Law unites our three quantities: V = I × R (Potential difference = Current × Resistance). When resistance is 5 Ω, voltage is always five times the current.";
                if(text.Contains("challenge")||text.Contains("dial")||text.Contains("knob")||text.Contains("0.6")||text.Contains("target")||text.Contains("set"))
                    return "To achieve 0.60 A across a 5 Ω wire, apply V = I × R: 0.60 A × 5 Ω = 3.0 V. Adjust the voltage dial to 3.0 V.";
                if(text.Contains("temperature")||text.Contains("condition")||text.Contains("constant"))
                    return "Ohm's Law holds for this conductor while its temperature stays constant, because heating can alter a material's resistance.";
                if(text.Contains("next")||text.Contains("help")||text.Contains("what")||text.Contains("understand"))
                    return "Drag or tap the equation tokens to assemble V = I × R, then use the rotary control to set 3.0 V and produce 0.60 A.";
            }
            if(context.activity=="Scene08Graph" && (text.Contains("graph")||text.Contains("plot")||text.Contains("axis")||text.Contains("axes")||text.Contains("next")||text.Contains("understand")||text.Contains("help")||text.Contains("pattern")))
            {
                if(text.Contains("pattern")||context.plottedObservationsCount==LessonExperimentData.ReadingCount)
                    return "The points form a straight line. Doubling voltage from 1 V to 2 V doubles current from 0.20 A to 0.40 A. This is proportionality for our fixed resistor at constant temperature. The origin (0 V, 0 A) is a prediction of this ideal model.";
                return "Current goes on the horizontal axis (A). Potential difference goes on the vertical axis (V). For the selected reading, start at "+context.selectedCurrent.ToString("0.00")+" A along the bottom and move up to "+context.selectedVoltage.ToString("0.0")+" V. Tap that intersection. "+context.plottedObservationsCount+" of 5 points are plotted.";
            }
            if (text.Contains("ammeter"))
                return "An ammeter measures electric current in amperes (A). It is connected in series in the loop so all current must pass through it.";
            if (text.Contains("voltmeter"))
                return "A voltmeter measures potential difference in volts (V). It is connected in parallel across the resistor's two terminals to measure the voltage drop.";
            if (text.Contains("zero"))
            {
                if (!context.switchClosed)
                    return "The current is 0.00 A because the switch is open. An open circuit has a broken path, so charges cannot flow around the loop.";
                if (context.sourceVoltage <= 0.001f)
                    return "The current is 0.00 A because the source potential difference is 0 V. Without electrical push, there is no net charge drift.";
                return "The current is zero whenever the circuit is open or disconnected.";
            }
            if (text.Contains("next") || text.Contains("do now") || text.Contains("should i do"))
            {
                if (context.activity == "Scene07OhmsExperiment")
                {
                    if (!context.switchClosed) return "Close the switch to complete the circuit path and observe current flow.";
                    if (context.recordedObservationsCount < LessonExperimentData.ReadingCount) return "Turn the source knob to each whole volt from 1 V to 5 V. Watch the current change, then press RECORD READING.";
                    return "Compare the five readings in the table and answer the evidence question below.";
                }
            }
            if ((text.Contains("same") || text.Contains("constant")) && (text.Contains("resist") || text.Contains("why")))
                return "We keep the resistance constant at " + context.circuitResistance.ToString("0") + " \u03a9 so we can test the effect of voltage alone. If resistance also changed, we wouldn't know which variable caused the current to change!";
            if (text.Contains("reading") || text.Contains("mean") || text.Contains("data") || text.Contains("table"))
                return "Look at your observations: as voltage across the resistor increases (1 V to 2 V to 3 V), the current also increases proportionally (0.20 A to 0.40 A to 0.60 A).";
            if (text.Contains("voltage") || text.Contains("potential")) return "Voltage is the potential difference between two points. In our experiment, think of it as the electrical push provided by the source. Its symbol is V; its unit is the volt.";
            if (text.Contains("current")) return "Current tells us how much electric charge flows through a point in the circuit each second. Its symbol is I; its unit is the ampere. Electron drift and conventional current point in opposite directions.";
            if (text.Contains("resistance")) return "Resistance describes how much a conductor opposes current. For this experiment, we keep the resistor and its resistance the same, including its temperature.";
            if (text.Contains("understand") || text.Contains("help") || text.Contains("hint") || text.Contains("why"))
            {
                if (context.activity == "Scene07OhmsExperiment")
                {
                    if (!context.switchClosed)
                        return "Let's take it one step at a time: close the switch on the apparatus to allow charge to flow through the circuit.";
                    if (context.recordedObservationsCount < LessonExperimentData.ReadingCount)
                        return "We are gathering five readings. Set 1 V, 2 V, 3 V, 4 V and 5 V, and press RECORD READING for each one. Asking for help never lowers mastery!";
                    return "Look at the observation table on the side: when voltage went up, did current go up, go down, or stay the same?";
                }
                return context.activity == "Scene06Prediction"
                    ? (context.support == SupportLevel.Supported || context.mastery < 40
                        ? "Let's take one step at a time. The same wire stays in place. Watch the source change from 1 V to 2 V, then watch the gentle net drift of the charges. Which prediction fits what you notice? You can try again; asking me never lowers mastery."
                        : "We are predicting what changes when the electrical push grows while resistance stays fixed. Watch the charge drift, then compare your three predictions. " + (string.IsNullOrEmpty(context.recentMistake) ? "You can ask for a clue at any time." : "Your recent prediction was: " + context.recentMistake + ". Let's investigate it together."))
                    : "Let's focus on " + context.concept + " in " + context.activity + ". Tell me which part you would like to explore.";
            }
            return "I'm using local lesson responses in this prototype. Ask me about voltage, current, resistance, the meters, or say 'What should I do next?' for guidance.";
        }
    }
    public class WattsonChatManager : MonoBehaviour
    {
        [SerializeField] WattsonServiceSettings service;
        static readonly List<WattsonChatTurn> history=new List<WattsonChatTurn>();
        public IReadOnlyList<WattsonChatTurn> History=>history.AsReadOnly();
        public bool UsesConnectedService=>provider is HttpWattsonResponseProvider;
        public bool LastReplyWasOffline {get;private set;}
        public bool LastReplyUsedSearch {get;private set;}
        public void SetService(WattsonServiceSettings settings)=>service=settings;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetSession()=>history.Clear();
        public LearningContext Context { get; private set; }
        public event Action<string, string> Responded;
        public event Action ExplanationRequested;
        IWattsonResponseProvider provider = new LocalWattsonResponseProvider();
        Func<LearningContext> getContext;
        bool sending;
        public void Configure(Func<LearningContext> context, IWattsonResponseProvider responseProvider = null)
        {
#if UNITY_EDITOR
            // Isolated lesson regressions must not spend API quota or depend on provider capacity.
            if(responseProvider==null&&StudentProfileManager.ValidationStore!=null)responseProvider=new LocalWattsonResponseProvider();
#endif
            getContext = context;provider=responseProvider??(service!=null&&service.IsConfigured?(IWattsonResponseProvider)new HttpWattsonResponseProvider(service):new LocalWattsonResponseProvider());
        }
        public async Task<string> SendAsync(string message, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";
            if(sending)throw new InvalidOperationException("Wattson is already responding.");
            if(message.Length>1200)throw new ArgumentException("Please keep the question below 1200 characters.");
            sending=true;
            try{
                cancellation.ThrowIfCancellationRequested();Context=getContext();
                string reply;LastReplyWasOffline=!UsesConnectedService;LastReplyUsedSearch=false;
                reply=provider is IWattsonConversationProvider conversational
                    ?await conversational.RespondAsync(message.Trim(),Context,history.ToArray(),cancellation)
                    :await provider.RespondAsync(message.Trim(),Context,cancellation);
                cancellation.ThrowIfCancellationRequested();
                if(string.IsNullOrWhiteSpace(reply))throw new InvalidOperationException("Wattson returned an empty reply.");
                if(reply.Length>6000)throw new InvalidOperationException("Wattson's reply exceeded the conversation limit.");
                // Failed or cancelled requests supplied no help and must not affect assistance scoring.
                ExplanationRequested?.Invoke();
                var turn=new WattsonChatTurn("assistant",reply);
                if(provider is HttpWattsonResponseProvider web){LastReplyUsedSearch=web.LastReplyUsedSearch;turn.searched=web.LastReplyUsedSearch;turn.sourcesUrl=web.LastSourcesUrl;}
                history.Add(new WattsonChatTurn("user",message.Trim()));history.Add(turn);
                while(history.Count>24)history.RemoveRange(0,2);
                Responded?.Invoke(message,reply);return reply;
            }finally{sending=false;}
        }
    }
}



