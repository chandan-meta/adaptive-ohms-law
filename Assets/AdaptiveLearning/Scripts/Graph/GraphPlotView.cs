using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace AdaptiveOhmsLaw
{
    public class GraphPlotView : MonoBehaviour
    {
        [SerializeField] GraphGridGraphic grid;
        [SerializeField] Button[] targets,rows;
        [SerializeField] Text status;
        List<ExperimentObservation> data;
        List<int> plotted;
        int selected;
        bool enabledForPlotting;
        SupportLevel currentSupport;
        public event Action<int,bool> Placement;
        public int Selected=>selected;
        public void Configure(GraphGridGraphic graphic,Button[] points,Button[] readings,Text instruction)
        {grid=graphic;targets=points;rows=readings;status=instruction;}
        public void Initialize(List<ExperimentObservation> observations,List<int> placed)
        {
            data=observations;plotted=placed;
            for(int i=0;i<targets.Length;i++){int cell=i;targets[i].onClick.AddListener(()=>Place(cell));}
            for(int i=0;i<rows.Length;i++){int row=i;rows[i].onClick.AddListener(()=>Select(row));}
            selected=Mathf.Max(0,data.FindIndex(o=>!plotted.Contains(data.IndexOf(o))));Refresh();
        }
        public void SetInteraction(bool value){enabledForPlotting=value;Refresh();}
        public void Select(int index){if(!enabledForPlotting||index<0||index>=data.Count||plotted.Contains(index))return;selected=index;Refresh();}
        void Place(int index)
        {
            if(!enabledForPlotting||plotted.Contains(selected))return;
            // Scene 11 uses the conventional V-versus-I layout: current on x,
            // potential difference on y.  The target array is column-major in x.
            float current=(index%LessonExperimentData.ReadingCount+1)*LessonExperimentData.CurrentStep, v=index/LessonExperimentData.ReadingCount+1;
            bool correct=Mathf.Abs(data[selected].voltage-v)<.01f&&Mathf.Abs(data[selected].current-current)<.005f;
            int chosen=selected;
            if(correct){plotted.Add(selected);for(int i=0;i<data.Count;i++)if(!plotted.Contains(i)){selected=i;break;}}
            Refresh();Placement?.Invoke(chosen,correct);
        }
        public void Highlight(SupportLevel support)
        {
            currentSupport=support;
            foreach(var target in targets)target.GetComponent<Outline>().enabled=false;
            if(data==null||plotted.Count==data.Count)return;
            var o=data[selected];
            for(int i=0;i<targets.Length;i++)
            {
                bool column=Mathf.Abs((i%LessonExperimentData.ReadingCount+1)*LessonExperimentData.CurrentStep-o.current)<.005f;
                bool row=Mathf.Abs((i/LessonExperimentData.ReadingCount+1)-o.voltage)<.01f;
                var outline=targets[i].GetComponent<Outline>();outline.enabled=support!=SupportLevel.Independent&&column&&(support!=SupportLevel.Supported||row);
            }
        }
        public void Refresh()
        {
            if(data==null)return;
            for(int i=0;i<rows.Length;i++)
            {
                rows[i].gameObject.SetActive(i<data.Count);if(i>=data.Count)continue;
                bool done=plotted.Contains(i);var o=data[i];
                rows[i].GetComponentInChildren<Text>().text=(done?"✓  ":"")+o.voltage.ToString("0.0")+" V   /   "+o.current.ToString("0.00")+" A";
                rows[i].interactable=enabledForPlotting&&!done;
                rows[i].GetComponentInChildren<Text>().color=WarmLabTheme.Ink;
                var rowColors=rows[i].colors;rowColors.normalColor=Color.white;rowColors.highlightedColor=Color.white;rowColors.selectedColor=Color.white;rowColors.disabledColor=Color.white;rows[i].colors=rowColors;
                rows[i].GetComponent<Image>().color=done?WarmLabTheme.Paper:i==selected?WarmLabTheme.Amber:WarmLabTheme.Ivory;
            }
            grid.points.Clear();
            for(int i=0;i<targets.Length;i++)
            {
                float current=(i%LessonExperimentData.ReadingCount+1)*LessonExperimentData.CurrentStep,v=i/LessonExperimentData.ReadingCount+1;
                bool done=plotted.Exists(p=>Mathf.Abs(data[p].voltage-v)<.01f&&Mathf.Abs(data[p].current-current)<.005f);
                targets[i].gameObject.SetActive(plotted.Count<data.Count||done);
                targets[i].interactable=enabledForPlotting&&!done;
                var marker=targets[i].GetComponent<GraphPointGraphic>();
                targets[i].GetComponentInChildren<Text>().text=marker!=null?"":done?"●":"+";
                if(marker!=null){marker.SetMarked(done);marker.color=done?WarmLabTheme.Plum:WarmLabTheme.Sage;}
                else targets[i].GetComponent<Image>().color=plotted.Count==data.Count?Color.clear:done?new Color(.1f,.48f,.45f):new Color(.065f,.17f,.23f);
            }
            foreach(var i in plotted)grid.points.Add(new Vector2(data[i].current,data[i].voltage));
            grid.points.Sort((a,b)=>a.x.CompareTo(b.x));grid.showTrend=plotted.Count==data.Count;grid.Refresh();Highlight(currentSupport);
            status.text=plotted.Count==data.Count?data.Count+" / "+data.Count+" points plotted — read the pattern":"Place: "+data[selected].voltage.ToString("0.0")+" V  /  "+data[selected].current.ToString("0.00")+" A";
        }
    }
}


