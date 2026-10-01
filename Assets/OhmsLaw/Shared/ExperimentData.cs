using UnityEngine;

namespace OhmsLaw
{
    // The one set of readings for the nichrome wire XY. Meters, table, graph and the V/I column all read from here.
    [CreateAssetMenu(menuName = "Ohm's Law/Experiment Data")]
    public class ExperimentData : ScriptableObject
    {
        public float[] voltages = { 1, 2, 3, 4, 5 };
        public float[] currents = { 0.20f, 0.40f, 0.60f, 0.80f, 1.00f };
        public float resistance = 5f;

        static ExperimentData main;
        public static ExperimentData Main => main ? main : main = Resources.Load<ExperimentData>("ExperimentData");

        public int Count => voltages.Length;
        public float CurrentAt(float volts) => volts / resistance;
        public float Ratio(int row) => voltages[row] / currents[row];

        // Every row must satisfy I = V / R, or the table and the meters would disagree.
        public bool Consistent()
        {
            if (voltages.Length != currents.Length) return false;
            for (int i = 0; i < Count; i++)
                if (Mathf.Abs(CurrentAt(voltages[i]) - currents[i]) > 0.001f) return false;
            return true;
        }
    }
}
