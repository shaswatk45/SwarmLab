using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// Turns the lab into a real experiment: it samples the live metrics on a fixed
    /// interval and exports them to a timestamped CSV file (experiment, active agents,
    /// connectivity, the experiment's own metric). This is the "it's not just visual"
    /// proof - you can open the CSV in Excel and plot coverage-over-time, rescue time,
    /// or how connectivity collapses when comms are jammed.
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        public float sampleInterval = 0.5f;

        private SwarmManager _swarm;
        private readonly List<string> _rows = new List<string>();
        private float _acc;
        private float _t;
        public string LastExportPath { get; private set; }
        public int SampleCount => _rows.Count;

        private void Start()
        {
            _swarm = FindFirstObjectByType<SwarmManager>();
            _rows.Add("time_s,experiment,active_agents,total_agents,connectivity,comms_jammed,metric");
        }

        private void Update()
        {
            if (_swarm == null) return;
            _t += Time.deltaTime;
            _acc += Time.deltaTime;
            if (_acc < sampleInterval) return;
            _acc = 0f;

            var b = _swarm.ActiveBehaviour;
            string exp = b != null ? b.DisplayName : "-";
            string metric = b != null ? b.Metrics(_swarm) : "";
            if (string.IsNullOrEmpty(metric)) metric = "";
            metric = metric.Replace(",", ";").Replace("\n", " ");

            var ci = CultureInfo.InvariantCulture;
            _rows.Add(string.Format(ci, "{0:F1},{1},{2},{3},{4:F2},{5},{6}",
                _t, exp, _swarm.ActiveCount, _swarm.Count, _swarm.Connectivity,
                _swarm.CommsJammed ? 1 : 0, metric));
        }

        /// <summary>Write the collected samples to a CSV next to the project. Returns the path.</summary>
        public string Export()
        {
            try
            {
                string root = Directory.GetParent(Application.dataPath).FullName;
                string dir = Path.Combine(root, "SwarmLab_Reports");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "run_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");

                var sb = new StringBuilder();
                foreach (var r in _rows) sb.AppendLine(r);
                File.WriteAllText(file, sb.ToString());

                LastExportPath = file;
                Debug.Log("SwarmLab: exported " + (_rows.Count - 1) + " samples to " + file);
                return file;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("SwarmLab CSV export failed: " + e.Message);
                return null;
            }
        }
    }
}
